using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace HousePartyPlugin
{
    internal static class DelegateConverterPatch
    {
        public static Assembly? Il2Cpp;
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            //loading the il2cpp
            var asmLoadContext = new AssemblyLoadContext("DelegateConverterPatchContext", false);
            Il2Cpp = asmLoadContext.LoadFromAssemblyPath($"{Assembly.GetExecutingAssembly().Location[..^"Plugins\\HousePartyPlugin.dll".Length]}MelonLoader\\Il2CppAssemblies\\Il2Cppmscorlib.dll")!;

            MelonLogger.Msg("Patching Il2CppInterop.Runtime.DelegateSupport::ConvertDelegate<TIL2CPP>()");
            MethodBase convertDelegateBase = typeof(DelegateSupport).GetMethod("ConvertDelegate")!
                .MakeGenericMethod(Il2Cpp!.GetType("Il2CppSystem.Delegate")!);
            var transpilerMethod = typeof(DelegateSupport_ConvertDelegatePatch)
                .GetMethod(nameof(DelegateSupport_ConvertDelegatePatch.Transpiler));
            harmony.Patch(convertDelegateBase, null, null, new(transpilerMethod));

            MelonLogger.Msg("Patching Il2CppInterop.Runtime.DelegateSupport::GenerateNativeToManagedTrampoline()");
            MethodBase generateTrampolineBase = typeof(DelegateSupport).GetMethod("GenerateNativeToManagedTrampoline")!;
            var trampolineTranspilerMethod = typeof(DelegateSupport_GenerateNativeToManagedTrampolinePatch)
                .GetMethod(nameof(DelegateSupport_GenerateNativeToManagedTrampolinePatch.Transpiler));
            harmony.Patch(generateTrampolineBase, null, null, new(trampolineTranspilerMethod));

            MelonLogger.Msg("Patching Il2CppInterop.Runtime.DelegateSupport+MethodSignature::MethodSignature()");
            MethodBase MethodSignatureBase = null!;
            foreach (var item in typeof(DelegateSupport).Assembly.GetType("Il2CppInterop.Runtime.DelegateSupport+MethodSignature")!.GetConstructors())
            {
                if (item.GetParameters()[0].ParameterType.ToString() == Il2Cpp.GetType("Il2CppSystem.Reflection.MethodInfo")!.ToString())
                {
                    MethodSignatureBase = MethodBase.GetMethodFromHandle(item.MethodHandle)!;
                }
            }
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
            var getParameters = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Reflection.MethodInfo")!.GetMethod("GetParameters");
            var getParametersInternal = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Reflection.MethodInfo")!.GetMethod("GetParametersInternal");
            var getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));
            var from = typeof(Il2CppType).GetMethod(nameof(Il2CppType.From), new[] { typeof(Type) });
            var getMethodFix = typeof(DelegateConverterPatchSupport).GetMethod(nameof(DelegateConverterPatchSupport.GetMethodFix));
            var FullNameFix = typeof(DelegateConverterPatchSupport).GetMethod(nameof(DelegateConverterPatchSupport.FullNameFix));
            var get_FullName = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Type")!.GetMethod("get_FullName", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);

            //patch getparameters to internal
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    if (((MethodBase)codes[i].operand!).FullDescription() == getParameters.FullDescription())
                    {
                        codes[i] = new CodeInstruction(OpCodes.Callvirt, getParametersInternal) { blocks = codes[i].blocks, labels = codes[i].labels };
                        MelonLogger.Msg("[House_Party_Compatibility_Layer] Patched DelegateSupport.ConvertDelegate to use the safe GetParametersInternal()");
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
                    MelonLogger.Msg("[House_Party_Compatibility_Layer] Patched DelegateSupport.ConvertDelegate to use the fixed GetMethod");
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
                        codes[i] = new CodeInstruction(OpCodes.Call, FullNameFix) { blocks = codes[i].blocks, labels = codes[i].labels };
                        MelonLogger.Msg("[House_Party_Compatibility_Layer] Patched DelegateSupport.ConvertDelegate to use the safe il2cpp internal way of getting the type name");
                        return codes.AsEnumerable();
                    }
                }
            }

            MelonLogger.Error("[House_Party_Compatibility_Layer] Couldn't patch DelegateSupport.ConvertDelegate");
            return instructions;
        }
    }

    public static class DelegateSupport_GenerateNativeToManagedTrampolinePatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var getParameters = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Reflection.MethodInfo")!.GetMethod("GetParameters");
            var getParametersInternal = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Reflection.MethodInfo")!.GetMethod("GetParametersInternal");

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    if (((MethodBase)codes[i].operand!).FullDescription() == getParameters.FullDescription())
                    {
                        codes[i] = new CodeInstruction(OpCodes.Callvirt, getParametersInternal) { blocks = codes[i].blocks, labels = codes[i].labels };
                        MelonLogger.Msg("[House_Party_Compatibility_Layer] Patched DelegateSupport.GenerateNativeToManagedTrampoline to use the safe GetParametersInternal()");
                        return codes.AsEnumerable();
                    }
                }
            }

            MelonLogger.Error("[House_Party_Compatibility_Layer] Couldn't patch DelegateSupport.GenerateNativeToManagedTrampoline");
            return codes.AsEnumerable();
        }
    }

    public static class DelegateSupport_MethodSignature_MethodSignature
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var getParameters = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Reflection.MethodInfo")!.GetMethod("GetParameters");
            var getParametersInternal = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Reflection.MethodInfo")!.GetMethod("GetParametersInternal");
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    if (((MethodBase)codes[i].operand!).ToString() == getParameters!.ToString())
                    {
                        codes[i] = new CodeInstruction(OpCodes.Callvirt, getParametersInternal) { blocks = codes[i].blocks, labels = codes[i].labels };
                        MelonLogger.Msg("[House_Party_Compatibility_Layer] Patched the Il2CppSystem.Reflection.MethodInfo constructor to use the safe GetParametersInternal()");
                        return codes.AsEnumerable();
                    }
                }
            }
            MelonLogger.Error("[House_Party_Compatibility_Layer] Couldn't patch the Il2CppSystem.Reflection.MethodInfo constructor");
            return codes.AsEnumerable();
        }
    }
}