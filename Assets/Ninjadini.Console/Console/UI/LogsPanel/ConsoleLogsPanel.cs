#if !NJCONSOLE_DISABLE
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using Ninjadini.Logger.Internal;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace Ninjadini.Console.UI
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public partial class ConsoleLogsPanel : VisualElement, IConsolePanelModule.IElement
    {
        public class Module : IConsolePanelModule
        {
            bool IConsoleModule.PersistInEditMode => true;
            
            public string Name => "Logs";
            
            public float SideBarOrder => 1;

            public bool PanelFeatureEnabled { get; private set; }

            void IConsoleModule.OnAdded(ConsoleModules modules)
            {
                PanelFeatureEnabled = Application.isEditor || modules.Settings.inPlayerLogsPanel;
            }

            public VisualElement CreateElement(ConsoleContext context)
            {
                return new ConsoleLogsPanel(context);
            }
        }

        ConsoleContext _context;
        ListView _listView;
        ScrollView _scrollView;
        int _drawnItemCount;
        LogsHistory _logs;
        LogsHistory.ListWrapper _logsList;
        bool _backlogLocked;
        StringBuilder _stringBuilder;
        Button _scrollToBottomBtn;
        ScrollView _detailsScrollView;
        VisualElement _detailsTopDragger;
        Label _detailsLbl;
        VisualElement _detailsLinks;
        VisualElement _detailsSideBtns;
        VisualElement _detailsStackTraces;
        Label _detailsTimestamp;
        Button _detailsExpandBtn;
        Button _detailsRichTextBtn;
        Button _detailsExpandStackTraceBtn;
        Filtering _filtering;
        IConsoleTimestampFormatter _timeFormatter;
        int _warnedTooManyLogs;
        
        ConsoleCommandLineElement _commandLineElement;// WARNING: Can be null if feature is disabled.
        /// WARNING: Can be null if feature is disabled.
        public ConsoleCommandLineElement CommandLineElement => _commandLineElement;
        public Filtering Filters => _filtering;

        int? _detailsExpanded;
        int _forcedBottomFrames;

        const int ItemHeight = 22;

        public ConsoleLogsPanel(ConsoleContext context)
        {
            _context = context;
            AddToClassList("panel-stretched");
            _stringBuilder = new StringBuilder(512);
            schedule.Execute(Update).Every(0);

            _logs = NjLogger.LogsHistory;
            if (_logs.MaxHistoryCount < LogsHistory.DefaultMaxHistoryCount)
            {
                _logs.SetMaxHistoryCount(LogsHistory.DefaultMaxHistoryCount);
            }
            _logsList = new LogsHistory.ListWrapper(_logs);
            _drawnItemCount = -1;
            
            var topMenu = new ScrollView();
            topMenu.mode = ScrollViewMode.Horizontal;
            topMenu.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            topMenu.AddToClassList("logs-menus-scrollview");
            topMenu.contentContainer.AddToClassList("logs-menus");

            _listView = new ListView(_logsList, ItemHeight, MakeItem, BindItem);
            _listView.AddToClassList("logs-list");
            _listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
#if UNITY_2022_2_OR_NEWER
            _listView.selectionChanged += OnSelectionChanged;
#else
            _listView.onSelectionChange += OnSelectionChanged;
#endif

            _filtering = new Filtering(this, _logsList);
            _filtering.AddMenuButtons(topMenu);

            
            topMenu.Add(new VisualElement()
            {
                style = { flexGrow = 1 }
            });
            var timeDropDown = new DropdownField(TimeStampFormats, TimeStampDisplay);
            timeDropDown.formatSelectedValueCallback = s => "Time";
            timeDropDown.RegisterValueChangedCallback(OnTimeStampDisplayChanged);
            timeDropDown.style.marginRight = 4;
            ConsoleUIUtils.FixDropdownFieldPopupSize(timeDropDown, context);
            timeDropDown.AddToClassList("logs-menus-item");
            topMenu.Add(timeDropDown);
            var clearBtn = new Button(OnClearLogClicked)
            {
                text = "Clear",
            };
            clearBtn.AddToClassList("logs-menus-item");
            clearBtn.AddToClassList("red-btn");
            topMenu.Add(clearBtn);
            
            Add(topMenu);
            Add(_filtering);
            Add(_listView);
            
            _scrollToBottomBtn = new Button(ScrollToBottom)
            {
                text = "↓",
                style =
                {
                    position = Position.Absolute,
                    right = 14,
                    bottom = 2,
                    width = 32,
                    display = DisplayStyle.None
                }
            };
            _listView.hierarchy.Add(_scrollToBottomBtn);

            _detailsTopDragger = new VisualElement();
            _detailsTopDragger.AddToClassList("logs-details-sizer");
            _detailsTopDragger.style.display = DisplayStyle.None;
            if (context.IsRuntimeOverlay)
            {
                _detailsTopDragger.style.cursor = new StyleCursor(StyleKeyword.None);
            }
            Add(_detailsTopDragger);
            
            _detailsTopDragger.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.currentTarget.CapturePointer(evt.pointerId);
            });

            _detailsTopDragger.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!_detailsTopDragger.HasPointerCapture(evt.pointerId)) return;
                var delta = evt.deltaPosition.y;
                var currentHeight = _detailsScrollView.resolvedStyle.height;
                _detailsScrollView.style.height = Mathf.Clamp(currentHeight - delta, 80f, Mathf.Min(280, currentHeight + _scrollView.resolvedStyle.height - 50));
            });

            _detailsTopDragger.RegisterCallback<PointerUpEvent>(evt =>
            {
                evt.currentTarget.ReleasePointer(evt.pointerId);
            });
            _detailsTopDragger.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2)
                {
                    OnExpandDetailsClicked();
                }
            });

            _detailsScrollView = new ScrollView()
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                style =
                {
                    display = DisplayStyle.None
                }
            };
            _detailsScrollView.AddToClassList("logs-details");
            Add(_detailsScrollView);

            _detailsLinks = new VisualElement();
            _detailsLinks.AddToClassList("logs-details-links");
            _detailsScrollView.Add(_detailsLinks);
            
            _detailsLbl = new Label();
            _detailsLbl.AddToClassList("logs-details-label");
            _detailsScrollView.Add(_detailsLbl);

            _detailsSideBtns = new VisualElement();
            _detailsSideBtns.AddToClassList("logs-details-sideBtns");
            _detailsScrollView.hierarchy.Add(_detailsSideBtns);
            
            _detailsTimestamp = new Label(_stringBuilder.ToString());
            _detailsTimestamp.AddToClassList("logs-details-frame");
            
            _detailsStackTraces = new VisualElement();
            _detailsScrollView.Add(_detailsStackTraces);
            
