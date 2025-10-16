using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Ninjadini.Console.Editor
{
    [FilePath("ProjectSettings/NinjadiniConsole.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ConsoleEditorSettings : ScriptableSingleton<ConsoleEditorSettings>
    {
        public const string DisableSymbol = "NJCONSOLE_DISABLE";

        public const string SettingsMenuPath = "Project/Ninjadini ⌨ Console";

        [Tooltip("Default value: Debug\n" +
                 "Stack traces allocate a bit of memory but are valuable for debugging.\n" +
                 "You may want to disable it reduce noise during profiling.\n" +
                 "Unity’s Debug.Log() stack trace behavior is controlled via Project Settings > Player")]
        public ConsoleSettings.StackTraceLevel inEditorStackTraceMinLevel = ConsoleSettings.StackTraceLevel.Debug;
        
         // This works, but just feels it's too complicated for general use.
        //public UnityToNjLogger.Modes inEditorLogMode = UnityToNjLogger.Modes.BothUnityAndNjLogger;
        
        [Range(128, 5120)]
        public int inEditorMaxLogsHistory = 2048;
        
        public bool clearLogsOnPlay = true;
        
        //public bool writeToSystemLogs;
        
        public ConsoleEditorSettings() : base()
        {
        }

        public static ConsoleEditorSettings Get()
        {
            return instance;
        }

        public void Save()
        {
            Save(true);
        }
        
        /// Add define symbol to disable console.
        /// 
        /// If you are calling it outside the pre-build step, please also call `UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()`;
        public static void AddDefineSymbolToDisableConsole()
        {
            ModifyDefineSymbol(DisableSymbol, true);
            // If you are calling it outside the pre-build step, please also call `UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()`;
        }

        /// Remove define symbol to ensure console is enabled.
        /// If you are calling it outside the pre-build step, please also call `UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()`;
        public static void RemoveDefineSymbolAndEnableConsole()
        {
            ModifyDefineSymbol(DisableSymbol, false);
            // If you are calling it outside the pre-build step, please also call `UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()`;
        }

        public static bool HasDefineSymbolToDisableConsole() => HasDefineSymbol(DisableSymbol);

        public static void ModifyDefineSymbol(string symbol, bool add)
        {
            var symbols = ExtractSymbols(out var namedTarget);
            if (add)
            {
                if (!symbols.Contains(symbol))
                {
                    symbols.Add(symbol);
                }
            }
            else
            {
                symbols.RemoveAll(s => s == symbol);
            }
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", symbols));
            // If you are calling it outside the pre-build step, please also call `UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()`;
        }

        public static bool HasDefineSymbol(string symbol)
        {
            var symbols = ExtractSymbols(out var namedTarget);
            return symbols.Contains(symbol);
        }

        static List<string> ExtractSymbols(out NamedBuildTarget namedTarget)
        {
            namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (namedTarget == NamedBuildTarget.Unknown) return new List<string>();
            var symbols = PlayerSettings.GetScriptingDefineSymbols(namedTarget)
                .Split(';')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList();
            return symbols;
        }
    }
}