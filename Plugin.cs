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
            MelonLogger.Msg("Patching the SceneHandler");
            PatchSceneHandler();
            //patching il2cppinterop.runtime
            MelonLogger.Msg("Unhooking the Hook on hkGenericMethodGetMethod from Il2CppInterop");
            HarmonyInstance.PatchAll();
            Unregister("All patches done, unregistered plugin");
        }

        private static void ForceDumperVersion()
        {
            PropertyInfo ForceDumperVersion = typeof(MelonLaunchOptions.Il2CppAssemblyGenerator).GetProperty("ForceVersion_Dumper")!;
            ForceDumperVersion.SetValue(null, ForcedCpp2ILVersion);
        }

        private void PatchSceneHandler()
        {
            var assembly = Assembly.Load(File.ReadAllBytes(".\\MelonLoader\\Dependencies\\SupportModules\\Il2Cpp.dll"));
            var type = assembly.GetType("MelonLoader.Support.SceneHandler")!;

            var loadType = typeof(SceneHandlerPatch_OnSceneLoad);
            var unloadType = typeof(SceneHandlerPatch_OnSceneUnload);

            HarmonyInstance.Patch(type.GetMethod("OnSceneLoad"), new(loadType.GetMethod("Prefix")));
            HarmonyInstance.Patch(type.GetMethod("OnSceneUnload"), new(unloadType.GetMethod("Prefix")));
        }
    }
}