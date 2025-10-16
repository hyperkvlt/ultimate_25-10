#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using Ninjadini.Console.Internal;
using Ninjadini.Logger.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public class ConsoleWindow : VisualElement
    {
        public readonly ConsoleContext Context;
        public ConsoleModules Modules => Context?.Modules;

        readonly VisualElement _sideBar;
        readonly ScrollView _sideBarScroll;
        readonly Button _showSideBarBtn;
        readonly List<PanelData> _panels = new List<PanelData>();
        VisualElement _scaleBtn;
        PanelData _activePanel;
        bool _panelsDirty;
        VisualElement _nonPanelToDisplayEle;
        IVisualElementScheduledItem _opacitySchedule;
        float _opacityDelay;

        const int MinWindowedWidth = 550;
        const int MinWindowedHeight = 250;

        public ConsoleWindow(ConsoleContext context)
        {
            name = "ConsoleWindow";
            Context = context ?? throw new ArgumentNullException(nameof(context));
            if (!styleSheets.Contains(context.StyleSheet))
            {
                styleSheets.Add(context.StyleSheet);
            }
            AddToClassList("fullscreen");

            var settings = context.Settings;
            UnityToNjLogger.Start(settings.inPlayerLogMode, settings.maxLogsHistory);

            style.flexGrow = 1;
            
            _sideBar = new VisualElement();
            _sideBar.AddToClassList("main-sidebar");
            Add(_sideBar);
            
            _sideBarScroll = new ScrollView();
            _sideBarScroll.style.flexGrow = 1;
            _sideBarScroll.style.flexShrink = 1;
            _sideBar.Add(_sideBarScroll);

            _showSideBarBtn = new Button(OnShowSideBarClicked)
            {
                text = "\u25b6"
            };
            _showSideBarBtn.style.display = DisplayStyle.None;
            Add(_showSideBarBtn);
            
            if (context.IsRuntimeOverlay)
            {
                _sideBar.RegisterCallback<PointerDownEvent>(OnSideBarPointerDown);
                _sideBar.RegisterCallback<PointerUpEvent>(OnSideBarPointerUp);
                
                _showSideBarBtn.RegisterCallback<PointerDownEvent>(OnSideBarPointerDown, TrickleDown.TrickleDown);
                _showSideBarBtn.RegisterCallback<PointerUpEvent>(OnSideBarPointerUp, TrickleDown.TrickleDown);
            }

            var endBtns = new VisualElement();
            endBtns.style.flexGrow = 0;
            endBtns.style.flexShrink = 0;
            _sideBar.Add(endBtns);
/*
            var hideSideBarBtn = new Button(OnHideSideBarClicked);
            hideSideBarBtn.AddToClassList("monoFont");
            endBtns.Add(hideSideBarBtn);
*/
            var minMaxBtn = new Button(OnMinMaxClicked);
            minMaxBtn.style.fontSize = 16;
            minMaxBtn.AddToClassList("monoFont");
            endBtns.Add(minMaxBtn);

            Button closeBtn = null;
            if (context.RuntimeOverlay != null)
            {
                closeBtn = new Button(context.RuntimeOverlay.Hide)
                { 
                    text = "X"
                };
                endBtns.Add(closeBtn);
            }

            var logsModule = Modules.GetModule<ConsoleLogsPanel.Module>(false);
            if(logsModule == null)
            {
                Modules.AddModule(new ConsoleLogsPanel.Module());
            }
            ConsoleUIUtils.RegisterOrientationCallback(this, this, (isPortrait) =>
            {
                style.flexDirection = isPortrait ? FlexDirection.Column : FlexDirection.Row;
                endBtns.style.flexDirection = isPortrait ? FlexDirection.Row : FlexDirection.Column;
                ConsoleUIUtils.SwitchClass(_sideBar, "main-sidebar-landscape", "main-sidebar-portrait", isPortrait);
                ConsoleUIUtils.SwitchClass(_sideBarScroll.contentContainer, "main-sidebar-inside", "main-sidebar-inside-portrait", isPortrait);
                ConsoleUIUtils.SwitchClass(_showSideBarBtn, "main-sidebar-showBtn", "main-sidebar-showBtn-portrait", isPortrait);
                //ConsoleUIUtils.SwitchClass(hideSideBarBtn, "main-sidebar-hideBtn", "main-sidebar-hideBtn-portrait", isPortrait);
                ConsoleUIUtils.SwitchClass(minMaxBtn, "main-sidebar-minmaxBtn", "main-sidebar-minmaxBtn-portrait", isPortrait);
                if (closeBtn != null)
                {
                    ConsoleUIUtils.SwitchClass(closeBtn, "main-sidebar-closeBtn", "main-sidebar-closeBtn-portrait", isPortrait);
                }
                if (IsBigScreen() || !IsFullScreen)
                {
                    //ConsoleUIUtils.SwitchClass(_minMaxBtn, "main-sidebar-hideBtn", "main-sidebar-hideBtn-portrait", isPortrait);
                    minMaxBtn.text = IsFullScreen ? "\u229f" : "\u229e";
                    minMaxBtn.style.display = DisplayStyle.Flex;
                }
                else
                {
                    minMaxBtn.style.display = DisplayStyle.None;
                }
                _showSideBarBtn.text = isPortrait ? "\u25bc" : "\u25b6";
                //hideSideBarBtn.text = isPortrait ? "\u25b2" : "\u25c0";
                _sideBarScroll.mode = isPortrait ? ScrollViewMode.Horizontal : ScrollViewMode.Vertical;
            });
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            
#if !UNITY_2022_3_OR_NEWER
            WarnAboutPre2022_3();
#endif
        }

        bool IsBigScreen()
        {
            var rectV = Context.RuntimeOverlay?.contentRect;
            if (rectV == null) return false;
            var size = rectV.Value.size;
            return Mathf.Max(size.x, size.y) > MinWindowedWidth + 200 && Mathf.Min(size.x, size.y) > MinWindowedWidth + 50;
        }
/*
        void OnHideSideBarClicked()
        {
            _sideBar.style.display = DisplayStyle.None;
            _showSideBarBtn.style.display = DisplayStyle.Flex;
        }
*/

        void OnMinMaxClicked()
        {
            IsFullScreen = !IsFullScreen;
        }

        void OnShowSideBarClicked()
        {
            _sideBar.style.display = DisplayStyle.Flex;
            _showSideBarBtn.style.display = DisplayStyle.None;
        }

        void OnSideBarPointerDown(PointerDownEvent evt)
        {
            var target = (VisualElement) evt.currentTarget;
            target.CapturePointer(evt.pointerId);
            _sideBarMouseDownPos = evt.position;
            StartOpacityCountDown();
            if (!IsFullScreen)
            {
                target.RegisterCallback<PointerMoveEvent>(OnSideBarPointerMove);
            }
        }

        Vector3 _sideBarMouseDownPos;
        void OnSideBarPointerUp(PointerUpEvent evt)
        {
            if (style.opacity.value < 0.5f || (_sideBarMouseDownPos - evt.position).magnitude > 5f)
            {
                evt.StopPropagation();
            }
            var target = (VisualElement) evt.currentTarget;
            target.ReleasePointer(evt.pointerId);
            _opacitySchedule?.Pause();
            style.opacity = 1f;
            target.UnregisterCallback<PointerMoveEvent>(OnSideBarPointerMove);
            SaveLatestWindowPosAndSize();
        }

        void OnSideBarPointerMove(PointerMoveEvent evt)
        {
            if (IsFullScreen) return;
            if ((_sideBarMouseDownPos - evt.position).magnitude > 3f)
            {
                _opacitySchedule?.Pause();
                style.opacity = 1f;
            }
            var delta = evt.deltaPosition;
            ApplyWindowDelta(delta.x, delta.y, 0, 0);
        }

        void StartOpacityCountDown()
        {
            _opacityDelay = 0.15f;
            if (_opacitySchedule == null)
            {
                _opacitySchedule = schedule.Execute((time) =>
                {
                    if (_opacityDelay > 0)
                    {
                        _opacityDelay -= time.deltaTime * 0.001f;
                        return;
                    }
                    var opt = float.IsNaN(style.opacity.value) ? 1f : style.opacity.value;
                    opt -= time.deltaTime * 0.005f;
                    style.opacity = Mathf.Max(0.15f, opt);
                }).Every(1);
            }
            else
            {
                _opacitySchedule.Resume();
            }
        }

        public bool IsFullScreen
        {
            get => style.overflow != Overflow.Hidden;
            set
            {
                if (value)
                {
                    style.overflow = Overflow.Visible;
                    style.left = 0;
                    style.top = 0;
                    style.right = 0;
                    style.bottom = 0;
                    style.width = StyleKeyword.Auto;
                    style.height = StyleKeyword.Auto;
                    _scaleBtn?.RemoveFromHierarchy();
                }
                else
                {
                    style.overflow = Overflow.Hidden;
                    CreateSizerBtn();
                    parent?.Insert(parent.IndexOf(this), _scaleBtn);
                    var rect = StoredWindowRect;
                    ApplyWindowRect(ref rect, false);
                }
                StoredWindowed = !value;
            }
        }

        void CreateSizerBtn()
        {
            if (_scaleBtn != null) return;
            
            _scaleBtn = new VisualElement();
            _scaleBtn.name = "sizer";
            
            var c = new Color(1f, 193/255f, 0.01f);

            var child = new VisualElement();
            child.style.backgroundColor = c;
            child.style.position = Position.Absolute;
            child.style.left = 15;
            child.style.width = 3;
            child.style.height = 18;
            _scaleBtn.Add(child);

            child = new VisualElement();
            child.style.backgroundColor = c;
            child.style.position = Position.Absolute;
            child.style.top = 15;
            child.style.width = 18;
            child.style.height = 3;
            _scaleBtn.Add(child);
                        
            _scaleBtn.style.backgroundColor =new Color(0f, 0f, 0f, 1/255f);
            _scaleBtn.style.position = Position.Absolute;
            _scaleBtn.style.width = 28;
            _scaleBtn.style.height = 28;
            _scaleBtn.style.paddingBottom = 0;
            _scaleBtn.style.paddingRight = 0;
            _scaleBtn.pickingMode = PickingMode.Position;
            _scaleBtn.RegisterCallback<PointerDownEvent>(OnScalingPointerDown);
            _scaleBtn.RegisterCallback<PointerUpEvent>(OnScalingPointerUp);
        }

        void OnScalingPointerDown(PointerDownEvent evt)
        {
            _scaleBtn.CapturePointer(evt.pointerId);
            _scaleBtn.RegisterCallback<PointerMoveEvent>(OnScalingPointerMove);
        }

        void OnScalingPointerMove(PointerMoveEvent evt)
        {
            var delta = evt.deltaPosition;
            ApplyWindowDelta(0, 0, delta.x, delta.y);
        }

        void OnScalingPointerUp(PointerUpEvent evt)
        {
            _scaleBtn.UnregisterCallback<PointerMoveEvent>(OnScalingPointerMove);
            _scaleBtn.ReleasePointer(evt.pointerId);
            SaveLatestWindowPosAndSize();
        }
        

        void ApplyWindowDelta(float x, float y, float width, float height)
        {
            if (IsFullScreen) return;
            var rect = new Rect(style.left.value.value + x, style.top.value.value + y, resolvedStyle.width + width, resolvedStyle.height + height);
            ApplyWindowRect(ref rect, false);
        }

        public void RefreshWindowSize()
        {
            if(IsFullScreen) return;
            if (IsBigScreen())
            {
                var rect = StoredWindowRect;
                ApplyWindowRect(ref rect, true);
            }
            else
            {
                IsFullScreen = true;
            }
        }

        void ApplyWindowRect(ref Rect rect, bool canStretch)
        {
            var parentSize = parent.contentRect.size;
            
            var w = rect.width <= 10 ? parentSize.x : Math.Max(MinWindowedWidth, Math.Min(rect.width, parentSize.x));
            var h = Math.Max(MinWindowedHeight, Math.Min(rect.height, parentSize.y - 10));
            
            var x = Mathf.Clamp(rect.x, 0, parentSize.x - w);
            var y = Mathf.Clamp(rect.y, 0, parentSize.y - h);

            style.top = y;
            style.height = h;
            if (canStretch && x <= 2 && w >= parentSize.x - 3f)
            {
                style.left = 0;
                style.width = StyleKeyword.Auto;
                style.right = 0;
                rect.width = 0;
            }
            else
            {
                style.left = x;
                style.width = w;
                style.right = StyleKeyword.Auto;
            }
            if (_scaleBtn != null)
            {
                _scaleBtn.style.left = x + w - 16;
                _scaleBtn.style.top = y + h - 16;
            }
        }

        void SaveLatestWindowPosAndSize()
        {
            if (IsFullScreen) return;
            var left = Mathf.Floor(style.left.value.value);
            var top = Mathf.Floor(style.top.value.value);
            var width = Mathf.Floor(style.width.value.value);
            var height = Mathf.Floor(style.height.value.value);
            var rect = new Rect(left, top, width, height);
            ApplyWindowRect(ref rect, true);
            StoredWindowRect = rect;
            PlayerPrefs.Save();
        }

        Rect StoredWindowRect
        {
            get => new(PlayerPrefs.GetInt("njcWinX", 10), PlayerPrefs.GetInt("njcWinY", 10), PlayerPrefs.GetInt("njcWinW", MinWindowedWidth + 20), PlayerPrefs.GetInt("njcWinH", MinWindowedHeight));
            set
            {
                PlayerPrefs.SetInt("njcWinX", (int)value.x);
                PlayerPrefs.SetInt("njcWinY", (int)value.y);
                PlayerPrefs.SetInt("njcWinW", (int)value.width);
                PlayerPrefs.SetInt("njcWinH", (int)value.height);
            }
        }

        bool StoredWindowed
        {
            get => Context.IsRuntimeOverlay && PlayerPrefs.GetInt(StandardStorageKeys.Windowed) > 0;
            set
            {
                if (Context.IsRuntimeOverlay)
                {
                    PlayerPrefs.SetInt(StandardStorageKeys.Windowed, value ? 1 : 0);
                }
            }
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (Modules != null)
            {
                Modules.ModuleAdd += OnModuleAddedOrRemove;
                Modules.ModuleRemoved += OnModuleAddedOrRemove;
                
                style.opacity = 1f;
                IsFullScreen = !(StoredWindowed && IsBigScreen());
                RefreshPanels(true);
            }
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (Modules != null)
            {
                Modules.ModuleAdd -= OnModuleAddedOrRemove;
                Modules.ModuleRemoved -= OnModuleAddedOrRemove;
                _scaleBtn?.RemoveFromHierarchy();
            }
        }

        void OnModuleAddedOrRemove(IConsoleModule module)
        {
            if (module is IConsolePanelModule && !_panelsDirty)
            {
                _panelsDirty = true;
                schedule.Execute(DelayedRefreshPanels).ExecuteLater(100);
            }
        }

        void DelayedRefreshPanels()
        {
            RefreshPanels(false);
        }

        void RefreshPanels(bool retryLastPanel)
        {
            _panelsDirty = false;
            
            var lockedPanelName = Context.Storage.GetString(StandardStorageKeys.LockedPanelTypeName);

            for (var index = _panels.Count-1; index >= 0; index--)
            {
                var data = _panels[index];
                if (Modules.HasModule(data.Control)
                    && (string.IsNullOrEmpty(lockedPanelName) || data.Control.GetType().FullName == lockedPanelName))
                {
                    continue;
                }
                _panels.RemoveAt(index);
                data.Element?.RemoveFromHierarchy();
                _sideBarScroll.Remove(data.Button);
                if(_activePanel == data)
                {
                    _activePanel = null;
                }
            }
            foreach (var panelModule in Modules.AllModules
                         .Select(kv => kv.Value)
                         .OfType<IConsolePanelModule>()
                         .Where(m => m.PanelFeatureEnabled)
                         .OrderBy(m => m.SideBarOrder))
            {
                if(!string.IsNullOrEmpty(lockedPanelName) && panelModule.GetType().FullName != lockedPanelName)
                {
                    continue;
                }
                var data = _panels.Find(p => p.Control == panelModule);
                if (data != null)
                {
                    data.Button.RemoveFromHierarchy();
                    _sideBarScroll.Add(data.Button);
                    continue;
                }
                data = new PanelData
                {
                    Control = panelModule
                };
                _panels.Add(data);
                var btn = data.Control.CreateSideButton(Context, () =>
                {
                    SetActivePanel(data);
                });
                if (btn != null)
                {
                    data.Button = btn;
                    btn.AddToClassList("main-sidebar-btn");
                    _sideBarScroll.Add(btn);
                }
            }
            if(_activePanel == null)
            {
                if (TryReopenLastPanel(retryLastPanel))
                {
                    // all good now. (lastPanelName worked)
                }
                else if (_panels.Count > 0)
                {
                    SetActivePanel(_panels[0]);
                }
                else
                {
                    ShowNoPanelsToDisplay();
                }
            }
            _sideBar.style.display = _panels.Count > 1 
                                     || string.IsNullOrEmpty(Context.Storage.GetString(StandardStorageKeys.LockedPanelTypeName)) 
                ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ShowNoPanelsToDisplay()
        {
            if (_nonPanelToDisplayEle == null)
            {
                var txt = "<b>No panels to display yet</b>";
                var lockedPanelName = Context.Storage.GetString(StandardStorageKeys.LockedPanelTypeName);
                if (!string.IsNullOrEmpty(lockedPanelName))
                {
                    txt += $"\n(Locked to display [{lockedPanelName}] only)";
                }
                _nonPanelToDisplayEle = new Label(txt);
                _nonPanelToDisplayEle.style.unityTextAlign = TextAnchor.MiddleCenter;
                _nonPanelToDisplayEle.AddToClassList("panel");
                _nonPanelToDisplayEle.AddToClassList("main-panel");
            }
            // this can easily happen if you have the window locked to a specific panel (in editor only)
            Add(_nonPanelToDisplayEle);
        }

        bool TryReopenLastPanel(bool retryAfterDelay)
        {
            var lastPanelName = Context.Storage.GetString(StandardStorageKeys.LastPanelTypeName); 
            if (!string.IsNullOrEmpty(lastPanelName))
            {
                foreach (var panelData in _panels)
                {
                    if (panelData.Control.Name == lastPanelName)
                    {
                        SetActivePanel(panelData);
                        return true;
                    }
                }
                if (retryAfterDelay)
                {
                    schedule.Execute(() =>
                    {
                        if (_activePanel == null 
                            && !TryReopenLastPanel(false) 
                            && _activePanel == null 
                            && _panels.Count > 0)
                        {
                            SetActivePanel(_panels[0]);
                        }
                        else if(_panels.Count == 0)
                        {
                            ShowNoPanelsToDisplay();
                        }
                    }).ExecuteLater(100);
                    return true;
                }
            }
            return false;
        }

        void SetActivePanel(PanelData value)
        {
            if(_activePanel == value)
            {
                if (value.Element is IConsolePanelModule.IElement panelElement) panelElement.OnReselected();
                return;
            }
            RemoveActivePanel();
            _activePanel = value;
            if (_activePanel.Button != null)
            {
                _activePanel.Button.AddToClassList("selected");
            }
            var element = value.Element;
            if (element == null)
            {
                element = value.Control.CreateElement(Context);
                if (element == null)
                {
                    return;
                }
                element.AddToClassList("main-panel");
                value.Element = element;
            }
            Add(element);
            //if (element is IConsolePanelModule.IElement addedPanel) addedPanel.OnShown();
            Context.Storage.SetString(StandardStorageKeys.LastPanelTypeName, value.Control.Name);
        }

        void RemoveActivePanel()
        {
            if (_nonPanelToDisplayEle != null && _nonPanelToDisplayEle.parent != null)
            {
                _nonPanelToDisplayEle.RemoveFromHierarchy();
            }
            if (_activePanel == null)
            {
                return;
            }
            var activeElement = _activePanel.Element;
            if (activeElement != null)
            {
                activeElement.RemoveFromHierarchy();
            }
            if (_activePanel.Button != null)
            {
                _activePanel.Button.RemoveFromClassList("selected");
            }
        }

        /// Set locked/unlocked to current active panel
        public void SetLockedToSinglePanel(bool locked)
        {
            if (locked)
            {
                var modName = _activePanel?.Control?.GetType().FullName;
                Context.Storage.SetString(StandardStorageKeys.LockedPanelTypeName, modName);
                _showSideBarBtn.style.display = DisplayStyle.None;
            }
            else
            {
                Context.Storage.SetString(StandardStorageKeys.LockedPanelTypeName, null);
                OnShowSideBarClicked();
            }
            RefreshPanels(false);
        }
        
        public bool IsLockedToSinglePanel() => !string.IsNullOrEmpty(Context.Storage.GetString(StandardStorageKeys.LockedPanelTypeName));

        public IConsolePanelModule ActivePanel => _activePanel?.Control;
        public VisualElement ActivePanelElement => _activePanel?.Element;

        public bool SideBarShown => _sideBar.style.display == DisplayStyle.Flex;

        public bool SetActivePanel<T>() where T : IConsolePanelModule
        {
            return SetActivePanel(typeof(T));
        }
        
        public bool SetActivePanel(Type panelModule)
        {
            foreach (var panelData in _panels)
            {
                if (panelData.Control.GetType() == panelModule)
                {
                    SetActivePanel(panelData);
                    return true;
                }
            }
            return false;
        }
        
        public bool SetActivePanel(string panelName)
        {
            foreach (var panelData in _panels)
            {
                if (panelData.Control.Name == panelName)
                {
                    SetActivePanel(panelData);
                    return true;
                }
            }
            return false;
        }

        public bool SetActivePanel(IConsolePanelModule panelModule)
        {
            foreach (var panelData in _panels)
            {
                if (panelData.Control == panelModule)
                {
                    SetActivePanel(panelData);
                    return true;
                }
            }
            return false;
        }

        public ConsoleCommandLineElement OpenAndFocusOnCommandLine()
        {
            if (SetActivePanel<ConsoleLogsPanel.Module>() 
                && ActivePanelElement is ConsoleLogsPanel logsPanel)
            {
                logsPanel.CloseDetails();
                logsPanel.schedule.Execute(logsPanel.ScrollToBottom);
                logsPanel.CommandLineElement?.Show();
                return logsPanel.CommandLineElement;
            }
            return null;
        }
        
        class PanelData
        {
            public IConsolePanelModule Control;
            public VisualElement Element;
            public VisualElement Button;
        }
        
#if !UNITY_2022_3_OR_NEWER
        static bool _warnedAboutEarlyUnity;
        void WarnAboutPre2022_3()
        {
            if (_warnedAboutEarlyUnity) return;
            _warnedAboutEarlyUnity = true;
#if UNITY_2022_2_OR_NEWER
            Ninjadini.Logger.NjLogger.Warn("WARNING: Unity 2022.2 has known issues with log selection in the Logs panel. For full functionality, please upgrade to Unity 2022.3 or newer.");
#else
            Ninjadini.Logger.NjLogger.Warn("WARNING: Unity 2022.1 has known issues with dropdown selection in player builds. Please upgrade to Unity 2022.3 or newer for proper functionality.");
#endif
        }
#endif
    }
}
#endif