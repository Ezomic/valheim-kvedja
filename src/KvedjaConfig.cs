using BepInEx.Configuration;

namespace Kvedja
{
    /// <summary>
    /// Everything tunable, bound in one place so the .cfg reads as a document rather than as
    /// whatever order the code happened to need things in.
    ///
    /// The standing BepInEx trap applies here as everywhere: every entry is written to disk on
    /// first run and the saved value beats a new default in code. Changing a default does
    /// nothing on a machine that has already run the plugin - edit
    /// <c>&lt;profile&gt;\BepInEx\config\ezomic.valheim.kvedja.cfg</c> as part of the same
    /// change. When a config-driven change appears to do nothing in game, read the cfg before
    /// reading any code.
    /// </summary>
    internal static class KvedjaConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> Url;
        internal static ConfigEntry<string> Title;
        internal static ConfigEntry<Talker.Type> Voice;
        internal static ConfigEntry<float> Delay;
        internal static ConfigEntry<int> Timeout;
        internal static ConfigEntry<bool> Repeat;
        internal static ConfigEntry<bool> Verbose;

        internal static void Bind(ConfigFile cfg)
        {
            // Loaded, bound, patched, and deciding nothing. Not "unloaded" - a plugin cannot
            // unload itself, and a switch that pretends otherwise is a lie somebody will debug.
            Enabled = cfg.Bind("Kvedja", "Enabled", true,
                "Off leaves the plugin loaded and says nothing. Nothing is fetched either, so "
                + "this also stops the mod asking the site anything at all.");

            // An address rather than the text itself, which is the whole point: the message is
            // edited in one place and every client reads the same thing, with no update to
            // install and no server to restart. Point it somewhere else and this becomes a
            // message of the day for any server that wants one.
            Url = cfg.Bind("Kvedja", "Url", "https://longhouse.thijssensoftware.nl/api/motd",
                "Where the message is read from. Plain text, one line of chat per line. Empty "
                + "turns the mod off as surely as Enabled does. Any server can host its own: "
                + "the only requirement is that the answer is text.");

            Title = cfg.Bind("Kvedja", "Title", "Longhouse",
                "The name in front of the message, where a player's name normally goes. It is "
                + "drawn orange by the game, which is what makes the line read as somebody "
                + "speaking rather than as an error.");

            // Normal, and the two alternatives are worse in ways that are not obvious from the
            // name: Terminal.AddString UPPERCASES a Shout and lowercases a Whisper before it
            // draws either. A message of the day in block capitals is a mod shouting at
            // somebody who just sat down.
            Voice = cfg.Bind("Kvedja", "Voice", Talker.Type.Normal,
                "How the line is styled. Shout is drawn yellow AND IN CAPITALS, whisper is "
                + "faded and forced to lowercase; both are done by the game, not here. Normal "
                + "is the only one that prints what was written.");

            // Four seconds because the first second or two after spawning is the fade out of
            // the loading screen, and a line that arrives under it has been said to nobody.
            Delay = cfg.Bind("Kvedja", "Delay", 4f,
                "Seconds after you appear in the world before the message is said. Too early "
                + "and it lands under the loading fade, which looks exactly like it never "
                + "arrived.");

            Timeout = cfg.Bind("Kvedja", "Timeout", 8,
                "Seconds to wait for the site before giving up. Giving up is silent: a message "
                + "of the day that reports its own failure to a player is worse than no "
                + "message, because the player can do nothing about it.");

            // Off, because OnSpawned also fires when you get up at your bed after dying, and
            // being greeted at the moment you have just lost everything reads as mockery.
            Repeat = cfg.Bind("Kvedja", "Repeat", false,
                "On says the message again every time you respawn, not only when you log in. "
                + "Off is once per world session, which is what login means.");

            // Not synced by intent - see the plugin. A diagnostic flag is personal, and a host
            // turning on someone else's logging is not a thing anybody asked for.
            Verbose = cfg.Bind("Kvedja", "Verbose", false,
                "Write what was asked for and what came back to BepInEx/LogOutput.log. Off "
                + "unless the message is not appearing; it is three or four lines per login.");
        }
    }
}
