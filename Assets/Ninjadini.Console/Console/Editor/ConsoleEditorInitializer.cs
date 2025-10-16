#if !NJCONSOLE_DISABLE
using System;
using System.Linq;
using Ninjadini.Console.UI;
using Ninjadini.Logger;
using Ninjadini.Logger.Internal;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.Editor
{
    [InitializeOnLoad]
    static class ConsoleEditorInitializer
    {
        static readonly (Type, Func<IConsoleModule>)[] DefaultEditModeModules =
        {
            // I am doing this via Func callback to make sure the constructor doesn't get stripped if someone decides to make the lib into a dll.
            //(typeof(ConsoleOptionsForEditMode), () => new ConsoleOptionsForEditMode()),
            (typeof(ConsoleHierarchyModule), () => new ConsoleHierarchyModule()),
            (typeof(ConsoleUtilitiesModule), () => new ConsoleUtilitiesModule()),
            (typeof(OptionCommandsModule), () => new OptionCommandsModule()),
        };
        
        static ConsoleEditorInitializer()
        {
            ConsoleContext.EditorBridge = new ConsoleEditorBridge();
            var settings = ConsoleEditorSettings.Get();
            
            ConsoleUtilitiesModule.LocalTimeAtStart = DateTime.Now;

            var  inEditorLogMode = UnityToNjLogger.Modes.BothUnityAndNjLogger; //settings.inEditorLogMode
            UnityToNjLogger.Start(inEditorLogMode, settings.inEditorMaxLogsHistory);
            NjLogger.SetMinSetTraceLevel((int)settings.inEditorStackTraceMinLevel);
            
            /*if (inEditorLogMode == UnityToNjLogger.Modes.PassThroughToNjLogger)
            {
                prevLogHandler.LogFormat(LogType.Log, null, $"NjConsole's logger pass through is enabled.\nAll normal unity console logs will redirected to NjConsole... open via {NjConsoleEditorWindow.MenuPath}.");
            }*/
            /*if (settings.writeToSystemLogs)
            {
                NjLogger.AddHandler(new LoggerToConsoleHandler());
            }*/
            CompilationPipeline.assemblyCompilationFinished += ProcessBatchModeCompileFinish;
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
            
            NjConsole.Modules.EnsureModulesExist(DefaultEditModeModules);
            var runtimeSettings = ConsoleSettings.Get();
            UnityToNjLogger.SendToChannel = runtimeSettings.channelUnityLogs;
            if (runtimeSettings.modules?.Count > 0)
            {
                foreach (var module in runtimeSettings.modules)
                {
                    if (module != null && module.PersistInEditMode && NjConsole.Modules.GetModule(module.GetType(), includeSubClasses:true) == null)
                    {
                        try
                        {
                            NjConsole.Modules.AddModule(module);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("There was a problem adding NjConsole module " + module.GetType().Name +" See next log for details of the exception");
                            Debug.LogException(e);
                        }
                    }
                }
            }
        }

        static void OnEditorUpdate()
        {
            if (EditorWindow.focusedWindow is NjConsoleEditorWindow consoleEditorWindow)
            {
                ConsoleContext.SetFocusedContext(consoleEditorWindow.Context);
            }
            else
            {
                ConsoleContext.SetFocusedContext(null);
            }
        }
        
        static void OnEditorPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.ExitingEditMode)
            {
                if (ConsoleEditorSettings.instance.clearLogsOnPlay)
                {
                    NjLogger.LogsHistory.Clear();
                }
                NjLogger.Info(Color.grey, "<b>>>>>> PLAY MODE STARTING >>>>> </b>");
            }
            else if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
                NjLogger.Info(Color.grey, "<b><<<<< PLAY MODE ENDED <<<<< </b>");
            }
            if (stateChange == PlayModeStateChange.ExitingPlayMode && NjConsole.HasStartedModules)
            {
                NjConsole.Modules.RemovePlayModeOnlyModules();
            }
        }
        
        static void ProcessBatchModeCompileFinish(string str, CompilerMessage[] compilerMessages)
        {
            var errorsCount = compilerMessages.Count(m => m.type == CompilerMessageType.Error);
            if (errorsCount > 0)
            {
                NjLogger.LogsHistory?.Clear();
                NjLogger.Warn("▼ <b>",errorsCount, $" compilation error{(errorsCount > 1 ? "s" : "")}</b>");
            }
            foreach (var message in compilerMessages)
            {
                if (message.type == CompilerMessageType.Error)
                {
                    NjLogger.Error(message.message, options:NjLogger.Options.ForceNoStackTrace);
                }
                else if (message.type == CompilerMessageType.Warning)
                {
                    NjLogger.Warn(message.message, options:NjLogger.Options.ForceNoStackTrace);
                }
                else if (message.type == CompilerMessageType.Info)
                {
                    NjLogger.Info(message.message, options:NjLogger.Options.ForceNoStackTrace);
                }
            }
            if (errorsCount > 0)
            {
                NjLogger.Warn("▲");
            }
        }

        const string InspectMenuName = "GameObject/Inspect in ⌨ NjConsole";
        [MenuItem(InspectMenuName, false)]
        static void InspectMenuItem(MenuCommand command)
        {
            var obj = command.context ?? Selection.activeObject;
            if (!obj)
            {
                return;
            }
            var window = NjConsoleEditorWindow.GetOrCreateWindow().Window;
            var inspector = window.Q<ConsoleInspector>();
            if (inspector != null)
            {
                inspector.Inspect(obj);
            }
            else
            {
                ConsoleInspector.Show(window, obj);
            }
        }

        [MenuItem(InspectMenuName, true)]
        static bool ValidateInspectMenuItem(MenuCommand command)
        {
            return command.context || Selection.activeObject;
        }
    }
}
#endif