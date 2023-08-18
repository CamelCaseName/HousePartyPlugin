using HarmonyLib;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HousePartyPlugin
{
    internal class SceneHandlerPatch
    {
        public static void Apply(HarmonyLib.Harmony harmony)
        {
            var type = Type.GetType("MelonLoader.Support.SceneHandler")!;
            MelonLogger.Msg("Patching the SceneHandler");

            harmony.Patch(type.GetMethod("OnSceneLoad"), null, null, new(typeof(SceneHandlerPatch_OnSceneLoad).GetMethod(nameof(SceneHandlerPatch_OnSceneLoad.Transpiler))) { debug = true });
            harmony.Patch(type.GetMethod("OnSceneUnload"), null, null, new(typeof(SceneHandlerPatch_OnSceneUnload).GetMethod(nameof(SceneHandlerPatch_OnSceneUnload.Transpiler))) { debug = true });
            
        }
    }

    internal static class SceneHandlerPatch_OnSceneLoad
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var codes = new List<CodeInstruction>(instructions);
                var getName = Type.GetType("UnityEngine.SceneManagement.Scene")!.GetMethod("get_name", BindingFlags.NonPublic | BindingFlags.Instance)!;
                var getNameFix = typeof(SceneSupport).GetMethod(nameof(SceneSupport.GetName));

                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].Calls(getName))
                    {
                        codes[i] = new CodeInstruction(OpCodes.Callvirt, getNameFix) { blocks = codes[i].blocks, labels = codes[i].labels };
                    }
                }

                MelonLogger.Msg("[House_Party_Compatibility_Layer] Patched the Scene.Name to a safe Scene.GetName()");
                return codes.AsEnumerable();
            }
            catch
            {
                MelonLogger.Error("[House_Party_Compatibility_Layer] Couldn't patch DelegateSupport.GenerateNativeToManagedTrampoline");
                throw;
            }
        }
    }

    internal static class SceneHandlerPatch_OnSceneUnload
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var codes = new List<CodeInstruction>(instructions);
                var getName = Type.GetType("UnityEngine.SceneManagement.Scene")!.GetMethod("get_name", BindingFlags.NonPublic | BindingFlags.Instance)!;
                var getNameFix = typeof(SceneSupport).GetMethod(nameof(SceneSupport.GetName));

                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].Calls(getName))
                    {
                        codes[i] = new CodeInstruction(OpCodes.Callvirt, getNameFix) { blocks = codes[i].blocks, labels = codes[i].labels };
                    }
                }

                MelonLogger.Msg("[House_Party_Compatibility_Layer] Patched the Scene.Name to a safe Scene.GetName()");
                return codes.AsEnumerable();
            }
            catch
            {
                MelonLogger.Error("[House_Party_Compatibility_Layer] Couldn't patch DelegateSupport.GenerateNativeToManagedTrampoline");
                throw;
            }
        }
    }
}
