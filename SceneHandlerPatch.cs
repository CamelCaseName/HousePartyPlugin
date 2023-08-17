using MelonLoader;
using System;
using System.IO;
using System.Reflection;

namespace HousePartyPlugin
{
    internal class SceneHandlerPatch_OnSceneLoad
    {
        public static bool Prefix(object scene, object mode)
        {
            if (PluginSupport.Main_get_obj() is null)
            {
                PluginSupport.CreateMainGameObject();
            }

            if (scene is null)
                return false;

            string name = scene.GetName();
            MelonLogger.Msg($"{name} loaded (mode: {mode})");
            var buildIndex = Type.GetType("UnityEngine.SceneManagement.Scene")!.GetMethod("get_buildIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
            int index = (int)buildIndex.Invoke(scene, null)!;
            PluginSupport.Main_get_interface().OnSceneWasLoaded(index, name);
            PluginSupport.SceneHandler_sceneLoaded_Enqueue(PluginSupport.GetNewSceneInitEvent(index, name));

            return false;
        }
    }

    internal class SceneHandlerPatch_OnSceneUnload
    {
        public static bool Prefix(object scene)
        {
            if (scene is null)
                return false;

            string name = scene.GetName();
            MelonLogger.Msg($"{name} unloaded");
            var buildIndex = Type.GetType("UnityEngine.SceneManagement.Scene")!.GetMethod("get_buildIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
            PluginSupport.Main_get_interface().OnSceneWasUnloaded((int)buildIndex.Invoke(scene, null)!, name);

            return false;
        }
    }

    internal class SceneHandlerPatch
    {
        public static void Apply(HarmonyLib.Harmony harmony)
        {
            var assembly = Assembly.Load(File.ReadAllBytes(".\\MelonLoader\\Dependencies\\SupportModules\\Il2Cpp.dll"));
            var type = assembly.GetType("MelonLoader.Support.SceneHandler")!;

            var loadType = typeof(SceneHandlerPatch_OnSceneLoad);
            var unloadType = typeof(SceneHandlerPatch_OnSceneUnload);

            harmony.Patch(type.GetMethod("OnSceneLoad"), new(loadType.GetMethod("Prefix")));
            harmony.Patch(type.GetMethod("OnSceneUnload"), new(unloadType.GetMethod("Prefix")));
        }
    }
}
