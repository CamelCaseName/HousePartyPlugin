﻿using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HousePartyPlugin
{
    internal static class DelegateConverterPatch
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            var methodBase = typeof(DelegateSupport).GetMethod(nameof(DelegateSupport.ConvertDelegate));
            var transpilerMethod = typeof(DelegateSupportPatch).GetMethod(nameof(DelegateSupportPatch.Transpiler));
            harmony.Patch(methodBase, null, null, new(transpilerMethod));
        }
    }

    internal static class DelegateSupportPatch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);



            return codes.AsEnumerable();
        }
    }

    internal static class DelegateConverterPatchHelpers
    {
        internal static unsafe Il2CppSystem.Reflection.MethodInfo GetMethodFix(this Il2CppSystem.Type type, MethodInfo predicate)
        {
            var nativeTypeClass = IL2CPP.il2cpp_object_get_class(type.Pointer);
            var nativeGetMethod = IL2CPP.il2cpp_class_get_method_from_name(nativeTypeClass, "GetMethod", 1);
            var exception = IntPtr.Zero;

            var parameters = stackalloc IntPtr[1];
            *parameters = IL2CPP.ManagedStringToIl2Cpp(predicate.Name);
            var nativeMethodInfoObject = IL2CPP.il2cpp_runtime_invoke(nativeGetMethod, type.Pointer, (void**)parameters, ref exception);
            Il2CppException.RaiseExceptionIfNecessary(exception);
            if (nativeMethodInfoObject != IntPtr.Zero)
            {
                var CreateUnsafeMethod = typeof(Il2CppObjectBase).GetMethod("CreateUnsafe`1", BindingFlags.Static | BindingFlags.NonPublic)!;
                CreateUnsafeMethod.MakeGenericMethod(typeof(Il2CppSystem.Reflection.MethodInfo));
                return (Il2CppSystem.Reflection.MethodInfo)(Il2CppObjectBase)CreateUnsafeMethod.Invoke(null, new object[] { nativeMethodInfoObject })!;
            }
            else
                throw new MissingMethodException("method was not found");
        }
    }
}