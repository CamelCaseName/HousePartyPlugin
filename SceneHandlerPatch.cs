using MelonLoader;
using System.IO;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace HousePartyPlugin
{
    internal class SceneHandlerPatch_OnSceneLoad
    {
        public static bool Prefix(Scene scene, LoadSceneMode mode)
        {
            if (PluginSupport.Main_get_obj() is null)
            {
                PluginSupport.CreateMainGameObject();
            }

            if ((Scene?)scene is null)
                return false;

            string name = scene.GetName();
            MelonDebug.Msg(name + " loaded as " + mode.ToString());
            PluginSupport.Main_get_interface().OnSceneWasLoaded(scene.buildIndex, name);
            PluginSupport.SceneHandler_sceneLoaded_Enqueue(PluginSupport.GetNewSceneInitEvent(scene.buildIndex, name));

            return false;
        }
    }

    internal class SceneHandlerPatch_OnSceneUnload
    {
        public static bool Prefix(Scene scene)
        {
            if ((Scene?)scene is null)
                return false;

            string name = scene.GetName();
            MelonDebug.Msg(name + " unloaded");
            PluginSupport.Main_get_interface().OnSceneWasUnloaded(scene.buildIndex, name);

            return false;
        }

    }

    internal class SceneHandlerPatch
    {
        public static void ApplyPatch(HarmonyLib.Harmony harmony)
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
