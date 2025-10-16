#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Reflection;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
// if you are seeing an error here, it means you have enabled the new input system but haven't installed the package.
// Install InputSystem package in package manager.
// Alternatively, go to Project Settings > Player > Other Settings > Active Input Handling > choose Input Manager (old)
using UnityEngine.InputSystem;
#endif

namespace Ninjadini.Console
{
    public partial class ConsoleOptions
    {
        public abstract class OptionItem
        {
            internal string Name;
            public string Path { get; internal set; }
            public string Header { get; internal set; }
            public Catalog Catalog { get; internal set; }
            
            public string Tooltip;
            
            public string GetName() => Name;

            ConsoleKeyBindings _keyBindings;
            
// we prioritise legacy because:
// In Unity 2022, you can have input set to both, but not have the actual input package installed.
// We want those cases to still work
#if ENABLE_LEGACY_INPUT_MANAGER
            KeyCode _key;
#elif ENABLE_INPUT_SYSTEM
            Key _key;
#endif
            ConsoleKeyBindings.Modifier _mods;
            
            public abstract VisualElement CreateElement(ConsoleOptions options, ConsoleContext context);

            protected void RecordUsageToHistory(ConsoleOptions options)
            {
                options.History.Add(Path);
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            public void BindToKeyboard(ConsoleKeyBindings keyBindings, KeyCode key, ConsoleKeyBindings.Modifier modifiers)
#else
            public void BindToKeyboard(ConsoleKeyBindings keyBindings, Key key, ConsoleKeyBindings.Modifier modifiers)
#endif
            {
#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
                _keyBindings?.UnbindKeyDown(_key, _mods);
                var act = KeyboardBindingCallback;
                if (act != null)
                {
                    _keyBindings = keyBindings;
                    _key = key;
                    _mods = modifiers;
                    _keyBindings?.BindKeyDown(act, key, modifiers);
                }
                else
                {
                    _key = 0;
                }
#endif
            }

            public void OnRemoved()
            {
#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
                var act = KeyboardBindingCallback;
                if (act != null)
                {
                    _keyBindings?.UnbindKeyDown(_key, _mods, act);
                }
#endif
            }

            protected void AddKeyBindingTextToElement(VisualElement element, ConsoleContext context)
            {
#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
                if (_key == 0) return;
                var keyBindings = context.Modules.GetModule<ConsoleKeyBindings>(true);
                
                if (keyBindings == null) return;
                if (!keyBindings.IsBoundToKeyDown(_key, _mods, KeyboardBindingCallback)) return;
                
                var mod = ConsoleKeyBindings.GetModKeysName(_mods);
                var str = ConsoleKeyBindings.GetKeyName(_key);
                if (mod.Length > 0)
                {
                    str = $"{mod}<b>{str}<b>";
                }
                var lbl = new Label(str);
                lbl.AddToClassList("options-option-btn-keyBind");
                element.Add(lbl);
#endif
            }

            protected virtual Action KeyboardBindingCallback => null;
        }

        internal class ButtonItem : OptionItem
        {
            public Action Callback { get; internal set; }
            public bool AutoCloseOverlay;

            public override VisualElement CreateElement(ConsoleOptions options, ConsoleContext context)
            {
                var btn = new Button(() =>
                {
                    RecordUsageToHistory(options);
                    if (AutoCloseOverlay)
                    {
                        context.RuntimeOverlay?.Hide();
                    }
                    Callback?.Invoke();
                });
                var lbl = new Label(Name); // < btn.text doesn't expand the button if the text is too long, not sure how to control it.
                lbl.AddToClassList("options-option-btn-lbl");
                btn.Add(lbl);
                AddKeyBindingTextToElement(btn, context);
                return btn;
            }

            protected override Action KeyboardBindingCallback => Callback;
        }
        
        internal class ToggleItem : OptionItem
        {
            public Func<bool> Getter { get; internal set; }
            public Action<bool> Setter { get; internal set; }

            Action _toggleAct;
            public bool AutoCloseOverlay;

            public override VisualElement CreateElement(ConsoleOptions options, ConsoleContext context)
            {
                var toggle = new Toggle(Name);
                toggle.AddToClassList("options-option-toggle");
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (Setter == null)
                    {
                        Update();
                        return;
                    }
                    RecordUsageToHistory(options);
                    if (AutoCloseOverlay)
                    {
                        context.RuntimeOverlay?.Hide();
                    }
                    Setter?.Invoke(evt.newValue);
                });
                toggle.schedule.Execute(Update).Every(100);
                Update();
                AddKeyBindingTextToElement(toggle, context);
                return toggle;

                void Update()
                {
                    if (Getter != null)
                    {
                        toggle.value = Getter();
                    }
                }
            }

            protected override Action KeyboardBindingCallback
            {
                get
                {
                    if (_toggleAct != null) return _toggleAct;
                    if (Getter == null) return null;
                    _toggleAct = () =>
                    {
                        Setter?.Invoke(!Getter());
                    };
                    return _toggleAct;
                }
            }
        }
        
        internal class TextItem : OptionItem
        {
            public Func<string> Getter { get; internal set; }

            public ConsoleTextPrompt.Data Data;
            
