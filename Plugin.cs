using MelonLoader;
using System.Reflection;

[assembly: HarmonyDontPatchAll]
[assembly: MelonInfo(typeof(HousePartyPlugin.Plugin), "House Party Compatibility Layer", "1.0.0", "Lenny")]
[assembly: MelonColor(255, 120, 20, 140)]

namespace HousePartyPlugin
{
    public class Plugin : MelonPlugin
    {
        private const string ForcedCpp2ILVersion = "2022.1.0-pre-release.12";

        public override void OnPreInitialization()
        {
            //inject a new unhollower version, old one doesnt work on house party
            ForceDumperVersion();
            MelonLogger.Msg($"Forced Cpp2Il version to {ForcedCpp2ILVersion}");

            //patch the scenehandler
            MelonLogger.Msg("Patching the SceneHandler and unhooking the Hook on hk_GetGenericMethodGetMethod from Il2CppInterop");
            HarmonyInstance.PatchAll();
            MelonLogger.Msg("All patches done, unregistering plugin");
            Unregister("Compatiblity set up");
        }

        private static void ForceDumperVersion()
        {
            PropertyInfo ForceDumperVersion = typeof(MelonLaunchOptions.Il2CppAssemblyGenerator).GetProperty("ForceVersion_Dumper")!;
            ForceDumperVersion.SetValue(null, ForcedCpp2ILVersion);
        }
    }
}