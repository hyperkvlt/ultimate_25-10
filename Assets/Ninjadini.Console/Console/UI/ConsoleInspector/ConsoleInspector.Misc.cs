#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Ninjadini.Console.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleInspector 
    {
        public static void StartClassesInspector(VisualElement visualSrc)
        {
            ShowClassSearchPrompt(visualSrc, type =>
            {
                if (type != null)
                {
                    var parent = ConsoleUIUtils.FindConsoleOrRoot(visualSrc);
                    Show(parent, type);
                }
            });
        }

        public static void ShowClassSearchPrompt(VisualElement visualSrc, Action<Type> result)
        {
            var typesList = new List<(string name, Type type)>();
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Thread_FillTypesToList(typesList);
                // Threads not supported in WebGL :(
            }
            else
            {
                var thread = new Thread(() => Thread_FillTypesToList(typesList));
                thread.Start();
            }
            
            var prompt = ConsoleTextPrompt.ShowInConsoleRoot(visualSrc, new ConsoleTextPrompt.Data()
            {
                Title = "Types Search...",
                ResultCallback = (a) =>
                {
                    if (a == null) return true;
                    var match = typesList.Find(t => t.name == a);
                    if (match.name != null)
                    {
                        result(match.type);
                        return true;
                    }
                    return false;
                },
                AutoCompleteResultsCallback = (term, resultList) =>
                {
                    lock (typesList)
                    {
                        var count = 0;
                        var terms = Regex.Split(term, @"\s+");
                        foreach (var item in typesList)
                        {
                            var matched = true;
                            foreach (var subTerm in terms)
                            {
                                if (!string.IsNullOrEmpty(subTerm) && item.name.IndexOf(subTerm, StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    matched = false;
                                    break;
                                }
                            }
                            if (!matched) continue;
                            resultList.Add(item.name);
                            count++;
                            if (count >= 20)
                            {
                                return;
                            }
                        }
                    }
                }
            });
            ConsoleUIUtils.AutoRemoveAWhenBIsRemoved(prompt, visualSrc);
        }

        static void Thread_FillTypesToList(List<(string, Type)> resultList)
        {
            var allTypesList = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (Exception)
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Select(t => (t.FullName, t));
            lock (resultList)
            {
                resultList.AddRange(allTypesList);
            }
        }
        
        public static bool IsUnitySerializable(Type type)
        {
            if (type.IsPrimitive || type == typeof(string))
                return true;
            if (type.IsEnum)
                return true;
            if (type.IsValueType && (type.IsSerializable || IsNativeClass(type)))
                return true;
            if (type.IsClass && type.IsSerializable)
                return true;
            return typeof(UnityEngine.Object).IsAssignableFrom(type);
        }

        static Type _nativeClassAttribute;
        static bool _searchedNativeClass;
        static bool IsNativeClass(Type type)
        {
            if (_nativeClassAttribute == null && !_searchedNativeClass)
            {
                _searchedNativeClass = true;
                foreach (var attribute in typeof(Vector2).GetCustomAttributes())
                {
                    if (attribute?.GetType().FullName == "UnityEngine.NativeClassAttribute")
                    {
                        _nativeClassAttribute = attribute.GetType();
                    }
                }
            }
            return _nativeClassAttribute != null && type.IsDefined(_nativeClassAttribute);
        }

        static bool ShouldUseReadPropertyWrapper(Type type, PropertyInfo propertyInfo)
        {
            if (!AutoReadProperties)
            {
                return true;
            }
            if (typeof(Component).IsAssignableFrom(type))
            {
                var n = propertyInfo.Name;
                if (n.Length > 2)
                {
                    n = n[0].ToString().ToUpper() + n.Substring(1);
                }
                return type.GetProperty("shared" + n) != null;
            }
            return false;
        }

        public static VisualElement CreateField(string memberName, Type type, Func<object> getter, Action<object> setter = null)
        {
            return CreateField(CreateInfo(memberName, type, getter, setter));
        }

        public static FieldData CreateInfo(string memberName, Type type, Func<object> getter, Action<object> setter = null)
        {
            return new FieldData()
            {
                Name = memberName,
                Type = type,
                Getter = getter,
                Setter = setter
            };
        }
        
        static void RunMethod(VisualElement container, object obj, MethodInfo methodInfo, object[] paramObjs = null)
        {
            try
            {
                var result = methodInfo.Invoke(obj, paramObjs ?? Array.Empty<object>());
                if (methodInfo.ReturnType == typeof(void))
                {
                    ConsoleToasts.TryShow($"Method `{methodInfo.Name}` was called.");
                }
                else if (result != null && !result.GetType().IsValueType && result is not string)
                {
                    ConsoleToasts.TryShow($"Method call `{methodInfo.Name}` returned `{result}`\nClick action to inspect it...", () =>
                    {
                        FindInspectorAndGoToChild(container, result);
                    });
                }
                else
                {
                    ConsoleToasts.TryShow($"Method call `{methodInfo.Name}` returned `{result}`");
                }
            }
            catch (Exception err)
            {
                ConsoleToasts.TryShow($"Method call `{methodInfo.Name}` threw an exception:\n{err.Message}");
                Debug.LogWarning($"Method call `{methodInfo.Name}` threw an exception:\n{err}");
            }
        }

        public static bool IsNull(object value)
        {
            // for some reason `unity object == null` is false even if object is null.
            return value == null || value.Equals(null);
        }
        
        public class FieldsFoldOut : Foldout
        {
            readonly List<FieldData> _fields = new List<FieldData>();

            public Action OnChildrenDrawn;

            public FieldsFoldOut(string name)
            {
                value = false;
                text = name;
                viewDataKey = name;
                this.RegisterValueChangedCallback(OnValueChanged);
            }

            public void Add(FieldData fieldData)
            {
                if (value)
                {
                    var fieldElement = CreateField(fieldData);
                    if (fieldElement != null)
                    {
                        Add(fieldElement);
                    }
                }
                else
                {
                    _fields.Add(fieldData);
                }
            }

            public bool HasFields => _fields.Count > 0 || contentContainer.childCount > 0;

            void OnValueChanged(ChangeEvent<bool> evt)
            {
                if (value)
                {
                    foreach (var fieldData in _fields)
                    {
                        var fieldElement = CreateField(fieldData);
                        if (fieldElement != null)
                        {
                            Add(fieldElement);
                        }
                    }
                    _fields.Clear();
                    var cb = OnChildrenDrawn;
                    OnChildrenDrawn = null;
                    cb?.Invoke();
                }
            }
        }
    }
}
#endif