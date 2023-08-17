using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace HousePartyPlugin
{
    [HarmonyPatch("Il2CppInterop.Runtime.Injection.InjectorHelpers", "Setup")]
    internal static class GenericMethod_GetMethod_HookPatch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;
            var codes = new List<CodeInstruction>(instructions);
            if (typeof(ClassInjector).Assembly.GetType("Il2CppInterop.Runtime.Injection.Hook`1") is null)
            {
                //old il2cppinterop pre commit a23fe71 => https://github.com/BepInEx/Il2CppInterop/commit/a24fe7166ebb0c8afb41bc04d3df673627b4350f

                MelonLogger.Msg("Patching the Il2CppInterop.Runtime.Injection.InjectorHelpers::Setup()");
                var InjectorHelpersType = typeof(ClassInjector).Assembly.GetType("Il2CppInterop.Runtime.Injection.InjectorHelpers")!;
                var GenericMethodGetMethod = InjectorHelpersType.GetField("GenericMethodGetMethod", BindingFlags.Static | BindingFlags.NonPublic)!;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].LoadsField(GenericMethodGetMethod))
                    {
                        if (codes[i + 1].Branches(out var _))
                        {
                            if (codes[i + 3].StoresField(GenericMethodGetMethod))
                            {
                                codes[i].opcode = OpCodes.Nop;
                                codes[i + 1].opcode = OpCodes.Nop;
                                codes[i + 2].opcode = OpCodes.Nop;
                                codes[i + 3].opcode = OpCodes.Nop;
                                patched = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                MelonLogger.Msg("Patching the new Il2CppInterop.Runtime.Injection.InjectorHelpers::Setup()");
                var InjectorHelpersType = typeof(ClassInjector).Assembly.GetType("Il2CppInterop.Runtime.Injection.InjectorHelpers")!;
                var GenericMethodGetMethod = InjectorHelpersType.GetField("GenericMethodGetMethodHook", BindingFlags.Static | BindingFlags.NonPublic)!;
                var GetMethodHookType = typeof(ClassInjector).Assembly.GetType("Il2CppInterop.Runtime.Injection.Hooks.GenericMethod_GetMethod_Hook")!;
                var applyMethod = GetMethodHookType.BaseType!.GetMethod("ApplyHook")!;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].LoadsField(GenericMethodGetMethod))
                    {
                        if (codes[i + 1].Calls(applyMethod))
                        {
                            codes[i].opcode = OpCodes.Nop;
                            codes[i + 1].opcode = OpCodes.Nop;
                            patched = true;
                            break;
                        }
                    }
                }
            }
            if (!patched)
                MelonLogger.Msg("Couldnt patch the Il2CppInterop InjectorHelpers");
            else
                MelonLogger.Msg("Patched the Il2CppInterop.Runtime.Injection.InjectorHelpers::Setup()");

            return codes.AsEnumerable();
        }
    }
}
