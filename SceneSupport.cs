using Il2CppInterop.Runtime;
using MelonLoader;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HousePartyPlugin
{
    internal static class SceneSupport
    {
        public static unsafe string GetName(this Scene scene)
        {
            var nativeSceneClass = Il2CppClassPointerStore.GetNativeClassPointer(typeof(Scene));
            if (nativeSceneClass == IntPtr.Zero)
            {
                MelonDebug.Error("scene.get_name is missing and the workaround failed (class pointer was zero)");
                return "scene names stripped";
            }
            var nativeMethod = IL2CPP.il2cpp_class_get_method_from_name(nativeSceneClass, "get_name", 0);
            if (nativeMethod == IntPtr.Zero)
            {
                MelonDebug.Error("scene.get_name is missing and the workaround failed, all workarounds failed (method pointer for Scene.get_name() was zero)");
                return "scene names stripped";
            }
            IntPtr* ptr = null;
            IntPtr error = IntPtr.Zero;
            var sceneHandle = GCHandle.Alloc(scene.handle, GCHandleType.Pinned);
            if (sceneHandle.AddrOfPinnedObject() == IntPtr.Zero)
            {
                MelonDebug.Error("scene couldn't be pinned");
                return "scene names stripped";
            }
            var nativeResult = IL2CPP.il2cpp_runtime_invoke(nativeMethod, sceneHandle.AddrOfPinnedObject(), (void**)ptr, ref error);
            sceneHandle.Free();
            if (nativeResult == IntPtr.Zero)
            {
                MelonDebug.Error("il2cpp_runtime_invoke on the native Scene.get_name failed (result pointer was zero)");
                return "scene names stripped";
            }
            Il2CppException.RaiseExceptionIfNecessary(error);
            return IL2CPP.Il2CppStringToManaged(nativeResult)!;
        }

        public static ISupportModule_From Main_get_interface()
        {
            var assembly = Assembly.GetAssembly(typeof(MelonPlugin))!;
            var type = assembly.GetType("Melonloader.Suppport.Main")!;
            var field = type.GetField("Interface")!;
            return (field.GetValue(null) as ISupportModule_From)!;
        }

        public static GameObject Main_get_obj(object instance)
        {
            var assembly = Assembly.GetAssembly(typeof(MelonPlugin))!;
            var type = assembly.GetType("Melonloader.Suppport.Main")!;
            var field = type.GetField("obj")!;
            return (field.GetValue(null) as GameObject)!;
        }

        public static void SceneHandler_sceneLoaded_Enqueue(object sceneInitEvent)
        {
            var assembly = Assembly.GetAssembly(typeof(MelonPlugin))!;
            var type = assembly.GetType("Melonloader.Suppport.SceneHandler")!;
            var field = type.GetField("scenesLoaded")!;
            var queue = field.GetValue(null)!;
            var queueType = queue.GetType();
            var enqueueMethod = queueType.GetMethod("Enqueue")!;
            enqueueMethod.Invoke(queue, new object[] { sceneInitEvent });
        }

        public static object GetNewSceneInitEvent(int buildIndex, string name)
        {
            var assembly = Assembly.GetAssembly(typeof(MelonPlugin))!;
            var type = assembly.GetType("Melonloader.Suppport.SceneHandler.SceneInitEvent")!;
            var constructor = type.GetConstructor(Array.Empty<Type>())!;
            var obj = constructor.Invoke(null);
            var nameField = type.GetField("name")!;
            nameField.SetValue(obj, name);
            var indexField = type.GetField("buildIndex")!;
            indexField.SetValue(obj, buildIndex);
            return obj;
        }
    }
}