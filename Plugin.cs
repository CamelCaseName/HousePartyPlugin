﻿using System.Collections.Generic;
using MelonLoader;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

[assembly: HarmonyDontPatchAll]
[assembly: MelonInfo(typeof(HousePartyPlugin.Plugin), "House Party Compatibility Layer", "1.0.0", "Lenny")]
[assembly: MelonColor(255, 120, 20, 140)]
[assembly: MelonGame("Eek", "House Party")]
[assembly: VerifyLoaderVersion(0, 6, 1, true)]

namespace HousePartyPlugin
{
    public class Plugin : MelonPlugin
    {
        private readonly List<AssemblyLoadContext> contexts = new();

        static Plugin()
        {
            AppDomain.CurrentDomain.ResourceResolve += new(AssemblyResolveEventListener!);
            SetOurResolveHandlerAtFront();
            AppDomain.CurrentDomain.ResourceResolve -= new(AssemblyResolveEventListener!);
        }

        private static void SetOurResolveHandlerAtFront()
        {
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            FieldInfo? field = null;

            Type domainType = typeof(AssemblyLoadContext);

            while (field is null)
            {
                if (domainType is not null)
                {
                    field = domainType.GetField("AssemblyResolve", flags);
                }
                else
                {
                    MelonLogger.Error("domainType got set to null for the AssemblyResolve event was null");
                    return;
                }
                if (field is null)
                    domainType = domainType.BaseType!;
            }

            MulticastDelegate resolveDelegate = (MulticastDelegate)field.GetValue(null)!;
            Delegate[] subscribers = resolveDelegate.GetInvocationList();

            Delegate currentDelegate = resolveDelegate;
            for (int i = 0; i < subscribers.Length; i++)
                currentDelegate = Delegate.RemoveAll(currentDelegate, subscribers[i])!;

            Delegate[] newSubscriptions = new Delegate[subscribers.Length + 1];
            newSubscriptions[0] = (ResolveEventHandler)AssemblyResolveEventListener!;
            Array.Copy(subscribers, 0, newSubscriptions, 1, subscribers.Length);

            currentDelegate = Delegate.Combine(newSubscriptions)!;

            field.SetValue(null, currentDelegate);
        }

        private const string ForcedCpp2ILVersion = "2022.1.0-pre-release.12";

        public override void OnPreInitialization()
        {
            //inject a new unhollower version, old one doesnt work on house party
            ForceDumperVersion();
            MelonLogger.Msg($"Forced Cpp2Il version to {ForcedCpp2ILVersion}");
        }

        public override void OnPreSupportModule()
        {
            //patching il2cppinterop.runtime
            HarmonyInstance.PatchAll();
            //it does its logging in the transpiler, no need to spam one more Msg();

            //patching the il2cppinterop delegate converter
            DelegateConverterPatch.Apply(HarmonyInstance);
            //does its own logging

            //after this the new files are generated
            AppDomain.CurrentDomain.ResourceResolve -= new(AssemblyResolveEventListener!);

            foreach (var context in contexts)
            {
                MelonLogger.Msg("Unloading " + context.Name + "from our own context");
                context.Unload();
                MelonLogger.Msg("Loading " + context.Name + " into the default context");
                AssemblyLoadContext.Default.LoadFromAssemblyName(context.Assemblies.First().GetName());
            }

            //patch the scenehandler
            SceneHandlerPatch.Apply(HarmonyInstance);
            MelonLogger.Msg("Patched the SceneHandler");
        }

        private static void ForceDumperVersion()
        {
            PropertyInfo ForceDumperVersion = typeof(MelonLaunchOptions.Il2CppAssemblyGenerator).GetProperty("ForceVersion_Dumper")!;
            ForceDumperVersion.SetValue(null, ForcedCpp2ILVersion);
        }

        private static Assembly AssemblyResolveEventListener(object sender, ResolveEventArgs args)
        {
            if (args is null) return null!;

            var name = "HousePartyPlugin.Resources." + args.Name[..args.Name.IndexOf(',')] + ".dll";
            using Stream? str = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (str is not null)
            {
                var context = new AssemblyLoadContext(name, true);
                MelonLogger.Msg($"Loaded {args.Name} from our embedded resources");
                return context.LoadFromStream(str);
            }
            else
            {
                MelonLogger.Error($"Assembly {args.Name} not found in resources!");
                return null!;
            }
        }
    }
}