#if UNITY_2022_2_OR_NEWER
            _detailsTimestamp.selection.isSelectable = true;
            _detailsLbl.selection.isSelectable = true;
#else
            _detailsTimestamp.isSelectable = true;
            _detailsLbl.isSelectable = true;
#endif
            
            _detailsExpandBtn = new Button(OnExpandDetailsClicked)
            {
                text = "\u2197"
            };
            _detailsSideBtns.Add(_detailsExpandBtn);
            
            var detailsCopyBtn = new Button(OnCopyToClipboardClicked)
            {
                text = "Cp",
                tooltip = "Copy to clipboard"
            };
            _detailsSideBtns.Add(detailsCopyBtn);
            
            _detailsRichTextBtn = new Button(OnDetailsRichTextToggleClicked)
            {
                text = "Rt",
                tooltip = "Toggle rich text"
            };
            _detailsSideBtns.Add(_detailsRichTextBtn); 
            _detailsSideBtns.Add(new VisualElement()
            {
                style = { flexGrow = 1 }
            });
            var detailsCloseBtn = new Button(CloseDetails)
            {
                text = "X"
            };
            detailsCloseBtn.style.alignSelf = Align.FlexEnd;
            _detailsSideBtns.Add(detailsCloseBtn);

            if (Application.isEditor || context.Settings.inPlayerCommandLine)
            {
                _commandLineElement = new ConsoleCommandLineElement(_context);
                if (_commandLineElement.Runner != null)
                {
                    _commandLineElement.Runner.RanAction += CommandLineRanAction;
                }
                Add(_commandLineElement);
            }
            
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            UpdateTimeFormatter(false);
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (_backlogLocked)
            {
                _backlogLocked = false;
                _logs.StopBackLoggingLock();
            }
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (!_backlogLocked)
            {
                ScrollToBottom();
            }
        }

        void IConsolePanelModule.IElement.OnReselected()
        {
            Reset();
            _commandLineElement?.Show();
        }

        public void Refresh()
        {
            _listView.RefreshItems();
        }

        void CommandLineRanAction(string input, bool success, object result)
        {
            ScrollToBottom();
        }

        void Reset()
        {
            _filtering.Reset();
            CloseDetails();
            ScrollToBottom();
        }
        
        void OnClearLogClicked()
        {
            CloseDetails();
            _logs.Clear();
        }

        void OnExpandDetailsClicked()
        {
            StoreIfAtBottom();
            var expand = !_detailsExpanded.HasValue;
            if (expand)
            {
                _detailsExpanded = _listView.selectedIndex;
            }
            else
            {
                var index = _detailsExpanded.Value;
                _listView.schedule.Execute(() =>
                {
                    _listView.ScrollToItem(index);
                }).StartingIn(10);
                _detailsExpanded = null;
            }
            _detailsExpandBtn.text = expand ? "\u2199" : "\u2197";
            if (expand)
            {
                _detailsScrollView.style.height = new StyleLength(StyleKeyword.Null);
                _detailsScrollView.AddToClassList("logs-details-expanded");
            }
            else
            {
                _detailsScrollView.RemoveFromClassList("logs-details-expanded");
            }
        }

        void OnCopyToClipboardClicked()
        {
            var text = _detailsLbl.text;
            if (_detailsLbl.enableRichText)
            {
                text = LoggerUtils.StripRichText(text);
            }
            for (int i = 0, l = _detailsStackTraces.childCount; i < l; i++)
            {
                var child = _detailsStackTraces[i];
                if (child is TextElement lbl)
                {
                    text += "\n" + lbl.text;
                }
            }
            ConsoleUIUtils.CopyText(text, _context);
        }

        public void CloseDetails()
        {
            _listView.ClearSelection();
            if (_detailsExpanded.HasValue)
            {
                OnExpandDetailsClicked();
            }
            _detailsTopDragger.style.display = DisplayStyle.None;
            _detailsScrollView.style.display = DisplayStyle.None;
        }

        void OnDetailsRichTextToggleClicked()
        {
            _detailsLbl.enableRichText = !_detailsLbl.enableRichText;
            UpdateRichTextToggle();
        }

        void UpdateRichTextToggle()
        {
            _detailsRichTextBtn.style.unityFontStyleAndWeight = _detailsLbl.enableRichText ? FontStyle.Bold : FontStyle.Normal;
        }

        private void OnSelectionChanged(IEnumerable<object> obj)
        {
            var logLine = _listView.selectedItem as LogLine;
            if (logLine != null)
            {
                StoreIfAtBottom();
                _stringBuilder.Length = 0;
                var lineStr = logLine.GetLineString();
                _detailsRichTextBtn.style.display = LoggerUtils.IsPotentiallyRichText(lineStr) ? DisplayStyle.Flex : DisplayStyle.None;
                _stringBuilder.Append(lineStr);
                _detailsLbl.text = _stringBuilder.ToString();
                _detailsScrollView.scrollOffset = new Vector2(0f, 0f);
                _detailsStackTraces.Clear();
                PopulateDetailsPaths(lineStr);
                PopulateDetailsStack(logLine);
                AddTimestampToDetailsStackTrace(logLine);
                UpdateRichTextToggle();
                PopulateDetailLinks(logLine, true);
                var selectedIndex = _listView.selectedIndex;
                _listView.schedule.Execute(() =>
                {
                    _listView.ScrollToItem(selectedIndex);
                    _listView.SetSelectionWithoutNotify(new []{ selectedIndex });
                }).ExecuteLater(100);
            }
            else
            {
                _detailsLbl.text = "";
            }
            var show = logLine != null ? DisplayStyle.Flex : DisplayStyle.None;
            _detailsTopDragger.style.display = show;
            _detailsScrollView.style.display = show;
        }
        
        static readonly Regex ResPathRegex = new Regex(@"res\([\s\""]*(.+?)[\s\""]*\)", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        void PopulateDetailsPaths(string lineString)
        {
            if (!ConsoleContext.EditorBridge.HasEditorFeatures)
            {
                return;
            }
            var added = false;
            
            var matches = ResPathRegex.Matches(lineString);
            foreach (Match match in matches)
            {
                AddFileLink(new ConsoleContext.IEditorBridge.StackFrame()
                {
                    FilePath = ConsoleContext.IEditorBridge.ResPathPrefix + match.Groups[1].Value
                });
                added = true;
            }
            
            var pathsInLog = StringParser.ExtractPaths(lineString);
            foreach (var stackFrame in pathsInLog)
            {
                if (!ConsoleContext.EditorBridge.ValidFilePath(stackFrame.FilePath))
                {
                    continue;
                }
                AddFileLink(stackFrame);
                added = true;
            }
            if (added)
            {
                _detailsStackTraces.Add(new VisualElement()
                {
                    style =
                    {
                        height = 5
                    }
                });
            }
        }
        
        void AddFileLink(ConsoleContext.IEditorBridge.StackFrame stackFrame)
        {
            var btn = new Button(() =>
            {
                ConsoleContext.EditorBridge.GoToFile(stackFrame.FilePath, stackFrame.LineNumber);
            });
            btn.AddToClassList("logs-details-frame");
            btn.AddToClassList("logs-details-frame-btn");
            var str = Path.GetFileName(stackFrame.FilePath) + (stackFrame.LineNumber > 0 ? $":{stackFrame.LineNumber.ToString()}" : "");
            if (!string.IsNullOrEmpty(stackFrame.Name))
            {
                str = $"{stackFrame.Name}() @ " + str;
            }
            btn.text =  "\u25b6 \u25c9 " + str;
            _detailsStackTraces.Add(btn);
        }

        void PopulateDetailsStack(LogLine logLine)
        {
            var editorBridge = ConsoleContext.EditorBridge;
            var hasEditorFeatures = editorBridge.HasEditorFeatures;
            
            var stackFrames = StringParser.ExtractLinesFromStacktrace(logLine.StackTrace, _stringBuilder);
            var hasStack = false;
            const string stackTraceLineStart = "\u21aa ";
            for (var index = 0; index < stackFrames.Length; index++)
            {
                var stackFrame = stackFrames[index];
                var skipType = ConsoleContext.EditorBridge.ShouldSkipStackFrame(stackFrame);
                if (skipType == ConsoleContext.IEditorBridge.StackSkipType.Hide)
                {
                    continue;
                }
                if (!hasStack && skipType == ConsoleContext.IEditorBridge.StackSkipType.SkipEarly)
                {
                    continue;
                }
                hasStack = true;
                if (string.IsNullOrEmpty(stackFrame.FilePath) || !hasEditorFeatures || !editorBridge.ValidFilePath(stackFrame.FilePath))
                {
                    AddToDetailsStackTrace(stackTraceLineStart + stackFrame.Name);
                }
                else
                {
                    var btn = new Button(() =>
                    {
                        editorBridge.GoToFile(stackFrame.FilePath, stackFrame.LineNumber);
                    });
                    btn.AddToClassList("logs-details-frame");
                    btn.AddToClassList("logs-details-frame-btn");
                    btn.text = stackTraceLineStart + stackFrame.Name;
                    _detailsStackTraces.Add(btn);
                }
            }

            if (stackFrames.Length > 0)
            {
                if (_detailsExpandStackTraceBtn == null)
                {
                    _detailsExpandStackTraceBtn = new Button(ExpandDetailsStackTraceClicked);
                    _detailsExpandStackTraceBtn.style.width = 150;
                    _detailsExpandStackTraceBtn.style.marginTop = 2;
                    _detailsExpandStackTraceBtn.style.marginBottom = 10;
                    _detailsExpandStackTraceBtn.text = "Show full stacktrace";
                    _detailsScrollView.Add(_detailsExpandStackTraceBtn);
                }
                else
                {
                    _detailsExpandStackTraceBtn.style.display = DisplayStyle.Flex;
                }
            }
            else
            {
                if (logLine.StackTrace is string stackStr)
                {
                    AddToDetailsStackTrace("\u21a9\n" + stackStr);
                }
                if (_detailsExpandStackTraceBtn != null)
                {
                    _detailsExpandStackTraceBtn.style.display = DisplayStyle.None;
                }
            }
        }
        
        void AddToDetailsStackTrace(string str)
        {
            var lbl = new Label(str);
            lbl.AddToClassList("logs-details-frame");
#if UNITY_2022_2_OR_NEWER
            lbl.selection.isSelectable = true;
#else
            lbl.isSelectable = true;
#endif
            _detailsStackTraces.Add(lbl);
        }

        void AddTimestampToDetailsStackTrace(LogLine logLine)
        {
            _stringBuilder.Length = 0;
            _stringBuilder.Append("@ ");
            ((StrValue)logLine.Time).Fill(_stringBuilder);
            _detailsTimestamp.text = _stringBuilder.ToString();
            _detailsStackTraces.Add(_detailsTimestamp);
        }

        void ExpandDetailsStackTraceClicked()
        {
            var logLine = _listView.selectedItem as LogLine;
            if(logLine == null) return;
            _stringBuilder.Length = 0;
            var lineStr = logLine.GetLineString();
            _stringBuilder.Append(lineStr);
            
            if (logLine.StackTrace is string str)
            {
                _stringBuilder.Append("\n\u21a9\n");
                _stringBuilder.AppendLine(str);
            }
            else if(logLine.StackTrace is StackTrace st)
            {
                _stringBuilder.Append("\n\u21a9\n");
                _stringBuilder.AppendLine(st.ToString());
            }
            else
            {
                _stringBuilder.AppendLine("");
            }
            _stringBuilder.Append("@ ");
            ((StrValue)logLine.Time).Fill(_stringBuilder);
            _detailsLbl.text = _stringBuilder.ToString();
            _detailsExpandStackTraceBtn.style.display = DisplayStyle.None;
            _detailsStackTraces.Clear();
        }

        void PopulateDetailLinks(LogLine logLine, bool pingAnyItem)
        {
            _detailsLinks.Clear();
            if (!Application.isEditor && !_context.Settings.inPlayerObjectInspector)
            {
                _detailsLinks.style.display = DisplayStyle.None;
                return;
            }

            Object unityObjToPing = null;
            foreach (var strValue in logLine.Values)
            {
                var (obj, objType) = strValue.GetObjectAndType();
                if (obj != null || objType != null)
                {
                    AddToDetailsLink(obj, obj?.GetType().Name ?? objType.Name);
                    if (obj is Object unityObj)
                    {
                        unityObjToPing = unityObj;
                    }
                }
            }
            var contextObj = logLine.Context?.Target;
            if (contextObj != null)
            {
                AddToDetailsLink(contextObj, contextObj.ToString());
                if (contextObj is Object unityObj)
                {
                    unityObjToPing = unityObj;
                }
            }
            _detailsLinks.style.display = _detailsLinks.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            if (pingAnyItem && unityObjToPing)
            {
                ConsoleContext.EditorBridge?.PingObject(unityObjToPing);
            }
        }

        void AddToDetailsLink(object obj, string objName)
        {
            var btn = new Button(() =>
            {
                if(obj is UnityEngine.Object o) // Unity safe / destroy check
                {
                    obj = o ? obj : null;
                }
                if (obj != null)
                {
                    ConsoleInspector.Show(parent, obj);
                    if (obj is UnityEngine.Object unityObj)
                    {
                        ConsoleContext.EditorBridge?.PingObject(unityObj);
                    }
                }
                else
                {
                    ConsoleToasts.Show(_context, $"<u>{(objName ?? "Object")}</u> no longer exists. Most likely destroyed or garbage collected.");
                }
            })
            {
                text = LoggerUtils.GetSingleShortenedLine(objName, 24)
            };
            btn.AddToClassList("logs-details-link-btn");
            _detailsLinks.Add(btn);
        }

        public void GotoLastLog()
        {
            Reset();
            ScrollToBottom();
            schedule.Execute(ScrollToBottom).ExecuteLater(20);
        }

        public void ScrollToBottom()
        {
            if (_scrollView != null && _scrollView.verticalScroller.highValue > 0f)
            {
                _scrollView.verticalScroller.value = _scrollView.verticalScroller.highValue;
                _forcedBottomFrames = System.Math.Max(_forcedBottomFrames, 2);
            }
        }

        void Update()
        {
            var atBottom = AtBottom();
            if (atBottom)
            {
                if (_backlogLocked)
                {
                    _backlogLocked = false;
                    _logs.StopBackLoggingLock();
                }
            }
            else
            {
                if(!_backlogLocked)
                {
                    _backlogLocked = true;
                    _logs.StartBackLoggingLock();
                }
                else
                {
                    TryWarnIfTooManyBacklog();
                }
            }
            
            if (_drawnItemCount != _logsList.RealCount || _forcedBottomFrames > 0)
            {
                if (_forcedBottomFrames > 0)
                {
                    _forcedBottomFrames--;
                }
                var firstDraw = _drawnItemCount == 0;
                _drawnItemCount = _logsList.RealCount;
                
                _listView.RefreshItems();

                if (_scrollView == null)
                {
                    _scrollView = _listView.Q<ScrollView>();
                }
                if (_scrollView != null)
                {
                    if (atBottom || firstDraw)
                    {
                        _scrollView.verticalScroller.value = _scrollView.verticalScroller.highValue;
                    }
                }
            }
            _scrollToBottomBtn.style.display = atBottom ? DisplayStyle.None : DisplayStyle.Flex;

            if (_detailsScrollView.style.display == DisplayStyle.Flex)
            {
                var hasScroll = _detailsScrollView.verticalScroller.highValue > _detailsScrollView.verticalScroller.lowValue;

                _detailsExpandBtn.style.display = hasScroll || _detailsExpanded.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
                _detailsSideBtns.style.right = hasScroll ? 12f : 0f;
            }
        }

        void TryWarnIfTooManyBacklog()
        {
            var increments = _logs.MaxHistoryCount * 2;
            if (_warnedTooManyLogs >= 0 && _logs.NumBackLoggedItems > _warnedTooManyLogs + increments)
            {
                _warnedTooManyLogs = _logs.NumBackLoggedItems;
                ConsoleToasts.Show(_context, 
                    ConsoleUIStrings.TooManyBacklog.Replace("{number}", _logs.NumBackLoggedItems.ToString("n0")),
                    () =>
                    {
                        _warnedTooManyLogs = -1;
                    },
                    ConsoleUIStrings.TooManyBacklogIgnore);
            }
        }

        public bool AtBottom()
        {
            if(_forcedBottomFrames > 0)
            {
                return true;
            }
            return _scrollView == null || _scrollView.verticalScroller.value >= (_scrollView.verticalScroller.highValue - 10);
        }
        
        bool StoreIfAtBottom()
        {
            if (!AtBottom()) return false;
            _forcedBottomFrames = 2;
            return true;
        }
        
        private VisualElement MakeItem()
        {
            var element = new VisualElementLogLine();
            element.RegisterCallback<ClickEvent>(OnListViewClick);
            element.RegisterCallback<MouseDownEvent>(OnListViewMouseDown);
            return element;
        }

        private void BindItem(VisualElement visualElement, int index)
        {
            var log = _logsList.GetLog(index);
            var lineElement = (VisualElementLogLine)visualElement;
            lineElement.Set(log, _timeFormatter);
            UpdatePinnedState(lineElement);
        }

        void OnListViewMouseDown(MouseDownEvent e)
        {
            if (e.button != (int)MouseButton.RightMouse 
                || e.currentTarget is not VisualElementLogLine lineElement
                )
            {
                return;
            }
            _filtering.SetPinned(lineElement.LogLine, !_filtering.IsPinned(lineElement.LogLine));
            UpdatePinnedState(lineElement);
        }

        void UpdatePinnedState(VisualElementLogLine lineElement)
        {
            var pinned =_filtering.IsPinned(lineElement.LogLine);
            if (pinned)
            {
                lineElement.AddToClassList("logs-line-pinned");
            }
            else
            {
                lineElement.RemoveFromClassList("logs-line-pinned");
            }
        }

        void OnListViewClick(ClickEvent e)
        {
            if (e.currentTarget is not VisualElementLogLine lineElement 
                || lineElement.LogLine != _listView.selectedItem
                || e.button == (int)MouseButton.RightMouse 
                || e.clickCount < 2 
                || !ConsoleContext.EditorBridge.HasEditorFeatures)
            {
                return;
            }
            var stackTraceObj = lineElement.LogLine.StackTrace;
            if (stackTraceObj is StackTrace stackTrace && stackTrace.FrameCount == 0)
            {
                // Likely a compile error line from unity
                stackTraceObj = lineElement.LogLine.GetLineString();
            }
            var hasLineToGo = ConsoleContext.EditorBridge.GoToFile(stackTraceObj, 0);
            if (!hasLineToGo)
            {
                ConsoleContext.EditorBridge.GoToFile(lineElement.LogLine.GetLineString(), 0);
            }
        }

        void OnTimeStampDisplayChanged(ChangeEvent<string> evt)
        {
            TimeStampDisplay = evt.newValue;
            _listView.RefreshItems();
        }

        string _timeStampDisplay;
        string TimeStampDisplay
        {
            get
            {
                if (_timeStampDisplay != null) return _timeStampDisplay;
                var index = _context.Storage.GetInt(StandardStorageKeys.LogsTimestampDisplay);
                if (index >= 0 && index < TimeStampFormats.Count)
                {
                    _timeStampDisplay = TimeStampFormats[index];
                }
                else
                {
                    _timeStampDisplay = TimeStampFormats[0];
                }
                return _timeStampDisplay;
            }
            set
            {
                var index = TimeStampFormats.IndexOf(value);
                if (index < 0)
                {
                    return;
                }
                _timeStampDisplay = value;
                _context.Storage.SetInt(StandardStorageKeys.LogsTimestampDisplay, index);
                UpdateTimeFormatter(true);
            }
        }

        void UpdateTimeFormatter(bool canAnnounceAboutNoCustomModule)
        {
            _timeFormatter = null;
            
            var index = TimeStampFormats.IndexOf(TimeStampDisplay);
            if (index < 0 || index >= TimeStampFormats.Count) return;

            _timeFormatter = DefaultFormats[index];
            if (index != 0 && _timeFormatter == null)
            {
                // this is the custom module...
                _timeFormatter = _context.Modules.GetModule<IConsoleTimestampFormatter>(true);
                if (canAnnounceAboutNoCustomModule)
                {
                    if (_timeFormatter == null)
                    {
                        ConsoleToasts.Show(_context, $"None of the modules implement `{nameof(IConsoleTimestampFormatter)}`. See documentation to find out how to do it.");
                    }
                    else
                    {
                        ConsoleToasts.Show(_context, $"{_timeFormatter.GetType().Name} was found as custom time formatter module");
                    }
                }
            }
        }

        class VisualElementLogLine : VisualElement
        {
            readonly Label _timeStamp;
            readonly Label _label;
            public LogLine LogLine;
            string _prevClass;
            
            public VisualElementLogLine()
            {
                AddToClassList("logs-line");
                
                _timeStamp = new Label();
                _timeStamp.AddToClassList("logs-label");
                Add(_timeStamp);
                
                _label = new Label();
                _label.AddToClassList("logs-label");
                Add(_label);
            }

            public void Set(LogLine log, IConsoleTimestampFormatter timeFormatter)
            {
                LogLine = log;
                if (log == null)
                {
                    if (!string.IsNullOrEmpty(_prevClass))
                    {
                        _label.RemoveFromClassList(_prevClass);
                        _prevClass = null;
                    }
                    _timeStamp.style.display = DisplayStyle.None;
                    _label.text = "...";
                    return;
                }
                UpdateTimeStamp(timeFormatter);
                _label.text = log.GetLineString();
                var targetClass = log.Level switch
                {
                    NjLogger.Options.Error => "logs-error",
                    NjLogger.Options.Warn => "logs-warn",
                    NjLogger.Options.Debug => "logs-debug",
                    _ => "logs-info"
                };
                if (!string.IsNullOrEmpty(_prevClass))
                {
                    if (_prevClass == targetClass)
                    {
                        return;
                    }
                    _label.RemoveFromClassList(_prevClass);
                }
                _prevClass = targetClass;
                _label.AddToClassList(targetClass);
            }

            void UpdateTimeStamp(IConsoleTimestampFormatter formatter)
            {
                if (formatter == null)
                {
                    _timeStamp.style.display = DisplayStyle.None;
                    return;
                }
                _timeStamp.style.display = DisplayStyle.Flex;

                var time = LogLine.Time;
                
                var sb = LoggerUtils.TempStringBuilder.Clear();
                formatter.AppendFormatted(LogLine, sb);
                _timeStamp.text =  sb.ToString();
                sb.Clear();
            }
        }
    }
}
#endif