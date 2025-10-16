#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ninjadini.Console.UI;
using Ninjadini.Logger;
using UnityEngine;

namespace Ninjadini.Console
{
    public partial class ConsoleOptions
    {
        static void AddByReflection(object obj, Catalog catalog, string parentPath)
        {
            var type = obj.GetType();
            if (parentPath != null)
            {
                parentPath = parentPath.Trim();
                if (!parentPath.EndsWith("/"))
                {
                    parentPath += "/";
                }
            }
            var bindings = BindingFlags.NonPublic | BindingFlags.Public;
            if (obj is Type t)
            {
                type = t;
                bindings |= BindingFlags.Static;
            }
            else
            {
                bindings |= BindingFlags.Instance;
            }
            var members = type.GetMembers(bindings);
            foreach (var memberInfo in members)
            {
                if (!memberInfo.IsDefined(typeof(ConsoleOptionAttribute)))
                {
                    continue;
                }
                var attribute = memberInfo.GetCustomAttribute<ConsoleOptionAttribute>();
                var path = parentPath + (string.IsNullOrEmpty(attribute.Path) ? memberInfo.Name : attribute.Path);

                if (memberInfo is FieldInfo fieldInfo)
                {
                    Func<object, object> getValue = fieldInfo.GetValue;
                    Action<object, object> setValue = fieldInfo.SetValue;
                    AddByReflection(catalog, path, obj, fieldInfo.FieldType, 
                        getValue, setValue, 
                        attribute, fieldInfo);
                }
                else if (memberInfo is PropertyInfo propertyInfo)
                {
                    Func<object, object> getValue = propertyInfo.CanRead ? propertyInfo.GetValue : null;
                    Action<object, object> setValue = propertyInfo.CanWrite ? propertyInfo.SetValue : null;
                    AddByReflection(catalog, path, obj, propertyInfo.PropertyType, 
                        getValue,  setValue, 
                        attribute, propertyInfo);
                }
                else if (memberInfo is MethodInfo methodInfo)
                {
                    var parameters = methodInfo.GetParameters();
                    if (parameters.Length == 0)
                    {
                        var opt = catalog.AddButton(path, () =>
                        {
                            HandleMethodReturn(methodInfo.Invoke(obj, Array.Empty<object>()));
                        });
    #if ENABLE_LEGACY_INPUT_MANAGER
                        if (attribute.Key != KeyCode.None)
                        {
                            opt.BindToKeyboard(attribute.Key, attribute.KeyModifier);
                        }
    #elif ENABLE_INPUT_SYSTEM
    // if you are seeing an error here, it means you have enabled the new input system but haven't installed the package.
    // Install InputSystem package in package manager.
    // Alternatively, go to Project Settings > Player > Other Settings > Active Input Handling > choose Input Manager (old)
                        if (attribute.Key != UnityEngine.InputSystem.Key.None)
                        {
                            opt.BindToKeyboard(attribute.Key, attribute.KeyModifier);
                        }
    #endif
                        AddedItem(opt, attribute, methodInfo);
                        continue;
                    }
                    var success = false;
                    if (parameters.Length == 1)
                    {
                        success = AddByReflection(catalog, path, obj, parameters[0].ParameterType, 
                            null,  
                            (o, v) =>
                            {
                                var objs = BorrowTempParams(v);
                                HandleMethodReturn(methodInfo.Invoke(o, objs));
                                ReturnTempObjs(objs);
                            },
                            attribute, methodInfo);
                    }
                    if (!success)
                    {
                        AddedItem(
                            catalog.AddItem(path, new CmdItem()
                            {
                                Method = methodInfo,
                                Target = methodInfo.IsStatic ? null : obj
                            })
                            , attribute, methodInfo);
                    }
                }
            }
        }

