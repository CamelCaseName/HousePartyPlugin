using HarmonyLib;

namespace HousePartyPlugin
{
    [HarmonyPatch("Il2CppInterop.Runtime.Injection.Hooks.GenericMethod_GetMethod_Hook", "FindTargetMethod")]
    internal static class GenericMethod_GetMethod_HookPatch
    {
        [HarmonyPrefix]
        public static bool FindTargetMethod(ref IntPtr __result)
        {
            __result = (IntPtr)0;
            return false;
        }
    }
}
