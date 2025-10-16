using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Ninjadini.Logger;
using UnityEngine;

namespace Ninjadini.Console
{
    public partial class ConsoleOptions
    {
        public class Catalog
        {
#if !NJCONSOLE_DISABLE
            readonly ConsoleModules _modules;
            readonly GroupItem _root;
            
            readonly List<string> _paths = new();
            
#endif
            
#if !NJCONSOLE_DISABLE
            public Catalog(GroupItem root, ConsoleModules modules)
            {
                _root = root;
                _modules = modules;
            }
#endif
            
            /// <summary>
            /// Add a button to options menu.
            /// Example code:
            /// <code>
            /// var catalog = NjConsole.Options.CreateCatalog();
            /// catalog.AddButton("My First Button", () => Debug.Log("Clicked my first button"));
            /// 
            /// catalog.AddButton("A Directory / Child Directory / Child Button", () => Debug.Log("Child button was clicked"));
            /// 
            /// catalog.AddButton("My Space Bound Button", () => Debug.Log("Clicked my space bound button"))
            ///   .SetHeader("My Header")  // set header text to group buttons together
            ///   .BindToKeyboard(KeyCode.Space)   // bind to space keyboard press (if using new input system it'll be `Key.Space`)
            ///   .AutoCloseOverlay();  // auto close the overlay when clicked
            /// </code>
            /// </summary>
            /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
            /// <param name="callback">The action to call when button is pressed</param>
            /// <returns>A chain result so you can add Key bindings or auto close to that button. e.g. '.BindToKeyboard(KeyCode.Space).AutoCloseOverlay();'</returns>
            public ItemResultWithKeyBind AddButton(string path, Action callback)
            {
#if !NJCONSOLE_DISABLE
                var item = new ButtonItem()
                {
                    Callback = callback
                };
                AddItem(path, item);
                return new ItemResultWithKeyBind()
                {
                    Console = _modules,
                    OptionItem = item
                };
#else
                return new ItemResultWithKeyBind();
#endif
            }
            
            /// <summary>
            /// Add a toggle to options menu.
            /// Example code:
            /// <code>
            /// var toggle1 = false;
            /// var toggle2 = false;
            /// 
            /// var catalog = NjConsole.Options.CreateCatalog();
            /// catalog.AddToggle("My First Toggle", (v) => toggle1 = v, () => toggle1);
            /// 
            /// catalog.AddToggle("A Directory / My T Bound Toggle", (v) => toggle2 = v, () => toggle2);
            ///   .SetHeader("My Header")  // set header text to group buttons together
            ///   .BindToKeyboard(KeyCode.T)   // bind to T keyboard press (if using new input system it'll be `Key.T`)
            ///   .AutoCloseOverlay();  // auto close the overlay when clicked
            /// </code>
            /// It is Setter-getter instead of getter-setter param, because getter can be null for other methods such as AddNumberPrompt();
            /// </summary>
            /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
            /// <returns>A chain result so you can add Key bindings or auto close to that button. e.g. '.BindToKeyboard(KeyCode.Space).AutoCloseOverlay();'</returns>
            public ItemResultWithKeyBind AddToggle(string path, Action<bool> setter, Func<bool> getter)
            {
#if !NJCONSOLE_DISABLE
                var item = new ToggleItem()
                {
                    Getter = getter,
                    Setter = setter
                };
                AddItem(path, item);
                return new ItemResultWithKeyBind()
                {
                    Console = _modules,
                    OptionItem = item
                };
#else
                return new ItemResultWithKeyBind();
#endif
            }
        
