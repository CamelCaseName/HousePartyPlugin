using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Il2CppException = Il2CppInterop.Runtime.Il2CppException;

namespace HousePartyPlugin
{
    internal static class DelegateConverterPatch
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            MelonLogger.Msg("Patching Il2CppInterop.Runtime.DelegateSupport::ConvertDelegate<TIL2CPP>()");
            MethodBase convertDelegateBase = typeof(DelegateSupport).GetMethod("ConvertDelegate")!
                .MakeGenericMethod(typeof(Il2CppObjectBase));
            var transpilerMethod = typeof(DelegateSupport_ConvertDelegatePatch)
                .GetMethod(nameof(DelegateSupport_ConvertDelegatePatch.Transpiler));
            harmony.Patch(convertDelegateBase, null, null, new(transpilerMethod));

            MelonLogger.Msg("Patching Il2CppInterop.Runtime.DelegateSupport::GenerateNativeToManagedTrampoline()");
            MethodBase generateTrampolineBase = typeof(DelegateSupport).GetMethod("GenerateNativeToManagedTrampoline")!;
            var trampolineTranspilerMethod = typeof(DelegateSupport_GenerateNativeToManagedTrampolinePatch)
                .GetMethod(nameof(DelegateSupport_GenerateNativeToManagedTrampolinePatch.Transpiler));
            harmony.Patch(generateTrampolineBase, null, null, new(trampolineTranspilerMethod));

