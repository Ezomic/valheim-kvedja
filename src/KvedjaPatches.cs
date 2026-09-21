using HarmonyLib;

namespace Kvedja
{
    /// <summary>
    /// The mod's Harmony patches. One class named in the plugin's PatchAll, so nothing goes
    /// live by being written.
    ///
    /// Three hooks, all on things that already exist for their own reasons: a new chat window,
    /// a player appearing, and the chat window's own frame.
    /// </summary>
    internal static class KvedjaPatches
    {
        /// <summary>
        /// A new Chat means a new world. Without this, going out to the main menu and back in
        /// would be greeted in silence, because the once-per-session flag would still be set
        /// from the world before - and logging in is exactly when this mod is supposed to
        /// speak.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Chat), nameof(Chat.Awake))]
        private static void ChatAwake()
        {
            Motd.NewWorld();
        }

        /// <summary>
        /// The local player appearing in the world. Not Chat.Awake for the fetch itself: the
        /// chat window is built during the load, well before there is anybody to greet.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static void OnSpawned(Player __instance)
        {
            // Every player object in the scene runs this, not only yours.
            if (__instance != Player.m_localPlayer) return;

            Motd.Wanted();
        }

        /// <summary>
        /// The game thread, once a frame, while the chat window exists. This is the only place
        /// anything Unity owns is touched - the fetch happens on a worker thread and leaves
        /// its lines in a list.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Chat), nameof(Chat.Update))]
        private static void ChatUpdate()
        {
            Motd.Tick();
        }
    }
}
