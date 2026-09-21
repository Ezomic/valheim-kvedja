using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace Kvedja
{
    /// <summary>
    /// Fetches the message and says it.
    ///
    /// <b>Why this is a client-side mod at all,</b> which is not obvious and cost this suite
    /// two versions to learn once already - see <c>crier/src/Announce.cs</c>. A server cannot
    /// put a line in the chat window. A routed <c>ChatMessage</c> arrives at
    /// <c>Chat.OnNewChatMessage</c>, which hands the sender's id to
    /// <c>Terminal.AddString(PlatformUserID, ...)</c>, and that opens by looking the sender up
    /// in the list of connected players. A dedicated server has no entry in that list and
    /// never will, so no identity it can invent gets through: the result was a red error in
    /// every client's log and a floating world-text pinned at the world origin. The overload
    /// that works, <c>AddString(string title, string text, Talker.Type)</c>, does no lookup
    /// and has no RPC behind it, so it can only be called here, on the machine reading it.
    ///
    /// <b>Nothing blocks the game thread.</b> The fetch is a network round trip to a box in
    /// another country, and doing it inline from a Harmony patch would freeze the client for
    /// as long as the far end takes to answer. So a worker thread asks, and the game thread
    /// only ever drains what came back.
    ///
    /// <b>Nothing here can break anybody's game.</b> No network, no DNS, a site that is down,
    /// a certificate that expired, a body that is not text: every one of them ends the same
    /// way, which is silence and at most a line in the log. A greeting is the least important
    /// thing on the screen and must behave like it.
    /// </summary>
    internal static class Motd
    {
        /// <summary>
        /// One for the life of the process. A new HttpClient per request leaks sockets in
        /// TIME_WAIT, which is a slow way to turn a greeting into a networking bug.
        /// </summary>
        private static readonly HttpClient Client = new HttpClient();

        private static readonly object Gate = new object();

        /// <summary>Lines waiting to be said, filled by the worker and drained by the game thread.</summary>
        private static readonly List<string> Pending = new List<string>();

        private static bool _asking;
        private static bool _saidThisSession;
        private static float _sayAt;

        /// <summary>
        /// A new Chat is a new world, so anything remembered about the last one goes. Without
        /// this, logging out to the menu and back in would be greeted in silence, because the
        /// session flag would still be set from the world before.
        /// </summary>
        internal static void NewWorld()
        {
            lock (Gate) Pending.Clear();

            _saidThisSession = false;
            _sayAt = 0f;
        }

        /// <summary>
        /// Called when the local player appears. Starts the fetch and sets the clock; the
        /// message is not said until Delay has passed, because the first second or two is the
        /// loading screen fading out and a line printed under it has been said to nobody.
        /// </summary>
        internal static void Wanted()
        {
            if (!KvedjaConfig.Enabled.Value) return;
            if (_saidThisSession && !KvedjaConfig.Repeat.Value) return;

            string url = KvedjaConfig.Url.Value;
            if (string.IsNullOrEmpty(url)) return;

            _sayAt = Time.realtimeSinceStartup + Mathf.Max(0f, KvedjaConfig.Delay.Value);

            Fetch(url);
        }

        private static void Fetch(string url)
        {
            lock (Gate)
            {
                // One request in flight at a time. Dying twice in ten seconds with Repeat on
                // must not become two requests racing each other into the same chat window.
                if (_asking) return;
                _asking = true;
            }

            var worker = new Thread(() => Ask(url)) { IsBackground = true, Name = "Kvedja" };
            worker.Start();
        }

        /// <summary>Runs on the worker thread. Touches nothing Unity owns.</summary>
        private static void Ask(string url)
        {
            try
            {
                // Unity's Mono still defaults to TLS 1.0, which no site has accepted for
                // years. Without this the request fails with a bare "could not create SSL/TLS
                // secure channel", which reads as a certificate problem and is not one. Crier
                // carries the same two lines for the same reason.
                try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; }
                catch { }

                Client.Timeout = TimeSpan.FromSeconds(Mathf.Clamp(KvedjaConfig.Timeout.Value, 1, 60));

                string body = Client.GetStringAsync(url).GetAwaiter().GetResult();

                var lines = new List<string>();
                foreach (string raw in body.Split('\n'))
                {
                    string line = raw.Trim('\r', ' ', '\t');
                    if (line.Length == 0) continue;

                    lines.Add(line);
                }

                lock (Gate)
                {
                    Pending.Clear();
                    Pending.AddRange(lines);
                }

                if (KvedjaConfig.Verbose.Value)
                {
                    KvedjaPlugin.Log.LogInfo("Read " + lines.Count + " line(s) from " + url);
                }
            }
            catch (Exception e)
            {
                // Silent to the player, on purpose. They cannot fix the site being down, and a
                // greeting that complains about itself is worse than no greeting.
                if (KvedjaConfig.Verbose.Value)
                {
                    KvedjaPlugin.Log.LogInfo("No message of the day: " + e.Message);
                }
            }
            finally
            {
                lock (Gate) _asking = false;
            }
        }

        /// <summary>Drains whatever came back, on the game thread. Called from Chat.Update.</summary>
        internal static void Tick()
        {
            if (_sayAt <= 0f) return;
            if (Time.realtimeSinceStartup < _sayAt) return;

            Chat chat = Chat.instance;
            if (chat == null) return;

            string[] lines;
            lock (Gate)
            {
                if (Pending.Count == 0) return;

                lines = Pending.ToArray();
                Pending.Clear();
            }

            _sayAt = 0f;
            _saidThisSession = true;

            string title = KvedjaConfig.Title.Value;
            Talker.Type voice = KvedjaConfig.Voice.Value;

            foreach (string line in lines)
            {
                chat.AddString(title, line, voice);
            }

            Show(chat);

            if (KvedjaConfig.Verbose.Value)
            {
                KvedjaPlugin.Log.LogInfo("Said " + lines.Length + " line(s) as " + title + ".");
            }
        }

        /// <summary>
        /// Opens the chat window, which adding the line does not do by itself.
        ///
        /// This is the trap in the whole mod. <c>Chat.Update</c> draws the window only while
        /// <c>m_hideTimer &lt; m_hideDelay</c>, and it is <c>OnNewChatMessage</c> that zeroes
        /// that timer when a real message arrives - not <c>AddString</c>, which only appends
        /// to the buffer. So the message would be in the scrollback and invisible, and the
        /// symptom is the worst kind: everything works, the log says it was said, and the only
        /// player who ever sees it is the one who happens to open chat in the next few seconds.
        /// </summary>
        private static void Show(Chat chat)
        {
            AccessTools.FieldRef<Chat, float> hideTimer = HideTimer();
            if (hideTimer == null) return;

            try { hideTimer(chat) = 0f; }
            catch (Exception) { }
        }

        private static AccessTools.FieldRef<Chat, float> _hideTimer;
        private static bool _triedHideTimer;

        /// <summary>
        /// Bound on first use inside a try/catch, never in a static initialiser. A
        /// FieldRefAccess with a wrong name throws at type-init, and from then on every member
        /// of the declaring class throws TypeInitializationException - which surfaces as
        /// unrelated things breaking rather than as this line being wrong. Bound this way, a
        /// rename in a game update costs the window opening by itself and nothing else.
        /// </summary>
        private static AccessTools.FieldRef<Chat, float> HideTimer()
        {
            if (_triedHideTimer) return _hideTimer;

            _triedHideTimer = true;

            try
            {
                _hideTimer = AccessTools.FieldRefAccess<Chat, float>("m_hideTimer");
            }
            catch (Exception e)
            {
                KvedjaPlugin.Log.LogWarning(
                    "Cannot reach the chat window's hide timer (" + e.GetType().Name + "). The "
                    + "message is still added to the chat log; what is lost is the window "
                    + "opening by itself to show it.");
            }

            return _hideTimer;
        }
    }
}
