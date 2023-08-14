using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HousePartyPlugin
{
    [HarmonyReversePatch]
    [HarmonyPatch("Melonloader.Support.SM_Component", "Create")]
    internal class SceneHandlerPatch_Component_Create
    {
        [HarmonyPrefix]
        public static GameObject SM_Component_Create(object instance)
        {
            // its a stub so it has no initial content
            throw new NotImplementedException("It's a stub");
        }
    }

    [HarmonyPatch("Melonloader.Support.SceneHandler", "OnSceneLoad")]
    internal class SceneHandlerPatch_OnSceneLoad
    {
        [HarmonyPrefix]
        private static bool OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            if (SceneSupport.Main_get_obj(null!) == null)
            {
                SceneHandlerPatch_Component_Create.SM_Component_Create(null!);
            }
            if ((Scene?)scene is null)
                return false;

            string name = scene.GetName();

            MelonDebug.Msg("scene " + name + " loaded as " + mode.ToString());
            SceneSupport.Main_get_interface().OnSceneWasLoaded(scene.buildIndex, name);
            SceneSupport.SceneHandler_sceneLoaded_Enqueue(SceneSupport.GetNewSceneInitEvent(scene.buildIndex, name));
            return false;
        }
    }

    [HarmonyPatch("Melonloader.Support.SceneHandler", "OnSceneUNload")]
    internal class SceneHandlerPatch_OnSceneUnload
    {
        [HarmonyPrefix]
        private static bool OnSceneUnload(Scene scene)
        {
            if ((Scene?)scene is null)
                return false;

            MelonDebug.Msg("scene unloaded");
            SceneSupport.Main_get_interface().OnSceneWasUnloaded(scene.buildIndex, scene.GetName());
            return false;
        }

    }
}