        static void HandleMethodReturn(object result)
        {
            if (result == null)
            {
                return;
            }
            if (result is IConsoleCommandlineModule commandlineModule)
            {
                var clElement = ConsoleContext.TryGetFocusedContext()?.Window?.OpenAndFocusOnCommandLine();
                if (clElement == null)
                {
                    return;
                }
                clElement.SetLockedModule(commandlineModule);
            }
            else
            {
                if (!OptionCommandsModule.TrySetReturnedResultToCurrentContext(result))
                {
                    NjLogger.Info($"<color=#7FFFB0>> Option button returned ", result is string str ? $"‘<noparse>{str}</noparse>’" : result);
                }
            }
        }

        static object[] BorrowTempParams(object obj)
        {
            object[] objs;
            if (_tempParams.Count > 0)
            {
                objs = _tempParams[^1];
                _tempParams.RemoveAt(_tempParams.Count - 1);
            }
            else objs = new object[1];
            objs[0] = obj;
            return objs;
        }

        static void ReturnTempObjs(object[] objs)
        {
            if (_tempParams.Count >= 16) return;
            objs[0] = null;
            _tempParams.Add(objs);
        }

        static readonly List<object[]> _tempParams = new ();

        static bool AddByReflection(Catalog catalog, string path, object obj, Type type, 
            Func<object, object> getValue, Action<object, object> setValue, 
            ConsoleOptionAttribute attribute, MemberInfo member)
        {
            if (type == typeof(bool))
            {
                if (getValue == null)
                {
                    if (setValue != null)
                    {
                        AddedItem(catalog.AddCommand(path,
                            ((Action<object>)((v) => setValue(obj, v))).Method,
                            obj
                        ), attribute, member);
                    }
                    return false;
                }
                var opt = catalog.AddToggle(path, 
                    setValue == null ? null : (v) => setValue(obj, v), 
                    () => (bool)getValue(obj));
                
#if ENABLE_LEGACY_INPUT_MANAGER
                    if (attribute.Key != KeyCode.None)
                    {
                        opt.BindToKeyboard(attribute.Key, attribute.KeyModifier);
                    }
#elif ENABLE_INPUT_SYSTEM
                    if (attribute.Key != UnityEngine.InputSystem.Key.None)
                    {
                        opt.BindToKeyboard(attribute.Key, attribute.KeyModifier);
                    }
#endif
                AddedItem(opt, attribute, member);
                return true;
            }
            else if (type == typeof(string))
            {
                AddedItem(
                catalog.AddTextPrompt(path, 
                    setValue == null ? null : (v) => setValue(obj, v), 
                    getValue == null ? null : () => getValue(obj) as string, 
                    member.IsDefined(typeof(MultilineAttribute)))
                , attribute, member);
                return true;
            }
            else if (type == typeof(int))
            {
                var range = member.GetCustomAttribute<RangeAttribute>();
                var min = int.MinValue;
                var max = int.MaxValue;
                if (range != null)
                {
                    min = (int)range.min;
                    max = (int)range.max;
                }
                AddedItem(
                catalog.AddNumberPrompt(path, 
                    setValue == null ? null : (v) => setValue(obj, Mathf.Clamp(v, min, max)),
                    getValue == null ? null : () => (int)getValue(obj), 
                    (int)Math.Round(attribute.Increments <= 0 ? 1 : attribute.Increments))
                , attribute, member);
                return true;
            }
            else if (type == typeof(float))
            {
                var range = member.GetCustomAttribute<RangeAttribute>();
                var min = range?.min ?? float.MinValue;
                var max = range?.max ?? float.MaxValue;
                AddedItem(
                catalog.AddNumberPrompt(path, 
                    setValue == null ? null : (v) => setValue(obj, Mathf.Clamp(v, min, max)), 
                    getValue == null ? null : () => (float)getValue(obj), 
                    (float)attribute.Increments)
                , attribute, member);
                return true;
            }
            else if (type == typeof(double))
            {
                var range = member.GetCustomAttribute<RangeAttribute>();
                var min = double.MinValue;
                var max = double.MaxValue;
                if (range != null)
                {
                    min = range.min;
                    max = range.max;
                }
                AddedItem(
                catalog.AddNumberPrompt(path,
                    setValue == null ? null : (v) => setValue(obj, Math.Clamp(v, min, max)), 
                    getValue == null ? null : () => (double)getValue(obj), 
                    attribute.Increments)
                , attribute, member);
                return true;
            }
            else if (type == typeof(long))
            {
                var range = member.GetCustomAttribute<RangeAttribute>();
                var min = long.MinValue;
                var max = long.MaxValue;
                if (range != null)
                {
                    min = (long)range.min;
                    max = (long)range.max;
                }
                AddedItem(
                catalog.AddNumberPrompt(path, 
                    setValue == null ? null : (v) => setValue(obj, Math.Min(Math.Max(v, min), max)), 
                    getValue == null ? null : () => (long)getValue(obj),
                    (long)attribute.Increments)
                , attribute, member);
                return true;
            }
            else if (type == typeof(uint))
            {
                var range = member.GetCustomAttribute<RangeAttribute>();
                var min = uint.MinValue;
                var max = uint.MaxValue;
                if (range != null)
                {
                    min = (uint)Math.Max(0, range.min);
                    max = (uint)Math.Max(0, range.max);
                }
                AddedItem(
                catalog.AddNumberPrompt(path,
                    setValue == null ? null : (v) => setValue(obj,  Math.Min(Math.Max((long)v, min), max)),
                    getValue == null ? null : () => (uint)getValue(obj)
                    )
                , attribute, member);
                return true;
            }
            else if (type == typeof(ulong))
            {
                var range = member.GetCustomAttribute<RangeAttribute>();
                var min = ulong.MinValue;
                var max = ulong.MaxValue;
                if (range != null)
                {
                    min = (ulong)Math.Max(0, range.min);
                    max = (ulong)Math.Max(0, range.max);
                }
                AddedItem(catalog.AddNumberPrompt(path,
                    setValue == null ? null : (v) => setValue(obj, Math.Min(Math.Max(v, min), max)),
                    getValue == null ? null : () => (ulong)getValue(obj)), attribute, member);
                return true;
            }
            else if (type.IsEnum)
            {
                if (getValue == null)
                {
                    if (setValue != null)
                    {
                        AddedItem(catalog.AddCommand(path,
                            ((Action<object>)((v) => setValue(obj, v))).Method,
                            obj
                            ), attribute, member);
                    }
                    return false;
                }
                var choices = Enum.GetNames(type).ToList();
                var values = Enum.GetValues(type);
                catalog.AddItem(path, new ChoiceItem()
                {
                    List = choices,
                    GetSelectedIndex = () => Array.IndexOf(values, getValue(obj)),
                    SetSelectedIndex = (index) =>
                    {
                        if (setValue != null && index >= 0 && index < values.Length)
                        {
                            setValue(obj, values.GetValue(index));
                        }
                    },
                    Header = attribute.Header
                });
                return true;
            }
            else if(member is not MethodInfo)
            {
                Func<object> getter = getValue == null ? null : () => getValue(obj);
                var method = getter?.Method;
                if (setValue != null)
                {
                    method = ((Action<object>)((v) => setValue(obj, v))).Method;
                }
                AddedItem(
                    catalog.AddItem(path, new CmdItem()
                            {
                                Method = method,
                                Target = obj,
                                Getter = getter
                            })
                    , attribute, member);
                return true;
            }
            return false;
        }

        static void AddedItem(ItemResult result, ConsoleOptionAttribute attribute, MemberInfo member)
        {
            if (attribute.Header != null)
            {
                result.SetHeader(attribute.Header);
            }
            var tooltip = member.GetCustomAttribute<TooltipAttribute>();
            if (tooltip != null)
            {
                result.SetTooltip(tooltip.tooltip);
            }
        }

        static void AddedItem(ItemResultWithKeyBind result, ConsoleOptionAttribute attribute, MemberInfo member)
        {
            if (attribute.Header != null)
            {
                result.SetHeader(attribute.Header);
            }
            if (attribute.AutoClose)
            {
                result.AutoCloseOverlay();
            }
            var tooltip = member.GetCustomAttribute<TooltipAttribute>();
            if (tooltip != null)
            {
                result.SetTooltip(tooltip.tooltip);
            }
        }
    }
}
#endif