            /// <summary>
            /// Add a text prompt button to options menu<br/>
            /// When the button is pressed, a text prompt will show where you can enter any text you want
            /// For general API help about Options feature, see AddButton();
            /// <code>
            /// catalog.AddTextPrompt("My Text Prompt", SetMyText);
            ///
            /// // a verison where it shows you the current value in the gui:
            /// var text = "Initial text";
            /// catalog.AddTextPrompt("My Text Prompt", (v) => text = v, () => text);
            /// </code>
            /// It is Setter-getter instead of getter-setter param, because getter can be null.
            /// </summary>
            /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
            /// <param name="setter">Callback for when the value is set</param>
            /// <param name="getter">Optional getter, if you want to show the current value in console GUI</param>
            /// <param name="multiline">Enable multiline text input</param>
            public ItemResult AddTextPrompt(string path, Action<string> setter, Func<string> getter = null, bool multiline = false)
            {
#if !NJCONSOLE_DISABLE
                var item = new TextItem()
                {
                    Getter = getter,
                    Data = new ConsoleTextPrompt.Data()
                    {
                        Multiline = multiline,
                        ResultCallback = setter == null ? null : (v) =>
                        {
                            if (v == null) return true;
                            setter(v);
                            return true;
                        }
                    }
                };
                return AddItem(path, item);
#else
                return new ItemResult();
#endif
            }
        
            /// <summary>
            /// Add a text prompt button to options menu with additional control<br/>
            /// For general API help about Options feature, see AddButton();
            /// <code>
            /// var text = "Initial text";
            /// catalog.AddTextPromptWithValidation("My validated text", 
            /// setter: v => {
            ///     if(v.All(char.IsUpper)) // in this example we only accept capital letters
            ///     {
            ///         text = v;
            ///         return true;
            ///     }
            ///     return false; // Return false to block user from closing the dialog with an invalid value.
            /// },
            /// getter: () => text, 
            /// validator: (v) => {
            ///     if (v.Length > 5) v = v.Substring(0, 5); // Trim out invalid characters and return the valid version (optional)
            ///     return v;
            /// } );
            /// </code>
            /// </summary>
            /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
            /// <param name="setter">Callback for when the value is set</param>
            /// <param name="getter">Optional getter, if you want to show the current value in console GUI</param>
            public ItemResult AddTextPromptWithValidation(string path, Func<string, bool> setter, Func<string> getter = null, Func<string, string> validator = null, TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default, bool multiline = false)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new TextItem()
                {
                    Getter = getter,
                    Data = new ConsoleTextPrompt.Data()
                    {
                        Multiline = multiline,
                        ResultCallback = setter == null ? null : (v) => v == null || setter(v),
                        ValueChangeCallback = validator,
                        KeyboardType = keyboardType
                    }
                });
#else
                return new ItemResult();
#endif
            }
        
            /// <summary>
            /// Add a number (float type) prompt button to options menu<br/>
            /// Set btnDeltaSteps to also add buttons on the side to increment and decrement the number without opening the prompt window.<br/>
            /// For general API help about Options feature, see AddButton();
            /// </summary>
            /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
            /// <param name="setter">Callback for when the value is set</param>
            /// <param name="getter">Optional getter, if you want to show the current value in console GUI</param>
            /// <param name="btnDeltaSteps">The delta amount of value change when pressing the left and right arrows</param>
            public ItemResult AddNumberPrompt(string path, Action<float> setter, Func<float> getter = null, float btnDeltaSteps = 0f)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new NumberItem()
                {
                    Getter = getter == null ? null : () => getter().ToString(CultureInfo.InvariantCulture),
                    Data = setter == null ? default : ConsoleTextPrompt.Data.CreateForNumberInput(setter),
                    DeltaStepCallback = setter == null || btnDeltaSteps <= 0f || getter == null ? null : direction => setter(getter() + direction * btnDeltaSteps),
                });
#else
                return new ItemResult();
#endif
            }
        
            /// <summary>
            /// Add a number (double type) prompt button to options menu<br/>
            /// Set btnDeltaSteps to also add buttons on the side to increment and decrement the number without opening the prompt window.<br/>
            /// For general API help about Options feature, see AddButton();
            /// </summary>
            /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
            /// <param name="setter">Callback for when the value is set</param>
            /// <param name="getter">Optional getter, if you want to show the current value in console GUI</param>
            /// <param name="btnDeltaSteps">The delta amount of value change when pressing the left and right arrows</param>
            public ItemResult AddNumberPrompt(string path, Action<double> setter, Func<double> getter = null, double btnDeltaSteps = 0)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new NumberItem()
                {
                    Getter = getter == null ? null : () => getter().ToString(CultureInfo.InvariantCulture),
                    Data = setter == null ? default : ConsoleTextPrompt.Data.CreateForNumberInput(setter),
                    DeltaStepCallback = setter == null || btnDeltaSteps <= 0 || getter == null  ? null : direction => setter(getter() + direction * btnDeltaSteps),
                });
