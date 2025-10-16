using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.Editor
{
    partial class ConsoleEditorSettingsUI : SettingsProvider
    {
        ConsoleEditorSettingsUI() : base(ConsoleEditorSettings.SettingsMenuPath, SettingsScope.Project) { }
        
        VisualElement _rootElement;

#if NJCONSOLE_DISABLE
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _rootElement = rootElement;
            _compilationFailed = false;
            if (CheckCompilationFailedMode(_rootElement))
            {
                return;
            }
            AddTitle(rootElement);
            
            rootElement.Add(new HelpBox("NjConsole is disabled via <u>NJCONSOLE_DISABLE</u> scripting define.", 
                HelpBoxMessageType.Error));
            AddEnableConsoleButton(rootElement);
        }
        
        public override void OnInspectorUpdate()
        {
            if (EditorUtility.scriptCompilationFailed && _rootElement != null)
            {
                CheckCompilationFailedMode(_rootElement);
            }
        }
#else
        List<(Button button, Action populator)> _sideBtns;
        int _currentIndex;
        ScrollView _panelContent;
        ConsoleSettings _drawnSetting;
        bool _needsToSave;
        bool _lockSave;

        SerializedObject _runtimeSerializedObject;
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _rootElement = rootElement;
            _compilationFailed = false;
            if (CheckCompilationFailedMode(_rootElement))
            {
                return;
            }
            SyncObjectInstance();
            
            AddTitle(rootElement);
            
            var horizontal = new VisualElement();
            horizontal.style.flexDirection = FlexDirection.Row;
            horizontal.style.flexGrow = 1;
            rootElement.Add(horizontal);
            
            var sideBar = new ScrollView();
            sideBar.style.paddingLeft = 6;
            sideBar.style.paddingRight = 2;
            horizontal.Add(sideBar);
            sideBar.contentContainer.style.flexGrow = 1f;
            

            _panelContent = new ScrollView();
            _panelContent.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            _panelContent.style.flexGrow = 1;
            _panelContent.style.paddingLeft = 5;
            _panelContent.style.paddingTop = 5;

            horizontal.Add(_panelContent);

            _sideBtns = new List<(Button, Action)>();
            AddSideBtn(sideBar, "Playmode Overlay", PopulateOverlayActivation);
            AddSideBtn(sideBar, "Logging", PopulateLogging);
            AddSideBtn(sideBar, "Features", PopulateFeatures);
            AddSideBtn(sideBar, "Extension Modules", PopulateExtensionModules);
            AddSideBtn(sideBar, "Disable NjConsole", PopulateDisableConsole);

            var gap = new VisualElement()
            {
                style = { flexGrow = 1 }
            };
            sideBar.Add(gap);
            
            var settingsFileBtn = new Button(LocateSettingsFile);
            settingsFileBtn.text = "Locate\nsettings file";
            settingsFileBtn.style.marginBottom = 4;
            sideBar.Add(settingsFileBtn);

            _currentIndex = -1;
            var index = LastPanelIndex;
            if (index < 0 || index >= _sideBtns.Count)
            {
                index = 0;
            }
            GoToPanel(index);
        }

        bool SyncObjectInstance()
        {
            if (!_drawnSetting || 
                _runtimeSerializedObject == null 
                ||_runtimeSerializedObject.targetObject != _drawnSetting)
            {
                var settings = ConsoleSettings.Get();
                var path = AssetDatabase.GetAssetPath(settings);
                if (string.IsNullOrEmpty(path))
                {
                    // Something has gone wrong, it won't properly save the asset but at least it won't blow up.
                    _drawnSetting = settings;
                }
                else
                {
                    _drawnSetting = AssetDatabase.LoadAssetAtPath<ConsoleSettings>(path);
                }
                _runtimeSerializedObject = new SerializedObject(_drawnSetting);
                return true;
            }
            return false;
        }

        void LocateSettingsFile()
        {
            if (!_drawnSetting) return;
            EditorApplication.ExecuteMenuItem("Window/General/Project");
            EditorApplication.delayCall += () =>
            {
                Selection.activeObject = _drawnSetting;
                EditorGUIUtility.PingObject(_drawnSetting);
            };
        }

        void AddSideBtn(ScrollView sideBar, string name, Action callback)
        {
            var index = _sideBtns.Count;
            var btn = new Button(() => GoToPanel(index));
            btn.text = name;
            btn.style.whiteSpace = WhiteSpace.Normal;
            btn.style.width = 100;
            btn.style.height = 40;
            sideBar.Add(btn);
            _sideBtns.Add((btn, callback));
        }

        public void GoToPanel(int index)
        {
            SyncObjectInstance();
            if (_currentIndex >= 0 && index < _sideBtns.Count)
            {
                var last = _sideBtns[_currentIndex];
                last.button.style.backgroundColor = StyleKeyword.Null;
            }
            _currentIndex = index;
            _panelContent.Clear();
            if (index < 0 || index >= _sideBtns.Count)
            {
                return;
            }
            LastPanelIndex = index;
            _drawnSetting = (ConsoleSettings)_runtimeSerializedObject.targetObject;
            var newBtn = _sideBtns[index];
            newBtn.button.style.backgroundColor = new Color(0.39f, 0.26f, 0.63f);
            newBtn.populator();
        }

        Label Title(string str, int fontSize = 18)
        {
            var lbl = new Label(str);
            lbl.style.fontSize = fontSize;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.paddingLeft = 4;
            lbl.style.paddingTop = 12;
            lbl.style.paddingBottom = 6;
            return lbl;
        }

        PropertyField AddPropertyField(SerializedObject serializedObject, string fieldName, string nameOverride = null)
        {
            var field = new PropertyField();
            if (!string.IsNullOrEmpty(nameOverride))
            {
                field.label = nameOverride;
            }
            field.BindProperty(serializedObject.FindProperty(fieldName));
            
            var tooltip = serializedObject.targetObject?.GetType()?.GetField(fieldName)?.GetCustomAttribute<TooltipAttribute>();
            AddWithTooltipBtn(field, tooltip);
            return field;
        }

        void AddWithTooltipBtn(VisualElement field, TooltipAttribute tooltip)
        {
            if (tooltip != null)
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.style.flexGrow = 1f;
                field.style.flexGrow = 1f;
                container.Add(field);
                
                container.Add(new Button(() =>
                {
                    EditorUtility.DisplayDialog("", tooltip.tooltip, "OK");
                })
                {
                    text = "Info",
                    tooltip = tooltip.tooltip,
                    style = { maxHeight = 22}
                });
                
                _panelContent.Add(container);
            }
            else
            {
                _panelContent.Add(field);
            }
        }

        int DrawModules(VisualElement parent, Type type, string displayName)
        {
            var prop = _runtimeSerializedObject.FindProperty(nameof(ConsoleSettings.modules));
            var arraySize = prop.arraySize;
            var count = 0;
            for (var i = 0; i < arraySize; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                var value = element.managedReferenceValue as IConsoleModule;
                if (value == null || !type.IsInstanceOfType(value))
                {
                    continue;
                }
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                if (HasVisibleChildren(element))
                {
                    var field = new PropertyField(element, FormatModuleName(value.GetType()));
                    field.BindProperty(element);
                    field.style.flexGrow = 1f;
                    container.Add(field);
                }
                else
                {
                    var lbl = new Label(FormatModuleName(value.GetType()));
                    lbl.style.flexGrow = 1f;
                    container.Add(lbl);
                }
                var tooltip = value.GetType().GetCustomAttribute<TooltipAttribute>();
                if (tooltip != null)
                {
                    container.Add(new Button(() =>
                    {
                        EditorUtility.DisplayDialog("", tooltip.tooltip, "OK");
                    })
                    {
                        text = "Tooltip",
                        tooltip = tooltip.tooltip,
                        style = { maxHeight = 22}
                    });
                }
                container.Add(new Button(() =>
                {
                    RemoveModuleBtnClicked(value);
                })
                {
                    text = "X",
                    style = { width = 20, maxHeight = 22 }
                });
                count++;
                parent.Add(container);
            }
            Button btn = null;
            btn = new Button(() => AddModuleBtnClicked(btn, type))
            {
                text = "Add " + displayName,
                style =
                {
                    width = 250
                }
            };
            btn.style.height = 24;
            parent.Add(btn);
            return count;
        }
        
        static bool HasVisibleChildren(SerializedProperty property)
        {
            var copy = property.Copy();
            var end = copy.GetEndProperty();
            copy.NextVisible(true);
            while (!SerializedProperty.EqualContents(copy, end))
            {
                if (copy.propertyPath.StartsWith(property.propertyPath) && copy.depth > property.depth)
                {
                    return true;
                }
                copy.NextVisible(false);
            }
            return false;
        }

        void AddModuleBtnClicked(Button btn, Type type)
        {
            var settings = _drawnSetting;
            var menu = new GenericDropdownMenu();
            var types = (from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                where !domainAssembly.IsDynamic
                from assemblyType in domainAssembly.GetExportedTypes()
                where assemblyType.IsClass 
                      && !assemblyType.IsAbstract 
                      && type.IsAssignableFrom(assemblyType)
                      && !type.IsDefined(typeof(HideInInspector))
                      && !settings.modules.Exists(m => m?.GetType() == assemblyType)
                select assemblyType);
            if (!types.Any())
            {
                var haveAny = settings.modules.Exists(m => type.IsAssignableFrom(m?.GetType()));

                if (haveAny)
                {
                    EditorUtility.DisplayDialog("", $"You have added all possible types of {type.Name}\nSee documentation on how to implement your own custom version.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("", $"No eligible {type.Name} types found.\nSee documentation on how to implement your own custom version.", "OK");
                }
                return;
            }
            foreach (var childTypeLoop in types)
            {
                var childType = childTypeLoop;
                var name = GetDisplayName(childType);
                menu.AddItem(name, false, () =>
                {
                    if (!childType.IsDefined(typeof(SerializableAttribute)))
                    {
                        EditorUtility.DisplayDialog("", $"{childType.Name} does not have [Serializable] class attribute.\nIt is required to be serialized properly.", "OK");
                        return;
                    }
                    if (typeof(UnityEngine.Object).IsAssignableFrom(childType))
                    {
                        EditorUtility.DisplayDialog("", $"{childType.Name} needs to be a basic data class but it is a subclass of a Unity Object.", "OK");
                        return;
                    }
                    _runtimeSerializedObject.Update();
                    var obj = (IConsoleModule) Activator.CreateInstance(childType);
                    settings.modules.Add(obj);
                    _runtimeSerializedObject.Update();
                    SaveAndReloadCurrentPanel();
                });
            }
            menu.DropDown(btn.worldBound, btn, true);
        }

        void RemoveModuleBtnClicked(IConsoleModule moduleToRemove)
        {
            _drawnSetting.modules.Remove(moduleToRemove);
            _runtimeSerializedObject.Update();
            SaveAndReloadCurrentPanel();
        }

        void SaveAndReloadCurrentPanel()
        {
            _needsToSave = true;
            Save();
            EditorApplication.delayCall += ReloadCurrentPanel;
        }

        public void ReloadCurrentPanel()
        {
            GoToPanel(_currentIndex);
        }

        public static string FormatModuleName(Type type)
        {
            return "<size=14><b>" + GetDisplayName(type) + "</b></size>";
        }
        
        static string GetDisplayName(Type type)
        {
            var displayNameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
            return string.IsNullOrEmpty(displayNameAttribute?.DisplayName) ? type.Name : displayNameAttribute.DisplayName;
        }
        
        public override void OnInspectorUpdate()
        {
            if (CheckCompilationFailedMode(_rootElement))
            {
                // :(
            }
            else if (SyncObjectInstance())
            {
                ReloadCurrentPanel();
            }
        }
        
        public override void OnDeactivate()
        {
            Save();
        }

        void Save()
        {
            if (_lockSave || _runtimeSerializedObject == null)
            {
                return;
            }
            ConsoleEditorSettings.Get().Save();
            EditorUtility.SetDirty(_drawnSetting);
            AssetDatabase.SaveAssets();
        }

        void ApplyChangesButton(string text = null)
        {
            var btn = new Button(() =>
            {
                _lockSave = true;
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            })
            {
                text = string.IsNullOrEmpty(text) ? "Apply changes" : text
            };
            btn.style.marginTop = 20;
            btn.style.width = 180;
            btn.style.height = 40;
            _panelContent.Add(btn);
        }

        static int LastPanelIndex
        {
            get => EditorPrefs.GetInt("njcEditor.panel");
            set => EditorPrefs.SetInt("njcEditor.panel", value);
        }
#endif

        void AddTitle(VisualElement rootElement)
        {
            var title = new Label("NjConsole");
            title.style.paddingLeft = 10;
            title.style.paddingTop = 1;
            title.style.paddingBottom = 7;
            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            rootElement.Add(title);
        }

        bool _compilationFailed;
        bool CheckCompilationFailedMode(VisualElement rootElement)
        {
            if (!_compilationFailed && EditorUtility.scriptCompilationFailed && !EditorApplication.isCompiling && rootElement != null)
            {
                _compilationFailed = true;
                rootElement.Clear();
                AddTitle(rootElement);
                rootElement.Add(new HelpBox("<b>Compile errors detected.</b>\nPlease check the Console window and fix any script errors before proceeding.", HelpBoxMessageType.Error));
                if (ConsoleEditorSettings.HasDefineSymbolToDisableConsole())
                {
                    AddEnableConsoleButton(rootElement);
                }
            }
            return _compilationFailed;
        }

        public static void AddEnableConsoleButton(VisualElement rootElement)
        {
            var btn = new Button(() =>
            {
                ConsoleEditorSettings.RemoveDefineSymbolAndEnableConsole();
                PostScriptDefineChange();
            });
            btn.text = "Enable NjConsole";
            btn.style.backgroundColor = new Color(0.1f, 0.5f, 0.1f);
            btn.style.width = 170;
            btn.style.height = 22;
            rootElement.Add(btn);
        }

        static void PostScriptDefineChange()
        {
            EditorUtility.DisplayDialog(
                "",
                "If Unity doesn't automatically recompile, try entering Play Mode briefly to trigger it manually.",
                "OK"
            );
            EditorApplication.delayCall += () =>
            {
                var path = AssetDatabase.GUIDToAssetPath("80d603fa6e7f4147b486adbc91bbe63f");
                if(!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.ImportAsset(path);
                }
                AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            };
        }
                            
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new ConsoleEditorSettingsUI();
        }
    }
}