#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using Ninjadini.Console.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleUtilitiesElement : VisualElement
    {
        readonly ConsoleContext _context;

        readonly ScrollView _menuScroll;
        Button _currentBtn;
        VisualElement _currentPanel;

        List<Button> _buttons = new List<Button>();
        
        public ConsoleUtilitiesElement(ConsoleContext context)
        {
            _context = context;
            AddToClassList("panel-stretched");

            _menuScroll = new ScrollView();
            Add(_menuScroll);
            
            AddMenus();
            
            ConsoleUIUtils.RegisterOrientationCallback(context.Window != null ? context.Window : this, this, (isPortrait) =>
            {
                style.flexDirection = isPortrait ? FlexDirection.Column : FlexDirection.Row;
                _menuScroll.mode = isPortrait ? ScrollViewMode.Horizontal : ScrollViewMode.Vertical;
                ConsoleUIUtils.SwitchClass(_menuScroll, "info-menu-scroll", "info-menu-scroll-portrait", isPortrait);
                ConsoleUIUtils.SwitchClass(_menuScroll.contentContainer, "info-menu-inside", "info-menu-inside-portrait", isPortrait);
            });
            
            var prevBtnName = _context.Storage.GetString(StandardStorageKeys.InfoLastScreen);
            Button initialBtn = null;
            if (!string.IsNullOrEmpty(prevBtnName))
            {
                initialBtn = _buttons.Find(b => b.text == prevBtnName);
            }
            initialBtn ??= _buttons.First();
            OnSelected(initialBtn);
        }

        protected virtual void AddMenus()
        {
            AddMenu("Tools", () => CreateUtilitiesPanel(_context));
            AddMenu("Player Prefs", () => new PlayerPrefDebugger(_context));
            AddMenu("App Info", () => CreateAppInfoPanel(_context));
            AddMenu("Screen Info", () => CreateScreenInfoPanel(_context));
            AddMenu("Device Info", CreateDeviceInfoPanel);
            AddMenu("QualitySettings", () => CreateQualityInfoPanel(_context));
            AddMenu("Graphics Info", CreateGraphicsInfoPanel);
            AddMenu("Other SystemInfo", CreateOtherSystemInfoPanel);
            AddMenu("About", ShowAbout);
        }

        void AddMenu(string btnName, Func<VisualElement> callback)
        {
            var btn = new Button();
            btn.text = btnName;
            btn.userData = callback;
            btn.clicked += () => OnSelected(btn);
            btn.AddToClassList("info-menu-btn");
            _menuScroll.Add(btn);
            _buttons.Add(btn);
        }

        void OnSelected(Button btn)
        {
            if (_currentBtn != null)
            {
                _currentBtn.RemoveFromClassList("selected");
            }

            if (_currentPanel != null)
            {
                _currentPanel.RemoveFromHierarchy();
                _currentPanel = null;
            }
            _currentBtn = btn;
            if (btn == null)
            {
                return;
            }
            _context.Storage.SetString(StandardStorageKeys.InfoLastScreen, btn.text);
            _currentBtn.AddToClassList("selected");
            var visualElement = btn.userData is Func<VisualElement> callback ? callback() : null;
            if (visualElement != null)
            {
                _currentPanel = visualElement;
                Add(_currentPanel);
            }
        }

        VisualElement ShowAbout()
        {
            var container = new ScrollView();
            container.AddToClassList("info-info-panel");
            var width = 120;

            var lbl = new Label("<size=28><b>Ninjadini Debug Console</b></size>\n" +
                                $"<size=18>Version: {ConsoleSettings.Version}</size>");
            container.Add(lbl);

#if UNITY_EDITOR
            AddButton(container, "Documentation\n(File)", ConsoleUIUtils.GotoEditorLocalDoc, customWidth: width);
#endif
            
            AddButton(container, "Documentation\n(Web)", () =>
            {
                Application.OpenURL(ConsoleSettings.WebDocsURL);
            }, customWidth: width);
            
            AddButton(container, "Unity\nAsset Store", () =>
            {
                Application.OpenURL(ConsoleSettings.AssetStoreURL);
            }, customWidth: width);
            
            return container;
        }

        static bool IsInspectorEnabled(ConsoleContext context) => Application.isEditor || context.Settings.inPlayerObjectInspector;
    }
}
#endif