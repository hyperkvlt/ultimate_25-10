
using System;
using Ninjadini.Console.Internal;
using Ninjadini.Console.UI;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Ninjadini.Console
{
#if !NJCONSOLE_DISABLE
    public class ConsoleOverlay : VisualElement
    {
        static ConsoleOverlay _instance;
        static bool? _accessChallengeRequired;

        public static bool HasInstance => _instance != null && _instance.UIDocument;

        public static ConsoleOverlay Instance => HasInstance ? _instance : null;

        public readonly ConsoleContext Context;
        public readonly UIDocument UIDocument;
        public GameObject GameObject => UIDocument ? UIDocument.gameObject : null;
        public readonly ConsoleScreenShortcuts Shortcuts;

        bool _askedTriggers;
        ConsoleWindow _window;
        ErrorLogHandler _errorLogHandler;

        public static ConsoleOverlay GetOrCreateInstance()
        {
            if (!HasInstance)
            {
                if (!Application.isPlaying)
                {
                    throw new Exception("ConsoleOverlay is only for play mode");
                }
                var gameObject = new GameObject(ConsoleSettings.GameObjectName);
                Object.DontDestroyOnLoad(gameObject);
                var document = gameObject.AddComponent<UIDocument>();
                var panelSettings = Resources.Load<PanelSettings>(ConsoleSettings.PanelSettingsResName);
                if (panelSettings == null)
                {
                    Debug.LogError($"NjConsole can not find `{ConsoleSettings.PanelSettingsResName}` in Resources. Console will not be visible.");
                }
                if (Application.isEditor && panelSettings)
                {
                    var name = panelSettings.name;
                    panelSettings = Object.Instantiate(panelSettings);
                    panelSettings.name = name;
                    panelSettings.hideFlags = HideFlags.HideAndDontSave;
                }

                panelSettings.scale = StoredScale;
                document.panelSettings = panelSettings;
                _instance = new ConsoleOverlay(NjConsole.Modules, document);
                document.rootVisualElement.styleSheets.Add(_instance.Context.StyleSheet);
                document.rootVisualElement.Add(_instance);
#if UNITY_2022_2_OR_NEWER
                Debug.developerConsoleEnabled = false;
#endif
            }
            return _instance;
        }

        ConsoleOverlay(ConsoleModules modules, UIDocument uiDocument)
        {
            name = "ConsoleOverlay";
            UIDocument = uiDocument;
            Context = new ConsoleContext(modules, new ConsoleContext.PlayerPrefsStorage(), this, this);
            pickingMode = PickingMode.Ignore;
            style.flexGrow = 1f;
            Shortcuts = new ConsoleScreenShortcuts(this);
            ConsoleUIUtils.AutoApplySafeRegion(this, () => SafeAreaScale);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
            
            if (modules.Settings.behaviourOnError != ConsoleSettings.OverlayBehaviourOnError.Ignore)
            {
                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }
        }

        public void WaitForTriggersToShow()
        {
            if (_window != null || _askedTriggers)
            {
                return;
            }
            _askedTriggers = true;
            var console = Context.Modules;
            var noTriggers = true;
            foreach (var kv in console.AllModules)
            {
                if (kv.Value is IConsoleOverlayTrigger triggerListener)
                {
                    noTriggers = false;
                    triggerListener.ListenForTriggers(this);
                }
            }
            if (noTriggers)
            {
                ShowWithAccessChallenge();
            }
            else if(!IsAccessChallengeRequired() && Shortcuts.AutoShow && Shortcuts.HasItemsInCurrentGroup())
            {
                ShowShortcuts();
            }
        }

        public bool IsAccessChallengeRequired()
        {
            if (_window != null)
            {
                return false;
            }
            if (_accessChallengeRequired.HasValue)
            {
                return _accessChallengeRequired.Value;
            }
            var console = Context.Modules;
            foreach (var kv in console.AllModules)
            {
                if (kv.Value is IConsoleAccessChallenge accessChallenge)
                {
                    if (accessChallenge.IsAccessChallengeRequired())
                    {
                        _accessChallengeRequired = true;
                        return true;
                    }
                }
            }
            _accessChallengeRequired = false;
            return false;
        }

        public void ShowWithAccessChallenge()
        {
            if (_window != null) // already past checks
            {
                ShowWithoutAccessChallenge();
                return;
            }
            if (ShowingAnyChallenge())
            {
                return;
            }
            _errorLogHandler?.HideTopError();
            var console = Context.Modules;
            foreach (var kv in console.AllModules)
            {
                if (kv.Value is IConsoleAccessChallenge accessChallenge)
                {
                    if (accessChallenge.IsAccessChallengeRequired())
                    {
                        _accessChallengeRequired = true;
                        accessChallenge.ShowChallenge(ShowWithAccessChallenge);
                        return;
                    }
                }
            }
            _accessChallengeRequired = false;
            ShowWithoutAccessChallenge();
        }

        public bool ShowingAnyChallenge()
        {
            if (_accessChallengeRequired == false)
            {
                return false;
            }
            foreach (var kv in Context.Modules.AllModules)
            {
                if (kv.Value is IConsoleAccessChallenge { ShowingChallenge: true })
                {
                    return true;
                }
            }
            return false;
        }

        /// This shows the console without waiting for triggers or access challenges
        public void ShowWithoutAccessChallenge()
        {
            _errorLogHandler?.HideTopError();
            Shortcuts?.RemoveFromHierarchy();
            if (_window == null)
            {
                _window = new ConsoleWindow(Context);
                Context.Window = _window;
            }
            if (_window.parent != this)
            {
                Insert(0, _instance._window);
            }
        }

        public void Hide()
        {
            _window?.RemoveFromHierarchy();
            if (!Shortcuts.UserHidden)
            {
                Insert(0, Shortcuts);
            }
        }

        public void ShowShortcuts(bool inEditMode = false)
        {
            _window?.RemoveFromHierarchy();
            Insert(0, Shortcuts);
            if(inEditMode) Shortcuts.ShowEditWindow();
            else Shortcuts.Show();
        }

        internal void Toggle()
        {
            if (Showing) Hide();
            else ShowWithAccessChallenge();
        }
        
        public bool Showing => _window?.parent != null;

        public ConsoleWindow Window => _window;

        void OnGeometryChangedEvent(GeometryChangedEvent evt)
        {
            if (_window?.parent != null && !_window.IsFullScreen)
            {
                _window.RefreshWindowSize();
            }
            else if (Application.isEditor && UIDocument && UIDocument.panelSettings)
            {
                if (Mathf.Min(contentRect.width, contentRect.height) < 350 && Scale > 0.6f)
                {
                    // unity does 2x game view on higher dpi and it can be too blown up, this is a temp hack
                    UIDocument.panelSettings.scale = Mathf.Max(UIDocument.panelSettings.scale - 0.25f, 0.6f);
                }
            }
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _errorLogHandler ??= new ErrorLogHandler(this);
            NjLogger.AddHandler(_errorLogHandler);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (_errorLogHandler != null)
            {
                NjLogger.RemoveHandler(_errorLogHandler);
            }
        }

        public float Scale
        {
            get => UIDocument?.panelSettings?.scale ?? 1f;
            set
            {
                if (UIDocument && UIDocument.panelSettings)
                {
                    value = Mathf.Clamp(value, 0.2f, 5f);
                    StoredScale = value;
                    UIDocument.panelSettings.scale = StoredScale;
                }
            }
        }

        static float StoredScale
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(StandardStorageKeys.OverlayScale, 
                Application.isEditor ? 1.2f : 1f ),
                0.2f, 5f);
            set => PlayerPrefs.SetFloat(StandardStorageKeys.OverlayScale, Mathf.Clamp(value, 0.2f, 5f));
        }

        public float SafeAreaScale
        {
            get => StoredSafeAreaScale;
            set
            {
                if (Mathf.Approximately(StoredSafeAreaScale, value)) return;
                StoredSafeAreaScale = value;
                ConsoleUIUtils.ApplySafeArea(this, StoredSafeAreaScale);
            }
        }

        internal static float StoredSafeAreaScale
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(StandardStorageKeys.OverlaySafeArea, 0.7f ), 0f, 1.2f);
            set => PlayerPrefs.SetFloat(StandardStorageKeys.OverlaySafeArea, Mathf.Clamp(value, 0f, 1.2f));
        }

        public void HideErrorDisplayedAtTop()
        {
            _errorLogHandler?.HideTopError();
        }
        
        public void DisableOnErrorBehaviour()
        {
            _errorLogHandler?.DisableOnErrorBehaviour();
        }

        public bool ErrorBehaviourEnabled => _errorLogHandler?.Enabled ?? true;
        
        class ErrorLogHandler : NjLogger.IHandler
        {
            readonly ConsoleOverlay _overlay;
            readonly  ConsoleSettings _settings;

            DateTime _soonestErrorTime;
            string _queuedError;
            bool _disabled;
            VisualElement _topError;
            Label _topErrorLbl;
            Button _closeTopBtn;
            Button _disableTopBtn;

            public bool Enabled => !_disabled;

            public ErrorLogHandler(ConsoleOverlay overlay)
            {
                _overlay = overlay;
                _settings = overlay.Context.Settings;

                overlay.schedule.Execute(Update).Every(0);
            }

            void NjLogger.IHandler.HandleLog(ref NjLogger.LogRow logRow)
            {
                if (logRow.Level == NjLogger.Level.Error)
                {
                    TryQueueError(ref logRow);
                }
            }

            void NjLogger.IHandler.HandleException(Exception exception, ref NjLogger.LogRow logRow)
            {
                TryQueueError(ref logRow);
            }

            void TryQueueError(ref NjLogger.LogRow logRow)
            {
                if (_disabled || _queuedError != null)
                {
                    return;
                }
                var timeNow = DateTime.Now;
                if (timeNow > _soonestErrorTime)
                {
                    _soonestErrorTime = timeNow.AddSeconds(1);
                    // we queue it in case it came from a thread
                    _queuedError = logRow.GetString(LoggerUtils.TempStringBuilder);
                }
            }

            void Update()
            {
                if (_disabled || _queuedError == null)
                {
                    return;
                }
                var message = _queuedError;
                _queuedError = null;
                HandleError(message);
            }

            void HandleError(string message)
            {
                var behaviour = _settings?.behaviourOnError ?? ConsoleSettings.OverlayBehaviourOnError.Ignore;
                if (behaviour == ConsoleSettings.OverlayBehaviourOnError.ShowAtTop)
                {
                    ShowTopError(message);
                }
                /*
                else if (behaviour == ConsoleSettings.OverlayBehaviourOnError.ShowErrorToast)
                {
                    _soonestErrorTime = _soonestErrorTime.AddSeconds(3);
                    ShowToast(message);
                }*/
                else if (behaviour == ConsoleSettings.OverlayBehaviourOnError.ShowConsoleOverlay)
                {
                    _soonestErrorTime = _soonestErrorTime.AddSeconds(10);
                    TryShowConsoleLogs();
                }
            }

            void ShowTopError(string message)
            {
                if (_topError == null)
                {
                    _topError = new VisualElement();
                    _topError.AddToClassList("topErrorDisplay");
                    _topError.RegisterCallback<ClickEvent>(OnTopErrorClicked);
                    
                    _topErrorLbl = new Label();
                    _topErrorLbl.style.flexGrow = 1;
                    _topErrorLbl.style.flexShrink = 1;
                    _topErrorLbl.style.maxHeight = 60;
                    _topErrorLbl.style.unityTextAlign = TextAnchor.UpperLeft;
                    _topErrorLbl.style.whiteSpace = WhiteSpace.Normal;
                    _topErrorLbl.style.overflow = Overflow.Hidden;
                    _topErrorLbl.style.textOverflow = TextOverflow.Ellipsis;
                    _topError.Add(_topErrorLbl);
                    
                    _disableTopBtn = new Button(OnDisableClicked);
                    _disableTopBtn.text = "disable";
                    _disableTopBtn.AddToClassList("topErrorBtn");
                    _topError.Add(_disableTopBtn);
                    
                    _closeTopBtn = new Button(HideTopError);
                    _closeTopBtn.text = "X";
                    _closeTopBtn.AddToClassList("topErrorBtn");
                    _topError.Add(_closeTopBtn);
                }
                _topErrorLbl.text = message;
                _overlay.Add(_topError);
            }

            void OnDisableClicked()
            {
                _disabled = true;
                HideTopError();
            }

            public void HideTopError()
            {
                _queuedError = null;
                if (_topError?.parent != null)
                {
                    _topError.RemoveFromHierarchy();
                }
            }

            public void DisableOnErrorBehaviour()
            {
                _disabled = true;
                HideTopError();
            }

            void OnTopErrorClicked(ClickEvent evt)
            {
                HideTopError();
                TryShowConsoleLogs();
            }

            void ShowToast(string message)
            {
                ConsoleToasts.Show(_overlay.Context, message, () =>
                {
                    _disabled = true;
                }, "Ignore all");
            }

            void TryShowConsoleLogs()
            {
                if (!_overlay.Showing)
                {
                    _overlay.ShowWithAccessChallenge();
                }
                if (!(_overlay.Window?.SetActivePanel<ConsoleLogsPanel.Module>() ?? false)) return;
                if (_overlay.Window.ActivePanelElement is ConsoleLogsPanel logsPanel)
                {
                    logsPanel.GotoLastLog();
                }
            }
        }
    }
#else
    public class ConsoleOverlay : VisualElement
    {
        public static bool HasInstance => false;
        public static ConsoleOverlay Instance => null;
        public ConsoleContext Context => null;
        public UIDocument UIDocument => null;
        public GameObject GameObject => null;
        public ConsoleScreenShortcuts Shortcuts => null;
        public void WaitForTriggersToShow() { }
        public bool IsAccessChallengeRequired() => false;
        public void ShowWithAccessChallenge(){ }
        public bool ShowingAnyChallenge() => false;
        public void ShowWithoutAccessChallenge() {}
        public void Hide() {}
        public void ShowShortcuts(bool inEditMode = false) {}
        public bool Showing => false;
        public float Scale { get; set; }
        public float SafeAreaScale { get; set; }
        public void HideErrorDisplayedAtTop() {}
        public void DisableOnErrorBehaviour() {}
        public bool ErrorBehaviourEnabled => false;
    }
#endif
}