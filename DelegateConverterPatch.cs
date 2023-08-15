using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Il2CppException = Il2CppInterop.Runtime.Il2CppException;

namespace HousePartyPlugin
{
    internal static class DelegateConverterPatch
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            MelonLogger.Msg("Patching Il2CppInterop.Runtime.DelegateSupport::ConvertDelegate<TIL2CPP>()");
            var methodBase = typeof(DelegateSupport).GetMethod("ConvertDelegate")!
                .MakeGenericMethod(typeof(Il2CppObjectBase));
            var transpilerMethod = typeof(DelegateSupport_ConvertDelegatePatch)
                .GetMethod(nameof(DelegateSupport_ConvertDelegatePatch.Transpiler));
            harmony.Patch(methodBase, null, null, new(transpilerMethod));

            MelonLogger.Msg("Patching Il2CppInterop.Runtime.DelegateSupport::GenerateNativeToManagedTrampoline()");
            methodBase = typeof(DelegateSupport).GetMethod("GenerateNativeToManagedTrampoline");
            transpilerMethod = typeof(DelegateSupport_GenerateNativeToManagedTrampolinePatch)
                .GetMethod(nameof(DelegateSupport_GenerateNativeToManagedTrampolinePatch.Transpiler));
            harmony.Patch(methodBase, null, null, new(transpilerMethod));

            MelonLogger.Msg("Patching Il2CppInterop.Runtime.DelegateSupport+MethodSignature::MethodSignature()");
            foreach (var item in typeof(DelegateSupport).Assembly.GetType("Il2CppInterop.Runtime.DelegateSupport+MethodSignature")!.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.InvokeMethod))
            {
                MelonLogger.Msg(item.FullDescription());
            }
            methodBase = typeof(DelegateSupport).Assembly.GetType("Il2CppInterop.Runtime.DelegateSupport+MethodSignature")!
                .GetMethod(".ctor", new Type[] { typeof(Il2CppSystem.Reflection.MethodInfo), typeof(bool) });
            MelonLogger.Msg(methodBase);
            transpilerMethod = typeof(DelegateSupport_MethodSignature_MethodSignature)
                .GetMethod(nameof(DelegateSupport_MethodSignature_MethodSignature.Transpiler));
            harmony.Patch(methodBase, null, null, new(transpilerMethod));
            MelonLogger.Msg("All Il2CppInterop DelegateSupport patches complete");
        }
    }

    public static class DelegateSupport_ConvertDelegatePatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);



            return codes.AsEnumerable();
        }
    }

    public static class DelegateSupport_GenerateNativeToManagedTrampolinePatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);



            return codes.AsEnumerable();
        }
    }

    public static class DelegateSupport_MethodSignature_MethodSignature
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);



            return codes.AsEnumerable();
        }
    }

    public static class DelegateConverterPatchHelpers
    {
        public static unsafe Il2CppSystem.Reflection.MethodInfo GetMethodFix(this Il2CppSystem.Type type, MethodInfo predicate)
        {
            var nativeTypeClass = IL2CPP.il2cpp_object_get_class(type.Pointer);
            var nativeGetMethod = IL2CPP.il2cpp_class_get_method_from_name(nativeTypeClass, "GetMethod", 1);
            var exception = IntPtr.Zero;

            var parameters = stackalloc IntPtr[1];
            *parameters = IL2CPP.ManagedStringToIl2Cpp(predicate.Name);
            var nativeMethodInfoObject = IL2CPP.il2cpp_runtime_invoke(nativeGetMethod, type.Pointer, (void**)parameters, ref exception);
            Il2CppException.RaiseExceptionIfNecessary(exception);
            if (nativeMethodInfoObject != IntPtr.Zero)

                return Il2CppObjectPool.Get<Il2CppSystem.Reflection.MethodInfo>(nativeMethodInfoObject);
            else
                throw new MissingMethodException("method was not found");
        }
    }
}