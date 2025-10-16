using System.Collections.Generic;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using Ninjadini.Logger.Internal;
using UnityEngine;

namespace Ninjadini.Console
{
    public class ConsoleSettings : ScriptableObject
    {
        public const string Version = "1.2.1";
        public const string WebDocsURL = "https://ninjadini.github.io/njconsole/";
        public const string AssetStoreURL = "https://assetstore.unity.com/packages/slug/319982";
        public const string ResourceName = "NjConsoleSettings";
        public const string GameObjectName = "NjConsole";
        public const string PanelSettingsResName = "NjConsole/NjConsoleOverlay";
        public const string StyleSheetResName = "NjConsole/NjConsoleStyle";
        
        public bool autoStartOverlay = true;

        [Tooltip("Default value: Debug\n" +
                 "Stack traces allocate a bit of memory but are valuable for debugging.\n" +
                 "Consider setting to 'Error' for production.\n" +
                 "Unity’s Debug.Log() stack trace behavior is controlled via Project Settings > Player")]
        public StackTraceLevel stackTraceMinLevel = StackTraceLevel.Debug;
        
        // see dynamic tooltip in project settings
        public UnityToNjLogger.Modes inPlayerLogMode = UnityToNjLogger.Modes.PassThroughToNjLogger;
        
        /*
        [Tooltip("Default value: false\nAlso write to Console.WriteLine()\n" +
                 "Every log call with multiple arguments will be joined into a single string, which causes memory allocation.")]
        public bool writeToSystemLogs;
        */

        [Tooltip("Default value: ShowAtTop\n" +
                 "Behaviour when encountering an error log:\n" +
                 "- ShowAtTop: Show the error message at the top of screen\n" +
                 "- ShowConsoleOverlay: Show the console overlay's logs panel\n" +
                 "- Ignore: Do nothing")]
        public OverlayBehaviourOnError behaviourOnError = OverlayBehaviourOnError.ShowAtTop;

        [Tooltip("Email address to prefill for 'Utilities > Options > Email Text Logs' button. It can be left blank.")]
        public string emailForTextLogButton;

        [Tooltip("Default value: false\nIf enabled, Unity logs (Debug.Log, etc.) will appear under 'unity' channel in NjConsole.")]
        public bool channelUnityLogs;
        
        [Tooltip("Default value: true\nEnable/disable the logs panel in player builds. Always enabled in the Editor.")]
        public bool inPlayerLogsPanel = true;
        
        [Tooltip("Default value: true\nEnable/disable the options panel in player builds. Always enabled in the Editor.")]
        public bool inPlayerOptionsPanel = true;
        
        [Tooltip("Default value: true\nEnable/disable the hierarchy panel in player builds. Always enabled in the Editor.")]
        public bool inPlayerHierarchyPanel = true;
        
        [Tooltip("Default value: true\nEnable/disable the utilities panel in player builds. Always enabled in the Editor.")]
        public bool inPlayerUtilitiesPanel = true;
        
        [Tooltip("Default value: true\n" +
                 "1. Allows clicking object references from logs for inspection in player builds.\n" +
                 "2. Allows viewing detailed component inspection in hierarchy view.\n" +
                 "3. Allows searching for types to inspect static fields, properties, methods etc.\n" +
                 "Always enabled in the Editor.")]
        public bool inPlayerObjectInspector = true;
        
        [Tooltip("Default value: true\nEnable/disable command line UI in player builds. Always enabled in the Editor.")]
        public bool inPlayerCommandLine = true;
        
        [Tooltip("Default value: false\n" +
                 "In Editor, keybindings are allowed even without passing the access challenge.\n" +
                 "In player builds, with inPlayerKeyBindings enabled, it will only work once access challenge has passed.")]
        public bool inPlayerKeyBindings;

        [Range(128, 2560)]
        [Tooltip("Default value: 512\nIf you want a larger size than allowed maximum, manually set it in code: NjLogger.LogsHistory.SetMaxHistoryCount()")]
        public int maxLogsHistory = 512;

        [SerializeReference]
        public List<IConsoleModule> modules = new List<IConsoleModule>();
        
        static ConsoleSettings _instance;
        
        public static ConsoleSettings Get(bool canCreate = true)
        {
            if (_instance)
            {
                return _instance;
            }
            _instance = Resources.Load<ConsoleSettings>(ResourceName);
            if (_instance)
            {
                return _instance;
            }
            if (!canCreate)
            {
                return null;
            }
            _instance = CreateInstance<ConsoleSettings>();
#if !NJCONSOLE_DISABLE
#if UNITY_EDITOR
            var instance = _instance;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (!instance || (_instance && _instance != instance)) return;
                const string resPath = "Assets/Resources";
                if (!UnityEditor.AssetDatabase.IsValidFolder(resPath))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                    UnityEditor.AssetDatabase.Refresh();
                }
                UnityEditor.AssetDatabase.CreateAsset(instance, resPath + "/" + ResourceName + ".asset");
            };
#endif
            SetupDefaultSettings(_instance);
#endif
            return _instance;
        }

#if !NJCONSOLE_DISABLE

        static void SetupDefaultSettings(ConsoleSettings settings)
        {
            settings.modules.Add(new ConsoleKeyPressTrigger());
            settings.modules.Add(new ConsolePressAndHoldTrigger());
        }

        void Reset()
        {
            autoStartOverlay = true;
            stackTraceMinLevel = StackTraceLevel.Debug;
            inPlayerLogMode = UnityToNjLogger.Modes.PassThroughToNjLogger;
            //writeToSystemLogs = false;
            behaviourOnError = OverlayBehaviourOnError.ShowAtTop;
            maxLogsHistory = 512;
            
            emailForTextLogButton = "";
            inPlayerLogsPanel = true;
            inPlayerObjectInspector = true;
            inPlayerOptionsPanel = true;
            inPlayerHierarchyPanel = true;
            inPlayerUtilitiesPanel = true;
            inPlayerKeyBindings = false;

            modules ??= new List<IConsoleModule>();
            modules.Clear();
            modules.Add(new ConsoleKeyPressTrigger());
            modules.Add(new ConsolePressAndHoldTrigger());
        }
#endif
        
        public enum OverlayBehaviourOnError
        {
            Ignore = 0,
            ShowAtTop = 1,
            //ShowErrorToast = 2,
            ShowConsoleOverlay = 3
        }

        public enum StackTraceLevel : byte
        {
            Debug = (byte)NjLogger.Options.Debug,
            Info = (byte)NjLogger.Options.Info,
            Warn = (byte)NjLogger.Options.Warn,
            Error = (byte)NjLogger.Options.Error,
            None = 8
        }
    }
}