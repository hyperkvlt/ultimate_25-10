#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using Ninjadini.Console.Internal;
using Ninjadini.Console.UI;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Ninjadini.Console
{
    public class ConsoleTextPrompt
    {
        public partial struct Data
        {
            public string Title;
            public string InitialText;
            public bool IsPassword;
            /// Please return true when success.
            /// WARNING: The string is null if user clicked on close button.
            public Func<string, bool> ResultCallback;
            public Func<string, string> ValueChangeCallback;
            public Action<string, List<string>> AutoCompleteResultsCallback;
            public bool CanSubmitWithoutAutoComplete; // if true, you can submit without any match to auto complete
            public Func<string, bool> AutoCompleteSelected; // return true to submit immediately (default/null is auto submit)
            public TouchScreenKeyboardType KeyboardType;
            public bool Multiline;
        }
        
        /// This does not work in editor windows
        public static UIDocument Show(Data data)
        {
            var panelSettings = Resources.Load<PanelSettings>(ConsoleSettings.PanelSettingsResName);
            var uiDoc = new GameObject("NjConsolePrompt").AddComponent<UIDocument>();
            uiDoc.panelSettings = panelSettings;
            uiDoc.sortingOrder = 10000;

            var resultCallback = data.ResultCallback;
            data.ResultCallback = (txt) =>
            {
                if (resultCallback?.Invoke(txt) ?? false)
                {
                    if (uiDoc)
                    {
                        Object.Destroy(uiDoc.gameObject);
                    }
                    return true;
                }
                return false;
            };
            ConsoleUIUtils.AutoApplySafeRegion(uiDoc.rootVisualElement);
            Show(uiDoc.rootVisualElement, data);
            
            return uiDoc;
        }

        /// This works in editor window as well as scene playmode
        public static VisualElement ShowInConsoleRoot(VisualElement visualSrc, Data data)
        {
            var parent = ConsoleUIUtils.FindConsoleOrRoot(visualSrc);
            return Show(parent, data);
        }

        /// This works in editor window as well as scene playmode
        public static VisualElement Show(VisualElement parent, Data data)
        {
            var container = new VisualElement();
            parent.Add(container);
            container.styleSheets.Add(Resources.Load<StyleSheet>(ConsoleSettings.StyleSheetResName));
            
            container.AddToClassList("fullscreen");
            container.AddToClassList("textPrompt-container");

            if (Application.isMobilePlatform 
                && (Screen.orientation == ScreenOrientation.LandscapeLeft 
                    || Screen.orientation == ScreenOrientation.LandscapeRight))
            {
                container.style.paddingTop = 0;
                container.style.paddingBottom = 200;
            }

            var underlay = new VisualElement();
            underlay.AddToClassList("underlay");
            underlay.pickingMode = PickingMode.Position;
            container.Add(underlay);
            
            TouchScreenKeyboard keyboard = null;
            
            if (!string.IsNullOrEmpty(data.Title))
            {
                var title = new Label(data.Title);
                title.AddToClassList("textPrompt-title");
                container.Add(title);
            }

            Button submitBtn = null;
            Button autoCompleteSelectedBtn = null;
            var autoCompleteHolder = data.AutoCompleteResultsCallback == null ? null : new VisualElement();
            var autoCompleteList = data.AutoCompleteResultsCallback == null ? null : new List<string>();

            var scrollView = new ScrollView();
            scrollView.mode = ScrollViewMode.Vertical;
            scrollView.contentContainer.style.paddingBottom = 100;
            container.Add(scrollView);
            
            var textField = new TextField();
            textField.AddToClassList("textPrompt-textfield");
            
            textField.isPasswordField = data.IsPassword;
            textField.focusable = true;
            textField.multiline = data.Multiline;
            textField.selectAllOnFocus = false;
            textField.selectAllOnMouseUp = false;
#if UNITY_2022_3_OR_NEWER
            textField.keyboardType = data.KeyboardType;
#endif
            textField.value = data.InitialText ?? "";
            
            if (data.ValueChangeCallback != null)
            {
                textField.RegisterValueChangedCallback(change =>
                {
                    var newValue = change.newValue;
                    var str = data.ValueChangeCallback.Invoke(newValue);
                    if (str != null && str != textField.text)
                    {
                        textField.schedule.Execute(() =>
                        {
                            if (newValue == textField.text)
                            {
                                textField.value = str;
                            }
                        }).ExecuteLater(1);
                    }
                    if (autoCompleteHolder != null)
                    {
                        UpdateAutoCompleteList(str);
                    }
                });
            }
            else if (autoCompleteHolder != null)
            {
                textField.RegisterValueChangedCallback(change =>
                {
                    textField.schedule.Execute(() =>
                    {
                        UpdateAutoCompleteList(textField.text);
                    }).ExecuteLater(1);
                });
            }
            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (!data.Multiline && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
                {
                    TakeAutoCompleteSelectionOrSubmit(textField.text);
                    evt.StopPropagation();
                    evt.StopImmediatePropagation();
                }
                else if (evt.keyCode == KeyCode.DownArrow)
                {
                    ShiftAutoCompleteSelection(1);
                }
                else if (evt.keyCode == KeyCode.UpArrow)
                {
                    ShiftAutoCompleteSelection(-1);
                }
                else if(evt.keyCode == KeyCode.Escape)
                {
                    SubmitResult(null);
                    evt.StopPropagation();
                    evt.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);

            var focusOutIgnoreFrame = 0;
            var btnClickedFrame = 0;

            textField.RegisterCallback<FocusEvent>(OnFocusIn);
            textField.RegisterCallback<FocusOutEvent>(OnFocusOut);

            scrollView.Add(textField);
            textField.schedule.Execute(() =>
            {
                if (textField.panel != null)
                {
                    textField.SelectRange(0, textField.value.Length);
                    textField.Focus();
                    UpdateAutoCompleteList(textField.value);
                }
            });
            
            if(autoCompleteHolder != null) scrollView.Add(autoCompleteHolder);

            if (data.Multiline)
            {
                textField.AddToClassList("textPrompt-textfield-small");
                
                submitBtn = new Button(() => SubmitResult(textField.text))
                {
                    text = "Submit"
                };
                submitBtn.AddToClassList("textPrompt-submit-btn");
                container.Add(submitBtn);
            }
            else if (Application.isMobilePlatform)
            {
                textField.AddToClassList("textPrompt-textfield-small");
            }
            var closeBtn = new Button(() => SubmitResult(null))
            {
                text = "X"
            };
            if (Application.platform == RuntimePlatform.Android)
            {
                closeBtn.RegisterCallback<MouseDownEvent>((evt) =>
                {
                    if (evt.currentTarget != closeBtn) return;
                    SubmitResult(null);
                    evt.StopPropagation();
                }, TrickleDown.TrickleDown);
            }
            closeBtn.AddToClassList("textPrompt-close-btn");
            container.Add(closeBtn);
            
            return container;

            void OnFocusIn(FocusEvent evt)
            {
#if UNITY_2022_3_OR_NEWER
                keyboard = textField.touchScreenKeyboard;
                if (submitBtn != null && keyboard != null)
                {
                    submitBtn.style.display = DisplayStyle.None;
                }
#endif
            }

            void OnFocusOut(FocusOutEvent evt)
            {
                if (focusOutIgnoreFrame == Time.frameCount)
                {
                    return;
                }
                if (submitBtn != null)
                {
                    submitBtn.style.display = DisplayStyle.Flex;
                }
                focusOutIgnoreFrame = Time.frameCount;
                if (keyboard != null)
                {
                    if (keyboard.status == TouchScreenKeyboard.Status.Done)
                    {
                        if (Application.platform == RuntimePlatform.Android)
                        {
                            // android drama :(
                            textField.Blur();
                            textField.schedule.Execute(HandleAndroidKeyboardFocusOutCheck);
                        }
                        else
                        {
                            TakeAutoCompleteSelectionOrSubmit(textField.text);
                        }
                    }
                    else
                    {
                        keyboard.active = false;
                        textField.Blur();
                        if (autoCompleteHolder == null || autoCompleteHolder.childCount == 0)
                        {
                            SubmitResult(null);
                        }
                    }
                }
                else
                {
#if UNITY_2022_3_OR_NEWER
                    textField.schedule.Execute(textField.Focus);
#else
                    TakeAutoCompleteSelectionOrSubmit(textField.text);
#endif
                }
            }

            void HandleAndroidKeyboardFocusOutCheck()
            {
                if(btnClickedFrame <= Time.frameCount + 1)
                {
                    TakeAutoCompleteSelectionOrSubmit(textField.text);
                }
                // else we clicked an auto complete button already and should be ignored.
            }

            void UpdateAutoCompleteList(string value)
            {
                if (autoCompleteList == null || data.AutoCompleteResultsCallback == null) return;
                autoCompleteSelectedBtn = null;
                autoCompleteList.Clear();
                data.AutoCompleteResultsCallback(value, autoCompleteList);
                autoCompleteHolder.Clear();
                for (var index = 0; index < autoCompleteList.Count; index++)
                {
                    var item = autoCompleteList[index];
                    var localItem = item;
                    var btn = new Button(() =>
                    {
                        AutoCompleteSelected(localItem);
                    });
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        btn.RegisterCallback<MouseDownEvent>((evt) =>
                        {
                            if (evt.currentTarget != btn) return;
                            btnClickedFrame = Time.frameCount;
                        }, TrickleDown.TrickleDown);
                    }
                    btn.text = localItem;
                    btn.AddToClassList("textPrompt-autocomplete-btn");
                    btn.AddToClassList("monoFont");
                    if (index == 0 && !data.CanSubmitWithoutAutoComplete)
                    {
                        autoCompleteSelectedBtn = btn;
                        btn.AddToClassList("nav-btn");
                    }
                    autoCompleteHolder.Add(btn);
                }
            }

            void ShiftAutoCompleteSelection(int dir)
            {
                if (autoCompleteHolder == null) return;
                if (autoCompleteSelectedBtn == null)
                {
                    if (autoCompleteHolder.childCount == 0) return;
                    autoCompleteSelectedBtn = autoCompleteHolder[0] as Button;
                    autoCompleteSelectedBtn?.AddToClassList("nav-btn");
                }
                else
                {
                    var index = autoCompleteHolder.IndexOf(autoCompleteSelectedBtn) + dir;
                    if (index < 0 || index >= autoCompleteHolder.childCount) return;
                    autoCompleteSelectedBtn.RemoveFromClassList("nav-btn");
                    autoCompleteSelectedBtn = autoCompleteHolder[index] as Button;
                    autoCompleteSelectedBtn?.AddToClassList("nav-btn");
                }
                SelectEnd();
            }

            void AutoCompleteSelected(string value)
            {
                btnClickedFrame = Time.frameCount;
                scrollView.verticalScroller.value = 0;
                if (data.AutoCompleteSelected?.Invoke(value) ?? true)
                {
                    textField.SetValueWithoutNotify(value);
                    SubmitResult(value);
                }
                else
                {
                    textField.value = value;
                    SelectEnd();
                }
            }

            void SelectEnd()
            {
                focusOutIgnoreFrame = Time.frameCount;
#if !UNITY_2023_2_OR_NEWER
                    if (textField.focusController.focusedElement == textField)
                    {
                        textField.Blur();  // issue in older unity
                    }
#endif
                textField.schedule.Execute(() =>
                {
                    if (textField.panel == null) return;
                    textField.SelectRange(textField.value.Length, textField.value.Length);
                    textField.Focus();
                });
            }

            void TakeAutoCompleteSelectionOrSubmit(string value)
            {
                if (autoCompleteSelectedBtn != null)
                {
                    AutoCompleteSelected(autoCompleteSelectedBtn.text);
                }
                else
                {
                    SubmitResult(value);
                }
            }

            void SubmitResult(string value)
            {
                focusOutIgnoreFrame = btnClickedFrame = Time.frameCount;
                textField.Blur();
                if (data.ResultCallback?.Invoke(value) ?? false)
                {
                    if (keyboard != null)
                    {
                        keyboard.active = false;
                    }
                    container.RemoveFromHierarchy();
                }
                else
                {
                    textField.AddToClassList("textPrompt-textfield-failed");
                    textField.schedule.Execute(() =>
                    {
                        textField.RemoveFromClassList("textPrompt-textfield-failed");
                        textField.Focus();
                    }).ExecuteLater(200);
                }
            }
        }

        public partial struct Data
        {
            public static Data CreateForNumberInput(Action<float> setter)
            {
                return CreateForNumberInputParse((v) =>
                {
                    if (!float.TryParse(v, out var number)) return false;
                    setter(number);
                    return true;
                }, false);
            }

            public static Data CreateForNumberInput(Action<double> setter)
            {
                return CreateForNumberInputParse((v) =>
                {
                    if (!double.TryParse(v, out var number)) return false;
                    setter(number);
                    return true;
                }, false);
            }

            public static Data CreateForNumberInput(Action<int> setter)
            {
                return CreateForNumberInputParse((v) =>
                {
                    if (!int.TryParse(v, out var number)) return false;
                    setter(number);
                    return true;
                }, true);
            }

            public static Data CreateForNumberInput(Action<uint> setter)
            {
                return CreateForNumberInputParse((v) =>
                {
                    if (!uint.TryParse(v, out var number)) return false;
                    setter(number);
                    return true;
                }, true);
            }

            public static Data CreateForNumberInput(Action<long> setter)
            {
                return CreateForNumberInputParse((v) =>
                {
                    if (!long.TryParse(v, out var number)) return false;
                    setter(number);
                    return true;
                }, true);
            }

            public static Data CreateForNumberInput(Action<ulong> setter)
            {
                return CreateForNumberInputParse((v) =>
                {
                    if (!ulong.TryParse(v, out var number)) return false;
                    setter(number);
                    return true;
                }, true);
            }

            public static Data CreateForNumberInputParse(Func<string, bool> tryParseAndSetFunc, bool isInterger)
            {
                return new Data()
                {
                    ResultCallback = (v) =>
                    {
                        if (v == null) return true;
                        return tryParseAndSetFunc(v);
                    },
                    KeyboardType = TouchScreenKeyboardType.NumbersAndPunctuation,
                    ValueChangeCallback = (input) => LoggerUtils.StripNonNumberString(input, isInterger)
                };
            }
        }
    }
}
#endif