            MelonLogger.Msg("Patching Il2CppInterop.Runtime.DelegateSupport+MethodSignature::MethodSignature()");
            MethodBase MethodSignatureBase = MethodBase.GetMethodFromHandle(typeof(DelegateSupport).Assembly.GetType("Il2CppInterop.Runtime.DelegateSupport+MethodSignature")!
                .GetConstructor(new Type[] { typeof(Il2CppSystem.Reflection.MethodInfo), typeof(bool) })!.MethodHandle)!;
            var methodSignaturetranspilerMethod = typeof(DelegateSupport_MethodSignature_MethodSignature)
                .GetMethod(nameof(DelegateSupport_MethodSignature_MethodSignature.Transpiler));
            harmony.Patch(MethodSignatureBase, null, null, new(methodSignaturetranspilerMethod));
            MelonLogger.Msg("All Il2CppInterop DelegateSupport patches complete");
        }
    }

    public static class DelegateSupport_ConvertDelegatePatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);
            var getParameters = typeof(Il2CppSystem.Reflection.MethodInfo).GetMethod(nameof(Il2CppSystem.Reflection.MethodInfo.GetParameters));
            var getParametersInternal = typeof(Il2CppSystem.Reflection.MethodInfo).GetMethod(nameof(Il2CppSystem.Reflection.MethodInfo.GetParametersInternal));
            var getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));
            var from = typeof(Il2CppType).GetMethod(nameof(Il2CppType.From), new Type[] { typeof(Type) });
            var getMethodFix = typeof(DelegateConverterPatchHelpers).GetMethod(nameof(DelegateConverterPatchHelpers.GetMethodFix));
            var getFullNameFix = typeof(DelegateConverterPatchHelpers).GetMethod(nameof(DelegateConverterPatchHelpers.GetFullNameFix));
            var get_FullName = typeof(Il2CppSystem.Type).GetMethod("get_FullName", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            
            //patch getparameters to internal
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    if (((MethodBase)codes[i].operand!).FullDescription() == getParameters.FullDescription())
                    {
                        codes[i] = new CodeInstruction(OpCodes.Callvirt, getParametersInternal) { blocks = codes[i].blocks, labels = codes[i].labels };
                        MelonLogger.Msg("Patched DelegateSupport.ConvertDelegate to use the safe GetParametersInternal()");
                        break;
                    }
                }
            }

            //patch the getmethod for the nativedelegateinvokemethod
            for (int i = 0; i < codes.Count; i++)
            {
                //this field is only set once
                if (codes[i].opcode == OpCodes.Stloc_3)
                {
                    CodeInstruction ldtokenTIl2Cpp = null!;
                    foreach (var code in codes)
                    {
                        if (code.opcode == OpCodes.Ldtoken)
                        {
                            if ($"{code.operand}" == "Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase")
                            {
                                ldtokenTIl2Cpp = code;
                                break;
                            }
                        }
                    }
                    //patch in the new code (both 5 instructions long without the stloc.3)
                    //old: 
                    //var il2CppDelegateType = Il2CppSystem.Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(classTypePtr));
                    //var nativeDelegateInvokeMethod = il2CppDelegateType.GetMethod("Invoke");
                    //new:
                    //var nativeDelegateInvokeMethod = Il2CppType.From(typeof(TIl2Cpp)).GetMethodFix(managedInvokeMethod);
                    codes[i - 5] = new CodeInstruction(OpCodes.Ldtoken, ldtokenTIl2Cpp.operand) { blocks = codes[i - 5].blocks, labels = codes[i - 5].labels };
                    codes[i - 4] = new CodeInstruction(OpCodes.Call, getTypeFromHandle) { blocks = codes[i - 4].blocks, labels = codes[i - 4].labels };
                    codes[i - 3] = new CodeInstruction(OpCodes.Call, from) { blocks = codes[i - 3].blocks, labels = codes[i - 3].labels };
                    codes[i - 2] = new CodeInstruction(OpCodes.Ldloc_0) { blocks = codes[i - 2].blocks, labels = codes[i - 2].labels };
                    codes[i - 1] = new CodeInstruction(OpCodes.Call, getMethodFix) { blocks = codes[i - 1].blocks, labels = codes[i - 1].labels };
                    MelonLogger.Msg("Patched DelegateSupport.ConvertDelegate to use the fixed GetMethod");
                }
            }

            //patch the fullname getter to the il2cpp side method
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    //nativeType.FullName
                    if (((MethodBase)codes[i].operand!).FullDescription() == get_FullName.FullDescription())
                    {
                        codes[i] = new CodeInstruction(OpCodes.Call, getFullNameFix) { blocks = codes[i].blocks, labels = codes[i].labels };
                        MelonLogger.Msg("Patched DelegateSupport.ConvertDelegate to use the safe il2cpp internal way of getting the type name");
                        return codes.AsEnumerable();
                    }
                }
            }

            MelonLogger.Error("Couldn't patch DelegateSupport.ConvertDelegate");
            return instructions;
        }
    }

    public static class DelegateSupport_GenerateNativeToManagedTrampolinePatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var getParameters = typeof(Il2CppSystem.Reflection.MethodInfo).GetMethod(nameof(Il2CppSystem.Reflection.MethodInfo.GetParameters));
            var getParametersInternal = typeof(Il2CppSystem.Reflection.MethodInfo).GetMethod(nameof(Il2CppSystem.Reflection.MethodInfo.GetParametersInternal));

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    if (((MethodBase)codes[i].operand!).FullDescription() == getParameters.FullDescription())
                    {
                        codes[i] = new CodeInstruction(OpCodes.Callvirt, getParametersInternal) { blocks = codes[i].blocks, labels = codes[i].labels };
                        MelonLogger.Msg("Patched DelegateSupport.GenerateNativeToManagedTrampoline to use the safe GetParametersInternal()");
                        return codes.AsEnumerable();
                    }
                }
            }

            MelonLogger.Error("Couldn't patch DelegateSupport.GenerateNativeToManagedTrampoline");
            return codes.AsEnumerable();
        }
    }

    public static class DelegateSupport_MethodSignature_MethodSignature
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var getParameters = typeof(Il2CppSystem.Reflection.MethodInfo).GetMethod(nameof(Il2CppSystem.Reflection.MethodInfo.GetParameters));
            var getParametersInternal = typeof(Il2CppSystem.Reflection.MethodInfo).GetMethod(nameof(Il2CppSystem.Reflection.MethodInfo.GetParametersInternal));
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    if (((MethodBase)codes[i].operand!).FullDescription() == getParameters.FullDescription())
                    {
                        codes[i] = new CodeInstruction(OpCodes.Callvirt, getParametersInternal) { blocks = codes[i].blocks, labels = codes[i].labels };
                        MelonLogger.Msg("Patched the Il2CppSystem.Reflection.MethodInfo constructor to use the safe GetParametersInternal()");
                        return codes.AsEnumerable();
                    }
                }
            }
            MelonLogger.Error("Couldn't patch the Il2CppSystem.Reflection.MethodInfo constructor");
            return codes.AsEnumerable();
        }
    }

    public static class DelegateConverterPatchHelpers
    {
        public static unsafe Il2CppSystem.Reflection.MethodInfo GetMethodFix(this Il2CppSystem.Type type, MethodInfo predicate)
        {
            var nativeTypeClass = IL2CPP.il2cpp_object_get_class(type.Pointer);
            var nativeGetMethod = IL2CPP.il2cpp_class_get_method_from_name(nativeTypeClass, "GetMethod", 2);
            var exception = IntPtr.Zero;
            int flags = (int)(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            var parameters = stackalloc IntPtr[2];
            *parameters = IL2CPP.ManagedStringToIl2Cpp(predicate.Name);
            *(parameters + sizeof(IntPtr)) = (IntPtr)(&flags);
            var nativeMethodInfoObject = IL2CPP.il2cpp_runtime_invoke(nativeGetMethod, type.Pointer, (void**)parameters, ref exception);
            Il2CppException.RaiseExceptionIfNecessary(exception);
            if (nativeMethodInfoObject != IntPtr.Zero)

                return Il2CppObjectPool.Get<Il2CppSystem.Reflection.MethodInfo>(nativeMethodInfoObject);
            else
                throw new MissingMethodException("method was not found");
        }

        public static unsafe string GetFullNameFix(this Il2CppSystem.Type type)
        {
            return Marshal.PtrToStringAnsi(IL2CPP.il2cpp_type_get_name(type._impl.value))!;
        }
    }
}