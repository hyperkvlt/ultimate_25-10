#if !NJCONSOLE_DISABLE
using System.Collections.Generic;
using System.ComponentModel;
using Ninjadini.Console.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ConsoleCommandLineElement : VisualElement
    {
        readonly ConsoleContext _context;
        readonly ConsoleCommandlineRunner _commandlineRunner;
        readonly TextField _textField;
        readonly VisualElement _autoCompletes;
        readonly Button _inputBtn;
        readonly Button _cancelBtn;
        int _historyIndex = -1;
        string _preHistory;
        bool _refreshingFocus;

        const string NormalInputSymbol = "\u2328";
        const string MobilePromptInputSymbol = "\u2594";
        
        public ConsoleCommandlineRunner Runner => _commandlineRunner;
        
        public ConsoleCommandLineElement(ConsoleContext context)
        {
            _context = context;
            AddToClassList("cmdline");
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            
            _commandlineRunner = new ConsoleCommandlineRunner(_context);
            _commandlineRunner.RanAction += CommandlineRunnerOnRanAction;
            
            _textField = new TextField(ConsoleCommandlineRunner.NormalPrompt);
            _textField.selectAllOnFocus = false;
            _textField.selectAllOnMouseUp = false;
            _textField.style.flexGrow = 1;
            _textField.style.flexShrink = 1;
            _textField.labelElement.style.minWidth = 0;
            _textField.multiline = false;
            _textField.AddToClassList("monoFont");
            Add(_textField); 
            
            _inputBtn = new Button(ToggleInputTypeBtnClicked);
            _inputBtn.AddToClassList("monoFont");
            if (Application.isMobilePlatform)
            {
                Add(_inputBtn);
                SetInputType(MobilePromptInputSymbol);
            }
            else
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    Add(_inputBtn);
                }
                SetInputType(NormalInputSymbol);
            }
            
            _cancelBtn = new Button(CancelLockedModule)
            {
                text = "cancel",
                style =
                {
                    height = 16
                }
            };
            _cancelBtn.AddToClassList("red-btn");
            _cancelBtn.style.display = DisplayStyle.None;
            Add(_cancelBtn);
            
            Add(new Button(OnCloseClicked)
            {
                text = "X",
                style =
                {
                    height = 16
                }
            });

            _autoCompletes = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 0,
                    right = 0,
                    bottom = 20,
                    flexDirection = FlexDirection.ColumnReverse,
                    display = DisplayStyle.None
                }
            };
            
            Add(_autoCompletes);
            if(_context.Storage.GetInt(StandardStorageKeys.CmdLineShown) == 0)
            {
                style.display = DisplayStyle.None;
            }
        }

        void RegisterAsTextFieldInput()
        {
            _textField.isReadOnly = false;
            _textField.focusable = true;
            _textField.pickingMode = PickingMode.Ignore;
            _textField.RegisterValueChangedCallback(OnTextFieldValueChanged);
            _textField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown, TrickleDown.TrickleDown);
            _textField.RegisterCallback<FocusInEvent>(OnTextFieldFocusIn);
            _textField.RegisterCallback<FocusOutEvent>(OnTextFieldFocusOut);
        }

        void UnRegisterBothInput()
        {
            _textField.UnregisterValueChangedCallback(OnTextFieldValueChanged);
            _textField.UnregisterCallback<KeyDownEvent>(OnTextFieldKeyDown, TrickleDown.TrickleDown);
            _textField.UnregisterCallback<FocusInEvent>(OnTextFieldFocusIn);
            _textField.UnregisterCallback<FocusOutEvent>(OnTextFieldFocusOut);
            _textField.UnregisterCallback<ClickEvent>(OnMobilePromptClicked);
        }

