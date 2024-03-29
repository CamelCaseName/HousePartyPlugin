﻿using AssetRipper.VersionUtilities;
using Il2CppInterop.Runtime.Runtime;
using MelonLoader;
using MelonLoader.InternalUtils;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

[assembly: HarmonyDontPatchAll]
[assembly: MelonInfo(typeof(HousePartyPlugin.Plugin), "House Party Compatibility Layer", "1.0.2", "Lenny")]
[assembly: MelonColor(255, 120, 20, 140)]
[assembly: MelonGame("Eek", "House Party")]
[assembly: VerifyLoaderVersion(0, 6, 1, true)]

namespace HousePartyPlugin
{
    public class Plugin : MelonPlugin
    {
        static Plugin()
        {
            SetOurResolveHandlerAtFront();
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
        private const string ForcedUnityVersion = "2022.3.13";

        public override void OnPreInitialization()
        {
            if (new Version(BuildInfo.Version) > new Version(0, 6, 1))
            {
                MelonLogger.Error("This plugin is no longer needed, you can safely remove it!");
            }
            //inject a new unhollower version, old one doesnt work on house party
            ForceDumperVersion();
            MelonLogger.Msg($"Forced Cpp2Il version to {ForcedCpp2ILVersion}");
            MelonLogger.Msg($"Forced Unity Runtime version to {ForcedUnityVersion}");
        }

        public override void OnPreModsLoaded()
        {
            //patching il2cppinterop.runtime
            HarmonyInstance.PatchAll();
        }

        private static void ForceDumperVersion()
        {
            PropertyInfo ForceDumperVersion = typeof(MelonLaunchOptions.Il2CppAssemblyGenerator).GetProperty("ForceVersion_Dumper")!;
            ForceDumperVersion.SetValue(null, ForcedCpp2ILVersion);
            
            PropertyInfo setEngineVersion = typeof(UnityInformationHandler).GetProperty("EngineVersion")!;
            setEngineVersion.SetValue(null, UnityVersion.Parse(ForcedUnityVersion));
        }

        private static Assembly AssemblyResolveEventListener(object sender, ResolveEventArgs args)
        {
            if (args is null) return null!;

            var name = "HousePartyPlugin.Resources." + args.Name[..args.Name.IndexOf(',')] + ".dll";
            using Stream? str = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (str is not null)
            {
                var context = new AssemblyLoadContext(name, false);
                MelonLogger.Msg($"Loaded {args.Name} from our embedded resources");
                return context.LoadFromStream(str);
            }
            else
            {
                return null!;
            }
        }
    }
}