            public override VisualElement CreateElement(ConsoleOptions options, ConsoleContext context)
            {
                return CreateElements(options).element;
            }
            
            protected (VisualElement element, Action updatePreview) CreateElements(ConsoleOptions options)
            {
                var btn = new Button();
                btn.style.flexDirection = FlexDirection.Column;
                btn.style.justifyContent = Justify.Center;
                btn.style.alignItems = Align.Center;
                
                var topLabel = new Label(Name);
                topLabel.AddToClassList("options-option-btn-upperText");
                btn.Add(topLabel);

                Action updatePreview = null;
                if (Getter != null)
                {
                    var valueLbl = new Label();
                    valueLbl.AddToClassList("options-option-btn-lowerText");
                    updatePreview = () =>
                    {
                        var txt = Getter?.Invoke() ?? "";
                        btn.tooltip = txt.Length > 20 ? txt : "";
                        valueLbl.text = LoggerUtils.GetFirstLine(txt);
                    };
                    valueLbl.schedule.Execute(updatePreview).Every(1000);
                    btn.Add(valueLbl);
                }
                
                // due to multi UI nature, its harder to manage instances so we'll just wrap it
                btn.clickable = Data.ResultCallback == null ? null : new Clickable(() =>
                {
                    var dataCopy = Data;
                    if (string.IsNullOrEmpty(dataCopy.Title))
                    {
                        dataCopy.Title = Name;
                    }
                    dataCopy.InitialText = Getter?.Invoke() ?? "";
                    dataCopy.ResultCallback = (v) =>
                    {
                        var result = Data.ResultCallback?.Invoke(v) ?? true;
                        if (result)
                        {
                            RecordUsageToHistory(options);
                            updatePreview?.Invoke();
                        }
                        return result;
                    };
                    var prompt = ConsoleTextPrompt.ShowInConsoleRoot(btn, dataCopy);
                    ConsoleUIUtils.AutoRemoveAWhenBIsRemoved(prompt, btn);
                });
                return (btn, updatePreview);
            }
        }
        
        internal class NumberItem : TextItem
        {
            public Action<int> DeltaStepCallback;
            
            public override VisualElement CreateElement(ConsoleOptions options, ConsoleContext context)
            {
                var baseElement = CreateElements(options);
                if (DeltaStepCallback != null)
                {
                    baseElement.element.style.paddingLeft = 25;
                    baseElement.element.style.paddingRight = 25;
                    
                    var leftBtn = new Button(() =>
                    {
                        DeltaStepCallback(-1);
                        RecordUsageToHistory(options);
                        baseElement.updatePreview();
                    });
                    leftBtn.AddToClassList("options-option-btn-sides");
                    leftBtn.style.left = -5;
                    leftBtn.style.width = 26;
                    leftBtn.text = "\u25c0";
                    baseElement.element.Add(leftBtn);
                    var rightBtn = new Button(() =>
                    {
                        DeltaStepCallback(1);
                        RecordUsageToHistory(options);
                        baseElement.updatePreview();
                    });
                    rightBtn.AddToClassList("options-option-btn-sides");
                    rightBtn.style.right = -5;
                    rightBtn.style.width = 26;
                    rightBtn.text = "\u25b6";
                    baseElement.element.Add(rightBtn);
                }
                return baseElement.element;
            }
        }

        internal class ChoiceItem : OptionItem
        {
            public List<string> List { get; internal set; }
            public Func<int> GetSelectedIndex { get; internal set; }
            public Action<int> SetSelectedIndex { get; internal set; }
            public Action OnBeforeDropDownListing { get; internal set; }

            public override VisualElement CreateElement(ConsoleOptions options, ConsoleContext context)
            {
                var dropDown = new DropdownField(Name);
                ConsoleUIUtils.FixDropdownFieldPopupSize(dropDown, context);
                dropDown.AddToClassList("options-option-dropdown");
                dropDown.choices = List ??= new List<string>();
                
                if (OnBeforeDropDownListing != null)
                {
                    dropDown.RegisterCallback<FocusEvent>(OnFocusEvent);
                }
                dropDown.RegisterValueChangedCallback(OnValueChanged);
                if (GetSelectedIndex != null)
                {
                    dropDown.schedule.Execute(Update).Every(100);
                    Update();
                }
                return dropDown;
                
                void Update()
                {
                    dropDown.index = GetSelectedIndex();
                }
                void OnFocusEvent(FocusEvent evt)
                {
                    if (evt.currentTarget != evt.target) return;
                    OnBeforeDropDownListing();
                    dropDown.choices = List;
                    dropDown.MarkDirtyRepaint();
                }
                void OnValueChanged(ChangeEvent<string> evt)
                {
                    if (SetSelectedIndex == null)
                    {
                        Update();
                        return;
                    }
                    SetSelectedIndex(dropDown.index);
                    RecordUsageToHistory(options);
                }
            }
        }

        internal class CmdItem : OptionItem
        {
            public MethodInfo Method;
            public object Target;
            public Func<object> Getter;
            
            public override VisualElement CreateElement(ConsoleOptions options, ConsoleContext context)
            {
                return null;
            }
        }
    }
}
#endif