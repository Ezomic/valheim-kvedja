using System;
using HarmonyLib;

namespace Kvedja
{
    /// <summary>
    /// Lines the server wants in the chat window: who died, who left, who arrived.
    ///
    /// <b>This half exists because a server cannot write to the chat window at all.</b> A
    /// routed <c>ChatMessage</c> ends at <c>Terminal.AddString(PlatformUserID, …)</c>, which
    /// opens by looking that id up in ZNet's connected-player list and returning on a miss -
    /// and a dedicated server has no entry in that list and never will. The overload that
    /// works, <c>AddString(string title, string text, Talker.Type)</c>, does no lookup and no
    /// RPC, so it can only be called on a client. That is the whole reason Kvedja is a
    /// separate DLL, and it is why the message of the day and these notices are the same
    /// shape of problem solved the same way.
    ///
    /// So the work is split across two mods and the seam is one RPC:
    ///
    ///   Crier, on the server, already knows who died, left and joined. It has known since
    ///   long before this - it posts all three to Discord and to the site - and until
    ///   2026-09-22 the only thing it could put on a player's screen was
    ///   <c>MessageHud.ShowMessage</c> in the top-left corner, because that is a channel
    ///   every stock client registers for itself.
    ///
    ///   Kvedja, here, turns one of those into a line in the chat window.
    ///
    /// <b>The RPC name below is a contract between two repositories and nothing enforces it.</b>
    /// Renaming it here without renaming it in <c>crier\src\Announce.cs</c> does not fail, it
    /// goes quiet: the server keeps sending, no handler matches, and every notice silently
    /// stops. Both files say so.
    ///
    /// A server that does not run Crier, or runs it set to the corner, sends nothing here and
    /// this class costs one registration per world.
    /// </summary>
    internal static class Notices
    {
        /// <summary>
        /// Namespaced, because a routed RPC is keyed on the stable hash of this string across
        /// every mod in the process. Must match Crier's copy exactly.
        /// </summary>
        internal const string NoticeRpc = "Kvedja_Notice";

        /// <summary>
        /// Registered from ZNet.Awake, not once per process.
        ///
        /// <c>ZNet.Awake</c> is where <c>m_routedRpc</c> is constructed, and it runs again for
        /// every world - logging out to the menu and back in builds a new ZRoutedRpc with an
        /// empty function table. A handler registered once per process is therefore gone by
        /// the second world, and the symptom is notices that work until somebody reconnects.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNet), "Awake")]
        private static void Register()
        {
            // Plain class rather than a UnityEngine.Object, so an ordinary null check is right.
            if (ZRoutedRpc.instance == null) return;

            ZRoutedRpc.instance.Register<string>(NoticeRpc, OnNotice);
        }

        /// <summary>
        /// A line from the server. Only from the server.
        ///
        /// The sender check is not ceremony. A routed RPC can be aimed at another client by
        /// anybody, and its sender field is written by whoever sent it - so without this, any
        /// player could put any words in everybody else's chat window under the server's own
        /// name, which is a better impersonation than typing could ever manage. Merki's rule
        /// handler guards itself the same way and for the same reason.
        /// </summary>
        private static void OnNotice(long sender, string text)
        {
            try
            {
                if (!KvedjaConfig.Enabled.Value) return;
                if (string.IsNullOrEmpty(text)) return;
                if (ZNet.instance == null) return;

                // A listen host is its own server and has no server peer; it also never sends
                // itself a routed RPC, so reaching here on one means somebody else did.
                ZNetPeer server = ZNet.instance.GetServerPeer();
                if (server == null || server.m_uid != sender) return;

                Motd.SayNow(text);
            }
            catch (Exception e)
            {
                // Never let this out. It runs inside ZRoutedRpc's dispatch, in the middle of
                // that connection's other messages, and a chat line is not worth a broken
                // connection.
                KvedjaPlugin.Log.LogWarning("Showing a server notice failed: " + e);
            }
        }
    }
}