#region Mobile text prompt input handling
        void RegisterAsMobilePromptInput()
        {
            _textField.isReadOnly = true;
            _textField.focusable = false;
            _textField.pickingMode = PickingMode.Position;
                    
            _textField.RegisterCallback<ClickEvent>(OnMobilePromptClicked);
        }

        void OnMobilePromptClicked(ClickEvent evt)
        {
            var data = new ConsoleTextPrompt.Data()
            {
                ResultCallback = (cmd) =>
                {
                    if (!string.IsNullOrEmpty(cmd))
                    {
                    Run(cmd);
                    }
                    return true;
                },
                AutoCompleteResultsCallback = MobilePromptAutoCompleteCallback,
                AutoCompleteSelected = MobilePromptAutoCompleteSelected,
                CanSubmitWithoutAutoComplete = true
            };
            var prompt = ConsoleTextPrompt.ShowInConsoleRoot(this, data);
            ConsoleUIUtils.AutoRemoveAWhenBIsRemoved(prompt, this);
        }

        static bool MobilePromptAutoCompleteSelected(string input) => false;

        void MobilePromptAutoCompleteCallback(string input, List<string> result)
        {
            var ctx = new IConsoleCommandlineModule.HintContext(input);
            _commandlineRunner.FillAutoCompletableHints(ctx);
            for(int i = 0, l = Mathf.Min(100, ctx.Result.Count); i < l; i++)
            {
                var v = ctx.Result[i];
                result.Add(input + v.cmd);
            }
        }

        void ToggleInputTypeBtnClicked()
        {
            UnRegisterBothInput();
            SetInputType(_inputBtn.text == MobilePromptInputSymbol ? MobilePromptInputSymbol :  NormalInputSymbol);
        }
        
        void SetInputType(string inputSymbol)
        {
            if (inputSymbol == MobilePromptInputSymbol)
            {
                _inputBtn.text = NormalInputSymbol;
                _inputBtn.tooltip = "Switch to physical keyboard input";
                _textField.SetValueWithoutNotify("");
                RegisterAsMobilePromptInput();
            }
            else
            {
                _inputBtn.text = MobilePromptInputSymbol;
                _inputBtn.tooltip = "Switch to virtual keyboard input";
                RegisterAsTextFieldInput();
            }
        }
