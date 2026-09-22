using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Ezomic.Core;
using HarmonyLib;

namespace Kvedja
{
    /// <summary>
    /// Kvedja says a line in the chat window when you log in, read from the Longhouse site.
    ///
    /// A *kveðja* is a greeting. The reason for it is not the greeting: it is that there is a
    /// board on the site where players vote on what a mod should do next, and file bugs, and
    /// almost nobody knows it is there. A link in a README is read once, by the person
    /// installing, months before they have an opinion worth casting. A pinned Discord message
    /// is read by the people already in Discord. The moment somebody is actually in the game,
    /// having just been annoyed by something or having just thought of something, is the
    /// moment the address is worth having in front of them, and the chat window is where they
    /// are already looking.
    ///
    /// The text lives on the site rather than in this DLL, which is the whole design. It is
    /// changed in one place, with no update to install and no server to restart, and because
    /// the client fetches it directly it arrives in singleplayer and on any server, including
    /// while Longhouse itself is down.
    ///
    /// Client-side, and it has to be. A server cannot put a line in the chat window at all -
    /// the routed ChatMessage path ends in a lookup of the sender among the connected players
    /// and a dedicated server is not one of them. Crier spent two versions on that before the
    /// identity turned out to be the wrong thing to fix; see crier/src/Announce.cs, and see
    /// Motd for the overload that does work and why it can only be called here.
    ///
    /// There is deliberately no BepInProcess attribute. A dedicated server runs
    /// valheim_server.exe, and Core's gate only refuses on the server side of RPC_PeerInfo -
    /// so a mod that must be enforced has to be allowed to load there.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    // Soft, not hard. A hard dependency that is absent does not degrade - the plugin never
    // loads at all - and every mod here has to be installable on its own, because a stranger
    // should not need two installs to get one mod. Soft still buys the load-order guarantee
    // when Core is present, which is all that registering with the gate needs.
    [BepInDependency(CoreGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class KvedjaPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ezomic.valheim.kvedja";
        public const string PluginName = "Kvedja";
        public const string PluginVersion = "1.0.0";
        public const string PluginAuthor = "Robbin Thijssen";

        /// <summary>Core's plugin GUID. Optional - see TryRegisterWithCore.</summary>
        private const string CoreGuid = "ezomic.valheim.core";

        internal static ManualLogSource Log;

        /// <summary>
        /// Whether Core answered at load. Worth keeping even when nothing reads it yet: the
        /// difference between gated and ungated is invisible to a player otherwise, and this
        /// is what a warning on spawn would be driven by.
        /// </summary>
        internal static bool CorePresent;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            // Config first. Registering absorbs every entry the mod has bound, so anything
            // bound after this line is carried only because Core re-absorbs at manifest
            // time - and depending on the order of two lines in an Awake is not a thing
            // worth relying on.
            KvedjaConfig.Bind(Config);

            TryRegisterWithCore();

            // PatchAll over a named type, never the whole assembly. A bare PatchAll() walks
            // every type in the DLL, so a half-written patch class in another file goes live
            // the moment it compiles.
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(KvedjaPatches));

            // Every patch class needs its own line here, which is the cost of patching named
            // types rather than the assembly. Merki lost an afternoon to exactly this: a
            // console command written, compiled, deployed and never patched, so it did not
            // exist and the failure read as a typo in the caller.
            _harmony.PatchAll(typeof(Notices));

            // The startup line every mod in the suite writes. It is how a log answers "which
            // build of what is actually loaded" without anyone guessing.
            Log.LogInfo(PluginName + " " + PluginVersion + " by " + PluginAuthor + " - ready.");
        }

        /// <summary>
        /// Joins Core's version gate when Core is installed, and does nothing when it is not.
        ///
        /// Name here exactly what standing alone costs, because it is usually not the mod.
        /// For most of these it is the *enforcement*: without Core nothing refuses a client
        /// that lacks the plugin, so the rule becomes an agreement between players rather
        /// than a property of the server. That is a real loss and a legitimate choice, and
        /// it is the server owner's to make - which is why this logs rather than refusing
        /// to run.
        /// </summary>
        private void TryRegisterWithCore()
        {
            CorePresent = Chainloader.PluginInfos.ContainsKey(CoreGuid);

            if (!CorePresent)
            {
                Log.LogInfo("Core not installed - running standalone, without the version gate.");
                return;
            }

            RegisterWithCore();
        }

        /// <summary>
        /// Kept separate and never inlined on purpose. The JIT resolves the assemblies a
        /// method needs when it first compiles that method, so a Suite call sitting directly
        /// in Awake would drag Ezomic.Core in before the check above could prevent it - and
        /// the missing-assembly exception would land during plugin load, which is the exact
        /// failure this arrangement exists to avoid. Isolating it means the type is only
        /// ever resolved on a machine that has Core.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RegisterWithCore()
        {
            // HostOnly, and it is the whole of what this mod asks of a server: nothing. It
            // registers no prefab, writes no ZDO and changes no item - it reads a web page and
            // writes into its own chat log - so a client without it is genuinely unaffected
            // and simply hears nothing. Core honours that in both directions, which is the
            // half that had to be fixed for Skaft: a server without Kvedja still lets in a
            // client that has it, which matters here because the message is not the server's.
            Suite.Register(PluginGuid, PluginName, PluginVersion, Config, Requirement.HostOnly);

            // Every entry is Local, and the URL is the one worth arguing about. Letting a host
            // impose it would let any server point every guest's client at an address of its
            // choosing, and Core writes an imposed value into the guest's own cfg - so it
            // would still be there after they disconnected. A greeting is not worth that.
            Suite.Local(
                KvedjaConfig.Enabled,
                KvedjaConfig.Url,
                KvedjaConfig.Title,
                KvedjaConfig.Voice,
                KvedjaConfig.Delay,
                KvedjaConfig.Timeout,
                KvedjaConfig.Repeat,
                KvedjaConfig.Verbose);
        }

        private void OnDestroy()
        {
            // UnpatchSelf, never UnpatchAll(). The argumentless one unpatches every mod in
            // the process, not just this one.
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }
}