#else
                return new ItemResult();
#endif
            }
        
            /// <summary>
            /// Add a number (int type) prompt button to options menu<br/>
            /// Set btnDeltaSteps to also add buttons on the side to increment and decrement the number without opening the prompt window.<br/>
            /// For general API help about Options feature, see AddButton();
            /// </summary>
            /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
            /// <param name="setter">Callback for when the value is set</param>
            /// <param name="getter">Optional getter, if you want to show the current value in console GUI</param>
            /// <param name="btnDeltaSteps">The delta amount of value change when pressing the left and right arrows</param>
            public ItemResult AddNumberPrompt(string path, Action<int> setter, Func<int> getter = null, int btnDeltaSteps = 0)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new NumberItem()
                {
                    Getter = getter == null ? null : () => getter().ToString(CultureInfo.InvariantCulture),
                    Data = setter == null ? default : ConsoleTextPrompt.Data.CreateForNumberInput(setter),
                    DeltaStepCallback = setter == null || btnDeltaSteps <= 0 || getter == null  ? null : direction => setter(getter() + direction * btnDeltaSteps),
                });
#else
                return new ItemResult();
#endif
            }
        
            /// <summary>
            /// Add a number (uint type) prompt button to options menu<br/>
            /// For general API help about Options feature, see AddButton();<br/>
            /// </summary>
            /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
            /// <param name="setter">Callback for when the value is set</param>
            /// <param name="getter">Optional getter, if you want to show the current value in console GUI</param>
            public ItemResult AddNumberPrompt(string path, Action<uint> setter, Func<uint> getter = null)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new NumberItem()
                {
                    Getter = getter == null ? null : () => getter().ToString(CultureInfo.InvariantCulture),
                    Data = setter == null ? default : ConsoleTextPrompt.Data.CreateForNumberInput(setter)
                });
#else
                return new ItemResult();
#endif
            }
        
            /// <summary>
            /// Add a number (long type) prompt button to options menu<br/>
            /// Set btnDeltaSteps to also add buttons on the side to increment and decrement the number without opening the prompt window.<br/>
            /// For general API help about Options feature, see AddButton();
            /// </summary>
            /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
            /// <param name="setter">Callback for when the value is set</param>
            /// <param name="getter">Optional getter, if you want to show the current value in console GUI</param>
            /// <param name="btnDeltaSteps">The delta amount of value change when pressing the left and right arrows</param>
            public ItemResult AddNumberPrompt(string path, Action<long> setter, Func<long> getter = null, long btnDeltaSteps = 0)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new NumberItem()
                {
                    Getter = getter == null ? null : () => getter().ToString(CultureInfo.InvariantCulture),
                    Data = setter == null ? default : ConsoleTextPrompt.Data.CreateForNumberInput(setter),
                    DeltaStepCallback = setter == null || btnDeltaSteps <= 0 || getter == null ? null : direction => setter(getter() + direction * btnDeltaSteps),
                });
#else
                return new ItemResult();
#endif
            }
        
        
            /// <summary>
            /// Add a number (ulong type) prompt button to options menu<br/>
            /// For general API help about Options feature, see AddButton();
            /// </summary>
            /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
            /// <param name="setter">Callback for when the value is set</param>
            /// <param name="getter">Optional getter, if you want to show the current value in console GUI</param>
            public ItemResult AddNumberPrompt(string path, Action<ulong> setter, Func<ulong> getter = null)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new NumberItem()
                {
                    Getter = getter == null ? null : () => getter().ToString(CultureInfo.InvariantCulture),
                    Data = setter == null ? default : ConsoleTextPrompt.Data.CreateForNumberInput(setter)
                });
