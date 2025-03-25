using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RoslynCSharp;
using RoslynCSharp.Compiler;
using XRL;
using HarmonyLib;

namespace XUnity.AutoTranslator.Plugin.Qud
{
    [HasModSensitiveStaticCache]
    public static class Bootstrap
    {
        public static void InitalizeDefaults()
        {
            var dataPath = DataManager.LocalPath("AutoTranslator");
            if (!Directory.Exists(dataPath))
            {
                MetricsManager.LogInfo($"Creating default data in {dataPath}");
                Directory.CreateDirectory(dataPath);
                var original = Path.Combine(ModManager.GetMod().Path, "defaults", "AutoTranslatorConfig.ini");
                File.Copy(original, Path.Combine(dataPath, "AutoTranslatorConfig.ini"));
            }
        }

        [ModSensitiveCacheInit]
        public static void Inject()
        {
            MetricsManager.LogInfo($"XUnity.AutoTranslator Bootstrap Starting");
            InitalizeDefaults();
            // This is some truely cursed magic happening....
            try {
                var info = ModManager.GetMod();
                var Loadables = Directory.EnumerateFiles(Path.Combine(info.Path, "Loadables"), "*.*", SearchOption.AllDirectories)
                    .Where(file => !file.Contains("Translators"))
                    .Where(file => file.EndsWith(".dll") || file.EndsWith(".exe"));
                var XUnityDomain = ScriptDomain.CreateDomain("XUnity.AutoTranslator");
                var service = XUnityDomain.RoslynCompilerService;
                service.GenerateInMemory = !XRL.UI.Options.OutputModAssembly;
                service.OutputDirectory = DataManager.SavePath("ModAssemblies");
                service.OutputPDBExtension = ".pdb";
                service.GenerateSymbols = !service.GenerateInMemory;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.IsDynamic) continue;
                    if (assembly.Location.IsNullOrEmpty()) continue;
                    if (assembly.Location.Contains("ModAssemblies")) continue;
                    if (assembly.Location.Contains("UIElements")) continue;
                    if (assembly.Location.Contains("UnityEditor.")) continue;
                    if (assembly.FullName.Contains("ExCSS")) continue;
                    service.ReferenceAssemblies.Add(AssemblyReference.FromAssembly(assembly));
                }
                foreach (var dll in Loadables)
                {
                    MetricsManager.LogInfo($"XUnity.AutoTranslator - Injecting {dll}");
                    var assembly = XUnityDomain.LoadAssembly(dll);
                    service.ReferenceAssemblies.Add(AssemblyReference.FromAssembly(assembly.RawAssembly));
                }
                var injectorFile = Path.Combine(info.Path, "Loadables", "AutoTranslator.Plugin.cs");
                MetricsManager.LogInfo($"XUnity.AutoTranslator - Compiling {injectorFile}");
                var result = service.CompileFromFile(injectorFile);
                foreach (var @error in result.Errors)
                {
                    MetricsManager.LogInfo($"Error: {error}");
                }
                foreach (var @type in result.OutputAssembly.GetTypes())
                {
                    MetricsManager.LogInfo($"Showing type {@type.AssemblyQualifiedName}");
                }

                var patcher = new Harmony("XUnity.AutoTranslator.Plugin.Qud");
                patcher.PatchAll(result.OutputAssembly);

                result.OutputAssembly.GetType("XUnity.AutoTranslator.Plugin.Qud.AutoTranslatorPlugin")
                    .GetMethod("StartMod", BindingFlags.Static | BindingFlags.Public)
                    .Invoke(null, new object[]{});
            }
            catch (Exception e)
            {
                MetricsManager.LogError("XUnity.AutoTranslator", e);
            }
       }
    }
}