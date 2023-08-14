using MelonLoader;
using UnityEngine.SceneManagement;

namespace HousePartyPlugin
{
    internal static class SceneHandlerPatch_OnSceneLoad
    {
        public static bool Prefix(Scene scene, LoadSceneMode mode)
        {
            if (SceneSupport.Main_get_obj() is null)
            {
                SceneSupport.CreateMainGameObject();
            }

            if ((Scene?)scene is null)
                return false;

            string name = scene.GetName();
            MelonDebug.Msg(name + " loaded as " + mode.ToString());
            SceneSupport.Main_get_interface().OnSceneWasLoaded(scene.buildIndex, name);
            SceneSupport.SceneHandler_sceneLoaded_Enqueue(SceneSupport.GetNewSceneInitEvent(scene.buildIndex, name));

            return false;
        }
    }

    internal static class SceneHandlerPatch_OnSceneUnload
    {
        public static bool Prefix(Scene scene)
        {
            if ((Scene?)scene is null)
                return false;

            string name = scene.GetName();
            MelonDebug.Msg(name + " unloaded");
            SceneSupport.Main_get_interface().OnSceneWasUnloaded(scene.buildIndex, name);

            return false;
        }

    }
}
