using HarmonyLib;
using ExIni;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XRL;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Configuration;
using XUnity.AutoTranslator.Plugin.Core.Constants;

namespace XUnity.AutoTranslator.Plugin.Qud
{
   public class AutoTranslatorPlugin : IPluginEnvironment
   {
      private IniFile _file;
      private string _configPath;

      public AutoTranslatorPlugin()
      {
         ConfigPath = DataManager.LocalPath("AutoTranslator");
         TranslationPath = DataManager.LocalPath("AutoTranslator");

         _configPath = Path.Combine( ConfigPath, "AutoTranslatorConfig.ini" );
      }

      public IniFile Preferences
      {
         get
         {
            return ( _file ?? ( _file = ReloadConfig() ) );
         }
      }

      public string ConfigPath { get; }

      public string TranslationPath { get; }

      public bool AllowDefaultInitializeHarmonyDetourBridge => false;

      public IniFile ReloadConfig()
      {
         if( !File.Exists( _configPath ) )
         {
            return ( _file ?? new IniFile() );
         }
         IniFile ini = IniFile.FromFile( _configPath );
         if( _file == null )
         {
            return ( _file = ini );
         }
         _file.Merge( ini );
         return _file;
      }

      public void SaveConfig()
      {
         _file.Save( _configPath );
      }

      void Awake()
      {
         // Harmony inject the LoadTranslations???
         PluginLoader.LoadWithConfig( this );
      }

      static public void StartMod()
      {
      
         MetricsManager.LogInfo($"Starting XUnity.AutoTranslator");
         new AutoTranslatorPlugin().Awake();
      }
   }

   [HarmonyPatch("XUnity.AutoTranslator.Plugin.Core.TextTranslationCache", "GetTranslationFiles")]
   public class LanguageAgnosticTranslationFileInjector
   {
      static IEnumerable<string> Postfix(IEnumerable<string> values)
      {
         if (values != null)
         {
            foreach (var value in values)
            {
               yield return value;
            }
         }
         yield return Path.Combine(ModManager.GetMod("XUnityAutoTranslator").Path, "defaults", "language-agnostic-translation-helpers.txt");
      }

   }

}