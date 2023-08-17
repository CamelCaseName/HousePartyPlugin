using Il2CppInterop.Runtime;
using MelonLoader;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HousePartyPlugin
{
    internal static class SceneSupport
    {
        /// <summary>
        /// only call on UnityEngine.SceneManagement.Scene object!
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static unsafe string GetName(this object scene)
        {
            if (scene.GetType().ToString() != Type.GetType("UnityEngine.SceneManagement.Scene")!.ToString())
            {
                throw new ArgumentException("Type mismatch in GetName extension method for unity scene");
            }
            var nativeSceneClass = Il2CppClassPointerStore.GetNativeClassPointer(Type.GetType("UnityEngine.SceneManagement.Scene"));
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
            var getHandle = Type.GetType("UnityEngine.SceneManagement.Scene")!.GetMethod("get_handle", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var sceneHandle = GCHandle.Alloc(getHandle.Invoke(scene, null)!, GCHandleType.Pinned);
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
    }

    internal class PluginSupport
    {

        public static ISupportModule_From Main_get_interface()
        {
            var assembly = Assembly.GetAssembly(typeof(MelonPlugin))!;
            var type = assembly.GetType("Melonloader.Suppport.Main")!;
            var field = type.GetField("Interface")!;
            return (field.GetValue(null) as ISupportModule_From)!;
        }

        public static object Main_get_obj()
        {
            var assembly = Assembly.GetAssembly(typeof(MelonPlugin))!;
            var type = assembly.GetType("Melonloader.Suppport.Main")!;
            var field = type.GetField("obj")!;
            return field.GetValue(null)!;
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
        public static object CreateMainGameObject()
        {
            var type = Type.GetType("Melonloader.Support.SM_Component")!;
            var method = type.GetMethod("CreateMainGameObject")!;
            return method?.Invoke(null, Array.Empty<object>())!;
        }
    }
}