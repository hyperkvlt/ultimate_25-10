#if !NJCONSOLE_DISABLE
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninjadini.Logger.Internal;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.Editor
{
    partial class ConsoleEditorSettingsUI
    {
        void PopulateOverlayActivation()
        {
            var field = new PropertyField();
            field.BindProperty(_runtimeSerializedObject.FindProperty(nameof(_drawnSetting.autoStartOverlay)));
            _panelContent.Add(field);

            var startHelp = new HelpBox(
                $"Start console overlay in playmode by calling <u>{nameof(NjConsole)}.{nameof(NjConsole.Overlay)}.{nameof(NjConsole.Overlay.EnsureStarted)}();</u>", 
                HelpBoxMessageType.Warning);
            _panelContent.Add(startHelp);
            void StartHelpUpdate()
            {
                if (!_drawnSetting) return;
                startHelp.style.display = _drawnSetting.autoStartOverlay ? DisplayStyle.None : DisplayStyle.Flex;
            }
            StartHelpUpdate();
            field.schedule.Execute(StartHelpUpdate).Every(100);
            
            _panelContent.Add(Title("Activation Triggers"));

            var count = DrawModules(_panelContent, typeof(IConsoleOverlayTrigger), "Trigger");

            if (count > 0)
            {
                _panelContent.Add(new HelpBox("Console overlay will start hidden. Performing the above actions will show the console. - this is for runtime only.", HelpBoxMessageType.Info));
            }
            else
            {
                _panelContent.Add(new HelpBox("No triggers set up. Console overlay will automatically show at start.", HelpBoxMessageType.Warning));
            }
            
            _panelContent.Add(new VisualElement()
            {
                style = { height = 10 }
            });
            
            _panelContent.Add(Title("Access Challenge"));
            count = DrawModules(_panelContent, typeof(IConsoleAccessChallenge), "Access Challenge");
            if (count > 0)
            {
                _panelContent.Add(new HelpBox("Console will perform the access challenge before allowing the user to use the console.", HelpBoxMessageType.Info));
            }
            else
            {
                _panelContent.Add(new HelpBox("Console will appear without any access challenge.", HelpBoxMessageType.Warning));
            }
            ApplyChangesButton();
            PopulateIfAnyInvalidModules();
        }

        void PopulateLogging()
        {
            var channelUnityLogsToggle = new Toggle("Channel Unity Logs");
            channelUnityLogsToggle.schedule.Execute(() =>
            {
                channelUnityLogsToggle.SetValueWithoutNotify(_drawnSetting.channelUnityLogs);
            }).Every(100);
            channelUnityLogsToggle.RegisterValueChangedCallback((v) =>
            {
                _drawnSetting.channelUnityLogs = v.newValue;
                UnityToNjLogger.SendToChannel = v.newValue;
            });
            var tooltipAttribute = typeof(ConsoleSettings).GetField(nameof(ConsoleSettings.channelUnityLogs))?.GetCustomAttribute<TooltipAttribute>();
            AddWithTooltipBtn(channelUnityLogsToggle, tooltipAttribute);
            if (tooltipAttribute != null)
            {
                channelUnityLogsToggle.tooltip = tooltipAttribute.tooltip;
            }

            var title = Title("In Player (Builds)");
            title.style.paddingTop = 3;
            _panelContent.Add(title);
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.maxLogsHistory));
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.stackTraceMinLevel));
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.behaviourOnError));
            
            var field = AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.inPlayerLogMode), "Debug.Log() behaviour");
            UnityToNjLogger.Modes? drawnMode = null;
            var modeHelp = new HelpBox("...", HelpBoxMessageType.Info);
            modeHelp.style.marginLeft = 125;
            _panelContent.Add(modeHelp);
            void UpdateModeHelp()
            {
                if (!_drawnSetting) return;
                if (drawnMode == _drawnSetting.inPlayerLogMode)
                {
                    return;
                }
                drawnMode = _drawnSetting.inPlayerLogMode;
                switch (_drawnSetting.inPlayerLogMode)
                {
                    case UnityToNjLogger.Modes.PassThroughToNjLogger:
                        modeHelp.text = ObjectNames.NicifyVariableName(drawnMode.ToString()) + ": (Recommended default setting)\n" +
                                        "\u2714 Debug.Log() will be redirected to NjLogger / NjConsole\n" +
                                        "\u2714 Will leverage fast logging and low allocation traits from NjLogger\n" +
                                        "\u2714 Context from Debug.Log(..., context) will arrive to NjConsole for inspection\n" +
                                        "\u2718 Logs will not go to Unity's console or system log\n" +
                                        "\u2718 \u2718 Application.logMessageReceived will not trigger any event in player";
                        modeHelp.messageType = HelpBoxMessageType.Info;
                        break;
                    case UnityToNjLogger.Modes.BothUnityAndNjLogger:
                        modeHelp.text = ObjectNames.NicifyVariableName(drawnMode.ToString()) +":\n"+
                                        "\u2714 Debug.Log() will function as normal\n" +
                                        "\u2714 Debug.Log() will be sent to NjLogger / NjConsole\n" +
                                        "\u2718 Context from Debug.Log(..., context) will not get to NjConsole for inspection\n" +
                                        "\u2718 Will not leverage fast logging and low allocation traits from NjLogger\n";
                        modeHelp.messageType = HelpBoxMessageType.Info;
                        break;
                    case UnityToNjLogger.Modes.BothUnityAndNjLoggerWithContext:
                        modeHelp.text = ObjectNames.NicifyVariableName(drawnMode.ToString()) +":\n"+
                                        "\u2714 Debug.Log() will function as normal\n" +
                                        "\u2714 Debug.Log() will be sent to NjLogger / NjConsole\n" +
                                        "\u2714 Context from Debug.Log(..., context) will arrive to NjConsole for inspection\n" +
                                        "\u2718 Some third party tools relying on Unity's default logger may not function properly\n" +
                                        "\u2718 Will not leverage fast logging and low allocation traits from NjLogger\n";
                        modeHelp.messageType = HelpBoxMessageType.Info;
                        break;
                    case UnityToNjLogger.Modes.StayInUnityConsole:
                        modeHelp.text = ObjectNames.NicifyVariableName(drawnMode.ToString()) +":\n"+
                                        "\u2714 Reduces stress to NjLogger's log history\n" +
                                        "\u2714 Debug.Log() will function as normal\n" +
                                        "\u2718 \u2718 \u2718 UnityEngine's Debug.Log() will not show up in NjConsole";
                        modeHelp.messageType = HelpBoxMessageType.Warning;
                        break;
                    default:
                        modeHelp.text = "?";
                        modeHelp.messageType = HelpBoxMessageType.Error;
                        break;
                }
            }
            UpdateModeHelp();
            field.schedule.Execute(UpdateModeHelp).Every(100);

            var editorSettings = ConsoleEditorSettings.Get();
            var editorObject = new SerializedObject(editorSettings);
            
            _panelContent.Add(Title("In Editor Only"));
            
            AddPropertyField(editorObject, nameof(editorSettings.inEditorMaxLogsHistory));
            AddPropertyField(editorObject, nameof(editorSettings.inEditorStackTraceMinLevel));
            AddPropertyField(editorObject, nameof(editorSettings.clearLogsOnPlay));
        }
        
        void PopulateFeatures()
        {
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.emailForTextLogButton));
            
            _panelContent.Add(Title("In Player Features"));
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.inPlayerLogsPanel));
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.inPlayerOptionsPanel));
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.inPlayerHierarchyPanel));
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.inPlayerUtilitiesPanel));
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.inPlayerObjectInspector));
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.inPlayerCommandLine));
            AddPropertyField(_runtimeSerializedObject, nameof(ConsoleSettings.inPlayerKeyBindings));
            
            var startHelp = new HelpBox(
                "When distributing your game externally, consider disabling certain features for security.\nAlternatively, use the <u>Disable NjConsole</u> tab to strip out NjConsole from your build.", 
                HelpBoxMessageType.Info);
            _panelContent.Add(startHelp);
        }

        void PopulateExtensionModules()
        {
            _panelContent.Add(Title("Extension Modules"));
            DrawModules(_panelContent, typeof(IConsoleExtension), "Extension Module");

            if (_needsToSave)
            {
                ApplyChangesButton("Apply extension changes");
            }
            PopulateIfAnyInvalidModules();

            
            _panelContent.Add(new Label("<b>See documentation</b>\n– Create custom extensions\n– Browse external extensions repo")
            {
                style = { marginTop = 50 }
            });
            var btn = new Button(() =>
            {
                Application.OpenURL(ConsoleSettings.WebDocsURL);
            });
            btn.text = "Documentation";
            btn.style.width = 170;
            btn.style.height = 22;
            _panelContent.Add(btn);
        }

        void PopulateIfAnyInvalidModules()
        {
            var nullModules = _drawnSetting.modules.Count(m => m == null);
            if (nullModules > 0)
            {
                var helpBox = new HelpBox(
                    $"Found {nullModules} invalid module(s).\nPerhaps the type was deleted or renamed.", 
                    HelpBoxMessageType.Error);
                helpBox.style.marginTop = 20;
                helpBox.style.width = 250;
                _panelContent.Add(helpBox);
                var btn = new Button(() =>
                {
                    var newModules = new List<IConsoleModule>(_drawnSetting.modules.Where(m => m != null));
                    _drawnSetting.modules = newModules;

                    var path = AssetDatabase.GetAssetPath(_drawnSetting);
                    
                    var clone = Object.Instantiate(_drawnSetting);
                    AssetDatabase.DeleteAsset(path);
                    
                    _drawnSetting = clone;
                    _runtimeSerializedObject = new SerializedObject(clone);

                    AssetDatabase.CreateAsset(clone, path);
                    AssetDatabase.SaveAssets();
                    ReloadCurrentPanel();
                });
                btn.text = "Remove invalid modules";
                btn.style.width = 250;
                btn.style.height = 22;
                _panelContent.Add(btn);
            }
        }
        
        void PopulateDisableConsole()
        {
            _panelContent.Add(Title("Disable / Strip - NjConsole"));
            
            _panelContent.Add( new HelpBox("- Stripping helps reduce memory and build size." +
                                           "\n- Helps prevent malicious users from accessing debug logs or cheat features." +
                                           "\n- Recommended for production builds." + 
                                           "\n" + 
                                           "\n- Most interface members and key classes are stubbed so your project continues to compile even if NjConsole is disabled." +
                                           "\n- Some APIs will be stripped entirely, so you may need to manually guard those calls with `#if !NJCONSOLE_DISABLE`." +
                                           "\n- For best security, consider wrapping your own cheat/debug calls as well." +
                                           "\n" + 
                                           "\n- To conditionally disable NjConsole in your own build script:\nCall `ConsoleEditorSettings.AddDefineSymbolToDisableConsole()` in your pre-build step, and RemoveDefineSymbolAndEnableConsole() in post-build if needed.",
                HelpBoxMessageType.Info));
            
            var btn = new Button(() =>
            {
                ConsoleEditorSettings.AddDefineSymbolToDisableConsole();
                PostScriptDefineChange();
            });
            btn.text = "Disable NjConsole";
            btn.style.backgroundColor = new Color(1f, 0.33f, 0.29f);
            btn.style.width = 170;
            btn.style.width = 170;
            btn.style.height = 22;
            _panelContent.Add(btn);
        }
    }
}
#endif