using HarmonyLib;

namespace HousePartyPlugin
{
    [HarmonyPatch("Il2CppInterop.Runtime.Injection.InjectorHelpers", "Setup")]
    internal static class GenericMethod_GetMethod_HookPatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions;
        }
    }
}
