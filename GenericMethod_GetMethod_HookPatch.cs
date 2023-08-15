using HarmonyLib;
using System;
using System.Collections.Generic;

namespace HousePartyPlugin
{
    [HarmonyPatch("Il2CppInterop.Runtime.Injection.InjectorHelpers", "Setup")]
    internal static class GenericMethod_GetMethod_HookPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            if (Type.GetType("Il2CppInterop.Runtime.Injection.Hooks.GenericMethod_GetMethod_Hook") is null)
            {
                //old il2cppinterop pre commit a23fe71 => https://github.com/BepInEx/Il2CppInterop/commit/a24fe7166ebb0c8afb41bc04d3df673627b4350f
            }
            else
            {
                //new il2cppinterop with special class per hook, see latest?
            }
            return instructions;
        }
    }
}