#else
                return new ItemResult();
#endif
            }
        
        
            /// <summary>
            /// Add a dropdown choice to options menu.<br/>
            /// If you modify the choices list later, it will be reflected in the dropdown.
            /// You may also use onBeforeDropDownListing callback to update the list just before showing the dropdown list.<br/>
            /// For general API help about Options feature, see AddButton();
            /// </summary>
            public ItemResult AddChoice(string path, 
                List<string> choices, 
                Action<int> setSelectedIndex,
                Func<int> getSelectedIndex, 
                Action onBeforeDropDownListing = null)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new ChoiceItem()
                {
                    List = choices,
                    GetSelectedIndex = getSelectedIndex,
                    SetSelectedIndex = setSelectedIndex,
                    OnBeforeDropDownListing = onBeforeDropDownListing
                });
#else
                return new ItemResult();
#endif
            }
            
        
            /// <summary>
            /// Add a dropdown choice to options menu.<br/>
            /// If you modify the choices list later, it will be reflected in the dropdown.
            /// You may also use onBeforeDropDownListing callback to update the list just before showing the dropdown list.<br/>
            /// For general API help about Options feature, see AddButton();
            /// </summary>
            public ItemResult AddChoice(string path, 
                string[] choices, 
                Action<int> setSelectedIndex,
                Func<int> getSelectedIndex)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new ChoiceItem()
                {
                    List = choices.ToList(),
                    GetSelectedIndex = getSelectedIndex,
                    SetSelectedIndex = setSelectedIndex
                });
#else
                return new ItemResult();
#endif
            }
            
        
            /// <summary>
            /// Add a dropdown enum choice to options menu.<br/>
            /// If you modify the choices list later, it will be reflected in the dropdown.
            /// You may also use onBeforeDropDownListing callback to update the list just before showing the dropdown list.<br/>
            /// For general API help about Options feature, see AddButton();
            /// </summary>
            public ItemResult AddEnumChoice<T>(string path, 
                Action<T> setSelectedEnum, 
                Func<T> getSelectedEnum) where T : Enum
            {
#if !NJCONSOLE_DISABLE
                var choices = Enum.GetNames(typeof(T)).ToList();
                var values = (T[])Enum.GetValues(typeof(T));
                return AddItem(path, new ChoiceItem()
                {
                    List = choices,
                    GetSelectedIndex = () => Array.IndexOf(values, getSelectedEnum()),
                    SetSelectedIndex = (index) =>
                    {
                        if (index >= 0 && index < values.Length)
                        {
                            setSelectedEnum(values[index]);
                        }
                    }
                });
#else
                return new ItemResult();
#endif
            }

        
            /// <summary>
            /// Add a command line bound function call. It can have multiple basic arguments and return value.<br/>
            /// This item can only be operated by using the command line feature.<br/>
            /// If you are passing a lambda function, you will need to cast it to a proper type, such as Action&lt;string&gt;, Func&lt;string, bool&gt; etc.
            /// </summary>
            /// <param name="path">Path of the command. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Cmd</param>
            /// <param name="method">MethodInfo to map to the command</param>
            /// <param name="target">Target object to call</param>
            /// <param name="getter">Optional getter callback when no param is passed to the command</param>
            public ItemResult AddCommand(string path, MethodInfo method, object target, Func<object> getter = null)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new CmdItem()
                {
                    Method = method,
                    Target = target,
                    Getter = getter
                });
#else
                return new ItemResult();
