using HarmonyLib;
using Il2CppInterop.Runtime;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace HousePartyPlugin
{
    internal static class SupportModulePatch
    {
        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            Debugger.Break();
            MelonLogger.Msg("Patching the Il2Cpp Support Module");
            Type? type = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if ((type = asm.GetType("MelonLoader.Support.Main")) is not null)
                {
                    break;
                }
            }
            //harmony.Patch(type.GetMethod("ConsoleCleaner", BindingFlags.NonPublic | BindingFlags.Static), new(typeof(Il2CppSupportModulePatch_ConsoleCleaner).GetMethod(nameof(Il2CppSupportModulePatch_ConsoleCleaner.Prefix))));

            harmony.Patch(type!.GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Static), null, null, new(typeof(Il2CppSupportModulePatch_ConsoleCleaner).GetMethod(nameof(Il2CppSupportModulePatch_ConsoleCleaner.Transpiler))) { debug = true });

        }
    }

    internal static class Il2CppSupportModulePatch_ConsoleCleaner
    {
        public static unsafe bool Prefix()
        {
            Debugger.Break();
            //maybe just nop out the call to the cleaner in the setup?
            //patching the console cleaner seems to have no effect on the jit
            var console = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.Console")!;
            MelonLogger.Msg($"{console}");
            var handle = GCHandle.Alloc(console, GCHandleType.Pinned);
            MelonLogger.Msg($"{handle}");
            var nativeKlass = IL2CPP.il2cpp_class_from_system_type(handle.AddrOfPinnedObject());
            MelonLogger.Msg($"{nativeKlass}");
            var setOut = IL2CPP.il2cpp_class_get_method_from_name(nativeKlass, "SetOut", 1);
            MelonLogger.Msg($"{setOut}");

            var streamType = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.IO.Stream")!;
            MelonLogger.Msg($"{streamType}");
            var nullStreamGetter = streamType.GetMethod("get_Null", BindingFlags.NonPublic | BindingFlags.Instance)!;
            MelonLogger.Msg($"{nullStreamGetter}");
            var nullStream = nullStreamGetter.Invoke(null, null)!;
            MelonLogger.Msg($"{nullStream}");

            var streamWriterType = DelegateConverterPatch.Il2Cpp!.GetType("Il2CppSystem.IO.StreamWriter")!;
            MelonLogger.Msg($"{streamWriterType}");
            var streamWriterCtor = streamWriterType.GetConstructor(new[] { streamType })!;
            MelonLogger.Msg($"{streamWriterCtor}");
            var nullStreamWriter = streamWriterCtor.Invoke(new[] { nullStream })!;
            MelonLogger.Msg($"{nullStreamWriter}");
            var nullhandle = GCHandle.Alloc(nullStreamWriter, GCHandleType.Pinned);

            nint exception = 0;
            nint* parameters = stackalloc nint[1];
            *parameters = nullhandle.AddrOfPinnedObject();
            IL2CPP.il2cpp_runtime_invoke(setOut, handle.AddrOfPinnedObject(), (void**)parameters, ref exception);
            nullhandle.Free();
            handle.Free();
            Il2CppException.RaiseExceptionIfNecessary(exception);

            return true;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Debugger.Break();
            MelonLogger.Msg("Patched the ConsoleCleaner!");
            var codes = new List<CodeInstruction>(instructions);
            var ConsoleCleaner = Type.GetType("MelonLoader.Support.Main")!.GetMethod("ConsoleCleaner", BindingFlags.NonPublic | BindingFlags.Static)!;
            var testMethod = typeof(Il2CppSupportModulePatch_ConsoleCleaner).GetMethod(nameof(Il2CppSupportModulePatch_ConsoleCleaner.Test));

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Call && codes[i].operand is not null)
                {
                    if (codes[i].operand.ToString() == ConsoleCleaner.ToString())
                    {
                        codes[i] = new CodeInstruction(OpCodes.Call, testMethod);
                        MelonLogger.Msg("[House_Party_Compatibility_Layer] Test patch successfull");
                        return codes.AsEnumerable();
                    }
                }
            }

            return instructions;
        }

        public static void Test()
        {
            MelonLogger.Msg("Test, patch successfull");
        }
    }
}