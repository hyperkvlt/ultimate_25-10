using System;
using System.Collections.Generic;
using Ninjadini.Console.UI;
using UnityEditor;
using UnityEngine;
#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace Ninjadini.Console.Editor
{
    public class NjConsoleEditorWindow : EditorWindow
#if !NJCONSOLE_DISABLE
        , ConsoleContext.IStorage, IHasCustomMenu
#endif
    {
        public const string IconString = "⌨";
        public const string MenuPath = "Window/⌨ Ninjadini Console";
        public const string WindowName = "⌨ NjConsole";

        [SerializeField] string lockedTitle;
        [SerializeField] List<KeyAndString> strings = new List<KeyAndString>();
        [SerializeField] List<KeyAndInt> ints = new List<KeyAndInt>();
        
/*
        [MenuItem(MenuPath)]
        public static void OpenExistingWindow()
        {
            GetWindow<NjConsoleEditorWindow>(WindowName).Show();
        }
*/

        [MenuItem(MenuPath + " (new window)")]
        public static void OpenNewWindow()
        {
            CreateWindow<NjConsoleEditorWindow>(WindowName).Show();
        }
        
        public static NjConsoleEditorWindow GetOrCreateWindow()
        {
            var window = GetWindow<NjConsoleEditorWindow>(WindowName);
            window.Show();
            return window;
        }
        
        public static NjConsoleEditorWindow CreateWindow()
        {
            var window = GetWindow<NjConsoleEditorWindow>(WindowName);
            window.Show();
            return window;
        }
        
#if NJCONSOLE_DISABLE
        public void CreateGUI()
        {
            rootVisualElement.Add(new UnityEngine.UIElements.HelpBox("NjConsole is disabled via <u>NJCONSOLE_DISABLE</u> scripting define.", 
                UnityEngine.UIElements.HelpBoxMessageType.Error));
            ConsoleEditorSettingsUI.AddEnableConsoleButton(rootVisualElement);
        }
#else

        public ConsoleContext Context { get; private set; }
        public ConsoleWindow Window { get; private set; }

        public void SetLockedToSinglePanel(bool wantsToLock)
        {
            Window.SetLockedToSinglePanel(wantsToLock);
            UpdateTitle();
        }

        public void CreateGUI()
        {
            if (Context != null)
            {
                throw new Exception("Gui Elements already created");
            }

            Context = new ConsoleContext(NjConsole.Modules, this, rootVisualElement);
            rootVisualElement.styleSheets.Add(Context.StyleSheet);
            Window = new ConsoleWindow(Context);
            Context.Window = Window;
            rootVisualElement.Add(Window);
            UpdateTitle();
        }

        void Update()
        {
            Repaint();
        }

        void ConsoleContext.IStorage.SetString(string key, string value)
        {
            for (var index = 0; index < strings.Count; index++)
            {
                var kv = strings[index];
                if (kv.Key == key)
                {
                    if (value == null)
                    {
                        strings.RemoveAt(index);
                    }
                    else
                    {
                        kv.Value = value;
                        strings[index] = kv;
                    }

                    return;
                }
            }

            strings.Add(new KeyAndString()
            {
                Key = key,
                Value = value
            });
        }

        string ConsoleContext.IStorage.GetString(string key)
        {
            foreach (var kv in strings)
            {
                if (kv.Key == key)
                {
                    return kv.Value;
                }
            }

            return null;
        }

        void ConsoleContext.IStorage.SetInt(string key, int value)
        {
            for (var index = 0; index < ints.Count; index++)
            {
                var kv = ints[index];
                if (kv.Key != key)
                {
                    continue;
                }

                if (value == 0)
                {
                    ints.RemoveAt(index);
                }
                else
                {
                    kv.Value = value;
                    ints[index] = kv;
                }

                return;
            }

            ints.Add(new KeyAndInt()
            {
                Key = key,
                Value = value
            });
        }

        int ConsoleContext.IStorage.GetInt(string key, int defaultValue)
        {
            foreach (var kv in ints)
            {
                if (kv.Key == key)
                {
                    return kv.Value;
                }
            }

            return defaultValue;
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Locked to single panel"), Window?.IsLockedToSinglePanel() ?? false,
                OnLockToSinglePanelMenuClicked);

            menu.AddItem(new GUIContent("New Window"), false, () => OpenNewWindow());

            menu.AddItem(new GUIContent("⌨ Settings"), false, () =>
            {
                SettingsService.OpenProjectSettings(ConsoleEditorSettings.SettingsMenuPath);
            });
        }

        void OnLockToSinglePanelMenuClicked()
        {
            var wantsLock = !Window.IsLockedToSinglePanel();
            SetLockedToSinglePanel(wantsLock);
        }

        void UpdateTitle()
        {
            var str = WindowName;
            if (Window.IsLockedToSinglePanel())
            {
                var panelModuleName = Window.ActivePanel?.Name;
                if (!string.IsNullOrEmpty(panelModuleName))
                {
                    str = IconString + " " + panelModuleName;
                    lockedTitle = str;
                }
                else if(!string.IsNullOrEmpty(lockedTitle))
                {
                    str = lockedTitle;
                }
            }
            else
            {
                lockedTitle = null;
            }

            titleContent = new GUIContent(str);
        }
#endif
        
        [Serializable]
        struct KeyAndString
        {
            public string Key;
            public string Value;
        }

        [Serializable]
        struct KeyAndInt
        {
            public string Key;
            public int Value;
        }
    }
}