#endif
            }
            
            /// If you are passing a lambda function, you will need to cast it to a proper type, such as Action&lt;string&gt;, Func&lt;string, bool&gt; etc.
            /// </summary>
            /// <param name="path">Path of the command. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Cmd</param>
            /// <param name="func">Delegate function to map to the command</param>
            /// <param name="getter">Optional getter callback when no param is passed to the command</param>
            public ItemResult AddCommand(string path, Delegate func, Func<object> getter = null)
            {
#if !NJCONSOLE_DISABLE
                return AddItem(path, new CmdItem()
                {
                    Method = func.Method,
                    Target = func.Target,
                    Getter = getter
                });
#else
                return new ItemResult();
#endif
            }

            /// <summary>
            /// Add options to this catalog using object reflection<br/>
            /// This is an alternative path to creating menus. Using [ConsoleOption] attribute.<br/>
            /// Using this call will NOT automatically remove the options when the monobehaviour is destroyed.<br/>
            /// It is only used to add additional options to an existing catalog.<br/>
            /// You should instead use `NjConsole.Options.CreateCatalogFrom(this, "TestOptions");`<br/><br/>
            /// To add static fields/properties/methods, you need to pass the type instead `AddByReflection(typeof(MyStaticOptionsClass))`
            ///
            /// Declare some example options in your code:
            ///<code>
            /// 
            /// public class TestOptions : MonoBehaviour {
            ///
            /// void Start() {
            ///     NjConsole.Options.CreateCatalogFrom(this, "TestOptions");
            ///     // ^ second param `TestOptions` is optional, it puts all the items inside the `TestOptions` group in this example.
            ///     // If the item being created from is a MonoBehaviour, it will automatically remove the options when the monobehaviour is destroyed.
            /// }
            /// 
            /// [ConsoleOption]
            /// void SayHello() {
            ///     Debug.Log("Hello");
            /// }
            ///
            /// [ConsoleOption("Set Player Name")]
            /// public void SetName(string newName) {
            ///     Debug.Log("SetName caleld: " + newName);
            /// }
            ///
            /// [ConsoleOption(key:UnityEngine.InputSystem.Key.I, header:"Player")] // bind to keyboard key I and put inside header sub-group "Player"
            /// bool InfiniteLives;
            ///
            /// [ConsoleOption(increments:1, header:"Player")]
            /// int Health {get; set;}
            ///
            /// [ConsoleOption(increments:1, header:"Player")]
            /// [Range(1, 10)]
            /// float Speed;
            ///
            /// }
            /// 
            /// </code>
            /// </summary>
            public void AddByReflection(object obj, string parentPath = null)
            {
#if !NJCONSOLE_DISABLE
                ConsoleOptions.AddByReflection(obj, this, parentPath);
#endif
            }

#if !NJCONSOLE_DISABLE
            public ItemResult AddItem(string path, OptionItem item)
            {
                item.Catalog = this;
                _root.Add(path, item);
                _paths.Add(path);
                return new ItemResult(item);
            }
#endif
            
            /// <summary>
            /// Remove an options item by path. This will only remove the item if the item was added from this catalog instance.
            /// </summary>
            public void Remove(string path)
            {
#if !NJCONSOLE_DISABLE
                LoggerUtils.RemoveSwapBack(_paths, path);
                _root.Remove(path, this);
#endif
            }
            
            
            /// <summary>
            /// Remove an options item by path. This will remove the item even if the item was added from another catalog instance.
            /// You could have multiple catalog instances with the same path - which causes conflicts.
            /// this is a way to remove them even if an option is registered from another catalog.
            /// </summary>
            public void RemoveIncludingConflicts(string path)
            {
#if !NJCONSOLE_DISABLE
                LoggerUtils.RemoveSwapBack(_paths, path);
                _root.Remove(path);
#endif
            }

            /// <summary>
            /// Remove all items from catalog. This will only remove items that was added from this catalog instance.
            /// </summary>
            public void RemoveAll()
            {
#if !NJCONSOLE_DISABLE
                foreach (var path in _paths)
                {
                    _root.Remove(path, this);
                }
                _paths.Clear();
#endif
            }

            /// <summary>
            /// Remove all items from catalog. This will remove items even if the item was added from another catalog instance.
            /// You could have multiple catalog instance with the same path - which causes conflicts.
            /// this is a way to remove them even if an option is registered from another catalog.
            /// </summary>
            public void RemoveAllIncludingConflicts()
            {
#if !NJCONSOLE_DISABLE
                foreach (var path in _paths)
                {
                    _root.Remove(path);
                }
                _paths.Clear();
#endif
            }
        }
    }
}