#endregion

        void CommandlineRunnerOnRanAction(string input, bool success, object result)
        {
            RefreshLockedMode();
        }

        void RefreshLockedMode()
        {
            if (_commandlineRunner.LockedModule != null)
            {
                _cancelBtn.style.display = DisplayStyle.Flex;
                _textField.label = ConsoleCommandlineRunner.LockedPrompt;
            }
            else
            {
                _cancelBtn.style.display = DisplayStyle.None;
                _textField.label = ConsoleCommandlineRunner.NormalPrompt;
            }
        }

        public void SetLockedModule(IConsoleCommandlineModule module)
        {
            _commandlineRunner.LockedModule = module;
            RefreshLockedMode();
        }

        void OnCloseClicked()
        {
            Hide();
        }

        public void Hide()
        {
            if (style.display != DisplayStyle.None)
            {
                style.display = DisplayStyle.None;
                _context.Storage.SetInt(StandardStorageKeys.CmdLineShown, 0);
            }
        }

        public void Show()
        {
            if (style.display != DisplayStyle.Flex)
            {
                _context.Storage.SetInt(StandardStorageKeys.CmdLineShown, 1);
                style.display = DisplayStyle.Flex;
                if (_context.Window?.ActivePanelElement is ConsoleLogsPanel logsPanel && logsPanel.AtBottom())
                {
                    logsPanel.ScrollToBottom();
                }
            }
            _textField.Focus();
        }
        
        public bool Showing => style.display == DisplayStyle.Flex;

        public void CancelLockedModule()
        {
            Runner.CancelLockedModule();
            RefreshLockedMode();
        }

        public void SetInputText(string value)
        {
            _textField.value = value;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            schedule.Execute(RegisterKeyDownAnywhere).ExecuteLater(20);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterKeyDownAnywhere();
            Runner?.ClearLastResult();
        }

        void RegisterKeyDownAnywhere()
        {
            UnregisterKeyDownAnywhere();
            panel?.visualTree?.RegisterCallback<KeyDownEvent>(OnKeyDownAnywhere, TrickleDown.TrickleDown);
        }
        
        void UnregisterKeyDownAnywhere()
        {
            panel?.visualTree?.UnregisterCallback<KeyDownEvent>(OnKeyDownAnywhere, TrickleDown.TrickleDown);
        }

        void OnKeyDownAnywhere(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.None or KeyCode.UpArrow or KeyCode.DownArrow or KeyCode.Escape)
            {
                return;
            }
            if (evt.commandKey || evt.ctrlKey || evt.altKey)
            {
                return;
            }
            if (evt.target is VisualElement element)
            {
                while (element != null)
                {
                    var type = element.GetType();
                    while (type != null)
                    {
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TextInputBaseField<>))
                        {
                            return;
                        }
                        type = type.BaseType;
                    }
                    element = element.parent;
                }
            }
            Show();
            _textField.Focus();
        }

        void OnTextFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                _textField.value = "";
                Hide();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                if (evt.shiftKey && HasAutoCompleteHints()) TraverseAutoCompleteHints(-1);
                else ReviveHistory(-1);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.DownArrow)
            {
                if (evt.shiftKey && HasAutoCompleteHints()) TraverseAutoCompleteHints(1);
                else ReviveHistory(1);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Tab)
            {
                evt.StopPropagation();
                var selected = GetSelectedHintButton();
                if (selected != null)
                {
                    var str = (string)selected.userData;
                    SetInputAndSelectEnd(_textField.value + str);
                }
                else
                {
                    AdoptNextHintStep();
                }
            }
            else if (evt.keyCode == KeyCode.Return)
            {
                evt.StopPropagation();
#if UNITY_2023_2_OR_NEWER
                focusController?.IgnoreEvent(evt);
#else
                evt.PreventDefault();
#endif
                var selected = GetSelectedHintButton();
                if (selected != null)
                {
                    var str = (string)selected.userData;
                    SetInputAndSelectEnd(_textField.value + str);
                }
                else if (!string.IsNullOrEmpty(_textField.text))
                {
                    RunInput();
                }
            }
        }

        void RunInput()
        {
            var cmd = _textField.text;
            History.Add(cmd);
            Run(_textField.text);
            _historyIndex = -1;
            SetInputAndSelectEnd("");
        }

        void Run(string cmd)
        {
            var prompt = _commandlineRunner.LockedModule != null ? ConsoleCommandlineRunner.LockedPrompt : ConsoleCommandlineRunner.NormalPrompt;
            IConsoleCommandlineModule.Channel.Info($"<color=#66ccff>{prompt} <noparse>{cmd}</noparse>");
            _commandlineRunner.TryRun(cmd);
        }

        void OnTextFieldValueChanged(ChangeEvent<string> evt)
        {
            RefreshHints();
        }

        void RefreshHints()
        {
            _autoCompletes.Clear();
            var txt = _textField.value;
            if (txt.Length == 0)
            {
                _autoCompletes.style.display = DisplayStyle.None;
                return;
            }
            var ctx = new IConsoleCommandlineModule.HintContext(txt);
            _commandlineRunner.FillAutoCompletableHints(ctx);
            for(int i = 0, l = Mathf.Min(100, ctx.Result.Count); i < l; i++)
            {
                var v = ctx.Result[i];
                AddHint(v.cmd, v.tooltip, v.startOfCmdAdjustment);
            }
            _autoCompletes.style.display = ctx.Result.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        Button AddHint(string cmd, string tooltipTxt, int startOfCmdAdjustment)
        {
            var input = _textField.value;
            if (startOfCmdAdjustment < 0)
            {
                input = input.Substring(0, Mathf.Max(0, input.Length + startOfCmdAdjustment));
            }
            var btn = new Button(() =>
            {
                SetInputAndSelectEnd(input + cmd);
            })
            {
                text = $"<alpha=#33><noparse>{input}</noparse><alpha=#FF><noparse>{cmd}</noparse>{tooltipTxt}",
                userData = cmd
            };
            btn.AddToClassList("cmdline-hintbtn");
            _autoCompletes.Add(btn);
            return btn;
        }

        void OnTextFieldFocusIn(FocusInEvent evt)
        {
            if(_refreshingFocus) return;
            RefreshHints();
            SelectEnd();
        }

        void OnTextFieldFocusOut(FocusOutEvent evt)
        {
            if(_refreshingFocus) return;
            _textField.schedule.Execute(() =>
            {
                var focused = _textField.focusController.focusedElement;
                if (focused is VisualElement element && Contains(element))
                {
                    return;
                }
                _autoCompletes.Clear();
                _autoCompletes.style.display = DisplayStyle.None;
            }).ExecuteLater(50);
        }

        void TraverseAutoCompleteHints(int direction)
        {
            VisualElement selected = null;
            var isReversed = _autoCompletes.style.flexDirection == FlexDirection.ColumnReverse;
            if (isReversed) direction *= -1;
            var index = -1;
            var list = new List<VisualElement>();
            var cCount = _autoCompletes.childCount;
            for (var i = 0; i < cCount; i++)
            {
                var btn = _autoCompletes[i];
                if (GetAutoCompleteBtnTxt(btn) == null) continue;
                    if (selected == null && btn.ClassListContains(SelectedHintBtnClass))
                    {
                        index = i;
                        selected = btn;
                    }
                    list.Add(btn);
                }
            if (list.Count == 0)
            {
                return;
            }
            index += direction;
            if (index < 0) index = list.Count - 1;
            else if (index >= list.Count) index = 0;
            if (selected != null)
            {
                selected.RemoveFromClassList(SelectedHintBtnClass);
            }
            list[index].AddToClassList(SelectedHintBtnClass);
        }

        Button GetSelectedHintButton()
        {
            var cCount = _autoCompletes.childCount;
            for (var i = 0; i < cCount; i++)
            {
                if (_autoCompletes[i] is Button btn && btn.userData is string && btn.ClassListContains(SelectedHintBtnClass))
                {
                    return btn;
                }
            }
            return null;
        }

        const string SelectedHintBtnClass = "cmdline-hintbtn-selected";

        string GetAutoCompleteBtnTxt(VisualElement btn)
        {
            return btn is Button { userData: string str } ? str : null;
        }
        
        bool HasAutoCompleteHints() => _autoCompletes.childCount > 0 && _autoCompletes.style.display == DisplayStyle.Flex;
        
        void AdoptNextHintStep()
        {
            var selected = GetSelectedHintButton();
            if (selected == null)
            {
                for (int i = 0, l = _autoCompletes.childCount; i < l; i++)
                {
                    if (!string.IsNullOrEmpty(GetAutoCompleteBtnTxt(_autoCompletes[i])))
                    {
                        selected = (Button)_autoCompletes[i];
                        break;
                    }
                }
            }
            if (selected == null)
            {
                return;
            }
            var str = (string)selected.userData;
            var nextIndex = FindFirstAutoCompleteDiffIndex(str);
            if (nextIndex > 0)
            {
                str = str.Substring(0, nextIndex);
            }
            SetInputAndSelectEnd(_textField.value + str);
        }
        
        int FindFirstAutoCompleteDiffIndex(string baseStr)
        {
            var baseLen = baseStr.Length;
            var result = -1;
            var cCount = _autoCompletes.childCount;
            for (var aIndex = 0; aIndex < cCount; aIndex++)
            {
                var a = GetAutoCompleteBtnTxt(_autoCompletes[aIndex]);
                if (string.IsNullOrEmpty(a))
                {
                    continue;
                }
                if (a == baseStr)
                {
                    continue;
                }
                a = a.TrimEnd();
                for (int i = 0, l = Mathf.Min(a.Length, baseLen); i < l; i++)
                {
                    if (a[i] != baseStr[i])
                    {
                        if (result < 0 || result > i)
                        {
                            result = i;
                        }
                        break;
                    }
                }
            }
            return result;
        }

        ConsoleUIUtils.PlayerPrefHistoryStrings _history;
        public ConsoleUIUtils.PlayerPrefHistoryStrings History
        {
            get
            {
                if (_history == null)
                {
                    _history = new ConsoleUIUtils.PlayerPrefHistoryStrings($"{StandardStorageKeys.OptionsHistory}_{GetType().Name}", 16, "\n");
                }
                return _history;
            }
        }

        void ReviveHistory(int direction)
        {
            var items = History.Items;
            if (items.Count == 0)
            {
                return;
            }
            if (_historyIndex == -1)
            {
                if (direction > 0)
                {
                    return;
                }
                _preHistory = _textField.value;
                _historyIndex = items.Count - 1;
            }
            else
            {
                _historyIndex += direction;
            }
            if (_historyIndex < 0)
            {
                _historyIndex = 0;
            }
            if (_historyIndex >= items.Count)
            {
                _historyIndex = -1;
                _textField.value = _preHistory;
                SetInputAndSelectEnd(_preHistory);
            }
            else
            {
                SetInputAndSelectEnd(items[_historyIndex]);
            }
        }

        public void SetInputAndSelectEnd(string txt)
        {
            _textField.value = txt;
            SelectEnd();
        }

        void SelectEnd()
        {
            _textField.schedule.Execute(() =>
            {
                if (_textField.panel == null) return;
                _refreshingFocus = true;
                try
                {
#if !UNITY_2023_2_OR_NEWER
                    if (_textField.focusController.focusedElement== _textField)
                    {
                        _textField.Blur();  // issue in older unity
                    }
#endif
                    _textField.Focus();
                }
                finally
                {
                    _refreshingFocus = false;
                }
                _textField.SelectRange(_textField.value.Length, _textField.value.Length);
            });
        }
    }
}
#endif