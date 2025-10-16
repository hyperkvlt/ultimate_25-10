#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleInspector : VisualElement, ConsoleInspector.IFieldController
    {
        public static void Show(VisualElement container, object value)
        {
            var element = new ConsoleInspector();
            element.AddToClassList("fullscreen");
            
            var underlay = new VisualElement();
            underlay.AddToClassList("underlay");
            underlay.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            element.Insert(0, underlay);
            
            container.Add(element);
            element.Inspect(value);
        }

        public static bool AutoReadProperties = true;
        static readonly StringBuilder TempStrBuilder = new StringBuilder();

        readonly Label _headerLbl;
        readonly ScrollView _valuesContainer;
        readonly Button _backButton;
        //readonly Button _searchBtn;
        //readonly TextField _searchField;
        readonly List<object> _stack = new ();

        public ConsoleInspector()
        {
            var header = new VisualElement();
            header.AddToClassList("inspector-header");
            Add(header);
            _backButton = new Button(OnBackButtonClicked);
            _backButton.AddToClassList("nav-btn");
            header.Add(_backButton);
            
            header.Add(_headerLbl = new Label());
            _headerLbl.style.flexGrow = 1;
            //_searchBtn = new Button(OnSearchBtnClicked);
            //header.Add(_searchBtn);

            if(Application.isEditor || (ConsoleSettings.Get()?.inPlayerCommandLine ?? false))
            {
                var commandLineBtn = new Button(OnCommandLineClicked);
                commandLineBtn.AddToClassList("monoFont");
                commandLineBtn.text = "⌨";
                header.Add(commandLineBtn);
            }
            if (DetailInspectorEnabled)
            {
                var searchClassesBtn = new Button(OnSearchClassesClicked);
                searchClassesBtn.text = "Types";
                header.Add(searchClassesBtn);
            }

            var autoReadProperties = new Toggle("Auto Properties");
            autoReadProperties.value = AutoReadProperties;
            autoReadProperties.tooltip = "<b>Auto read properties</b>\n"+ConsoleUIStrings.InspectReadProp;
            autoReadProperties.RegisterValueChangedCallback(OnAutoReadPropChanged);
            header.Add(autoReadProperties);
            var closeBtn = new Button(Close)
            {
                text = "X"
            };
            closeBtn.AddToClassList("red-btn");
            closeBtn.AddToClassList("inspector-closeBtn");
            header.Add(closeBtn);

            //_searchField = new TextField("Search");
            //_searchField.style.display = DisplayStyle.None;
            //Add(_searchField);
            _valuesContainer = new ScrollView();
            _valuesContainer.mode = ScrollViewMode.VerticalAndHorizontal;
            Add(_valuesContainer);
            //UpdateSearchBtn();
        }

        void OnSearchClassesClicked()
        {
            ShowClassSearchPrompt( this, (type) =>
            {
                if (type != null)
                {
                    Inspect(type);
                }
            });
        }

        void OnCommandLineClicked()
        {
            var obj = _stack[^1];
            if (obj == null) return;
            var window = ConsoleUIUtils.FindParent<ConsoleWindow>(this) ?? ConsoleContext.TryGetFocusedContext()?.Window;
            var clElement =  window?.OpenAndFocusOnCommandLine();
            if (clElement != null)
            {
                clElement.Runner?.SetScope(obj);
                var storage = window.Modules.GetOrCreateModule<ConsoleObjReferenceStorage>();
                storage.StoreAsLastResult(obj);
            }
            Close();
        }

        void OnAutoReadPropChanged(ChangeEvent<bool> evt)
        {
            AutoReadProperties = evt.newValue;
            DrawValues(_stack[^1]);
        }

        public void Inspect(object value)
        {
            _stack.Clear();
            PushInspect(value);
        }

        void UpdateBackButton()
        {
            var lastItem = _stack.Count > 1 ? _stack[^2] : null;
            if (lastItem != null)
            {
                _backButton.text = "\u25c0 "+LoggerUtils.GetSingleShortenedLine(lastItem.GetType().Name, 12);
                _backButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                _backButton.style.display = DisplayStyle.None;
            }
        }

        /// Keep the previous object as the back button option and inspect this new object.
        public void PushInspect(object value)
        {
            _stack.Add(value);
            UpdateBackButton();
            if (IsNull(value))
            {
                _headerLbl.text = "object is null";
                _valuesContainer.Clear();
                return;
            }
            _headerLbl.text = GetHeaderText(value);
            DrawValues(value);
        }

        void DrawValues(object value)
        {
            _valuesContainer.Clear();
            if (value is GameObject go)
            {
                AddForGameObject(_valuesContainer, go);
            }
            else
            {
                AddMemberFields(_valuesContainer, value);
            }
        }

        public static string GetHeaderText(object obj)
        {
            Type type;
            if (obj is Type t)
            {
                type = t;
                obj = null;
            }
            else type = obj.GetType();
            string nameAndId;
            if (obj is UnityEngine.Object unityObj)
            {
                var hashCode = obj.GetHashCode();
                var instanceId = unityObj.GetInstanceID();
                var nam = $"<b>{unityObj.name}</b> {type.Name} ";
                if (hashCode != instanceId)
                {
                    nameAndId = $"{nam} id#{instanceId} #{hashCode}";
                }
                else
                {
                    nameAndId = $"{nam} #{hashCode}";
                }
            }
            else if(obj != null)
            {
                nameAndId = $"<b>{type.FullName}</b> #{obj.GetHashCode()}";
            }
            else nameAndId = $"<b>{type.FullName}</b>";

            var interfaces = type.GetInterfaces()
                .Select(i => i.Name)
                .ToArray();
            var interfaceList = interfaces.Length > 0 ? $": {string.Join(", ", interfaces)}" : "";

            return nameAndId + interfaceList;
        }

        void OnBackButtonClicked()
        {
            if (_stack.Count > 1)
            {
                var lastItem = _stack[^2];
                _stack.RemoveRange(_stack.Count - 2, 2);
                PushInspect(lastItem);
            }
        }
/*
        void OnSearchBtnClicked()
        {
            if (_searchField.style.display == DisplayStyle.None)
            {
                _searchField.style.display = DisplayStyle.Flex;
                _searchField.Focus();
            }
            else
            {
                _searchField.style.display = DisplayStyle.None;
            }
            UpdateSearchBtn();
        }

        void UpdateSearchBtn()
        {
            if (_searchField.style.display == DisplayStyle.None)
            {
                _searchBtn.text = "\ud83d\udd0d";
            }
            else
            {
                _searchBtn.text = "\ud83d\udd0d X";
            }
        }*/
        
        static readonly List<Component> tempComps = new List<Component>();

        public static void AddForGameObject(VisualElement container, GameObject gameObject)
        {
            var field = CreateField(new FieldData()
            {
                Name = "Active",
                Type = typeof(bool),
                Getter = () => gameObject?.activeSelf,
                Setter = (v) =>
                {
                    if (gameObject)
                    {
                        gameObject.SetActive((bool)v);
                    }
                }
            });
            if(field != null) container.Add(field);
            
            gameObject.GetComponents(tempComps);
            foreach (var component in tempComps)
            {
                if (!component)
                {
                    continue;
                }
                var foldOut = new FieldsFoldOut(component.GetType().Name);
                if (DetailInspectorEnabled)
                {
                    var gotoComp = new Button(() =>
                    {
                        FindInspectorAndGoToChild(container, component);
                    })
                    {
                        text = " \u25b6"
                    };
                    gotoComp.style.width = 50;
                    gotoComp.AddToClassList("foldout-header-child");
                    gotoComp.AddToClassList("nav-btn");
                    gotoComp.AddToClassList("monoFont");
                    foldOut.hierarchy.Add(gotoComp);
                }
                var custom = GetCustomDrawer(component.GetType());
                if (custom != null)
                {
                    custom(component, foldOut);
                }
                else
                {
                    AddForComponent(foldOut, component);
                }
                container.Add(foldOut);
            }
            tempComps.Clear();
            
            container.Add(new VisualElement()
            {
                style = { height = 10 }
            });
            // couldn't use reflection here due to some platforms stripping some stuff.
            field = CreateField(nameof(gameObject.name), typeof(string), () => gameObject.name, (v) => gameObject.name = (string)v);
            if(field != null) container.Add(field);
            field = CreateField(nameof(gameObject.tag), typeof(string), () => gameObject.tag, (v) => gameObject.tag = (string)v);
            if(field != null) container.Add(field);
            field = CreateField(nameof(gameObject.layer), typeof(int), () => gameObject.layer, (v) => gameObject.layer = (int)v);
            if(field != null) container.Add(field);
            field = CreateField(nameof(gameObject.isStatic), typeof(bool), () => gameObject.isStatic, (v) => gameObject.isStatic = (bool)v);
            if(field != null) container.Add(field);
            if (Application.isEditor)
            {
                field = CreateField(nameof(gameObject.hideFlags), typeof(HideFlags), () => gameObject.hideFlags, (v) => gameObject.hideFlags = (HideFlags)v);
                if(field != null) container.Add(field);
            }
        }

        static bool DetailInspectorEnabled => Application.isEditor || (ConsoleSettings.Get()?.inPlayerObjectInspector ?? false);

        public static void AddForComponent(FieldsFoldOut fieldsFoldOut, Component component)
        {
            AddForComponent(component, fieldsFoldOut.Add);
        }

        public static void AddForComponent(Component component, Action<FieldData> addCall)
        {
            var type = component.GetType();
            if (component is Behaviour behaviour)
            {
                addCall(CreateInfo(nameof(behaviour.enabled), typeof(bool), () => behaviour.enabled, (v) => behaviour.enabled = (bool)v));
            }
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsStatic || field.IsInitOnly)
                {
                    continue;
                }
                if (!field.IsPublic && !field.IsDefined(typeof(SerializeField)))
                {
                    continue;
                }
                if (!IsUnitySerializable(field.FieldType))
                {
                    continue;
                }
                var fieldData = new FieldData()
                {
                    Name = field.Name,
                    Type = field.FieldType,
                    Getter = () => field.GetValue(component),
                    Setter = (v) => field.SetValue(component, v),
                };
                addCall(fieldData);
            }
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite)
                    continue;
                if (prop.GetIndexParameters().Length > 0)
                    continue;
                if (!IsUnitySerializable(prop.PropertyType))
                    continue;
                var baseType = prop.DeclaringType;
                if (baseType == typeof(UnityEngine.Object) && (prop.Name == nameof(Component.name) || prop.Name == nameof(Component.hideFlags)))
                    continue;
                if (baseType == typeof(Component) && prop.Name == nameof(Component.tag))
                    continue;
                if (baseType == typeof(Behaviour) && prop.Name == nameof(Behaviour.enabled))
                    continue;
                // editor only, and is there any point to show that in inspector?
                //if (baseType == typeof(MonoBehaviour) && prop.Name is nameof(MonoBehaviour.runInEditMode) or nameof(MonoBehaviour.useGUILayout))
                //{
                //    continue;
                //}
                var fieldData = new FieldData()
                {
                    Name = prop.Name,
                    Type = prop.PropertyType,
                    Getter = () => prop.GetValue(component),
                    Setter = (v) => prop.SetValue(component, v),
                    IsProperty = ShouldUseReadPropertyWrapper(type, prop)
                };
                addCall(fieldData);
            }
        }

        public static void AddMemberFields(VisualElement container, object obj)
        {
            if (!TypeOrObjectSplit(ref obj, out var type))
            {
                return;
            }
            var custom = GetCustomDrawer(type);
            if (obj is Component component)
            {
                var foldOut = new FieldsFoldOut("Inspector view");
                if (custom != null)
                {
                    custom(obj, foldOut);
                }
                else
                {
                    AddForComponent(foldOut, component);
                }

                foldOut.Add(CreateInfo(nameof(component.gameObject), typeof(GameObject), () => component.gameObject));
                if (component is not Transform)
                {
                    foldOut.Add(CreateInfo(nameof(component.transform), typeof(Transform), () => component.transform));
                }
                container.Add(foldOut);
            }
            else if (custom != null)
            {
                var foldOut = new FieldsFoldOut("Inspector view");
                custom(obj, foldOut);
                container.Add(foldOut);
            }
            
            var publicFields = new FieldsFoldOut(obj == null ? "Public Static Fields" : "Public Fields");
            publicFields.value = true;
            var privateFields = new FieldsFoldOut(obj == null ? "Private Static Fields" : "Private Fields");
            var publicProperties = new FieldsFoldOut(obj == null ? "Public Static Properties" : "Public Properties");
            publicProperties.value = true;
            var privateProperties = new FieldsFoldOut(obj == null ? "Private Static Properties" : "Private Properties");
            
            var obsoleteMembers = new FieldsFoldOut(obj == null ? "Static Obsolete Members" : "Obsolete Members");
            var instanceOrStatic = obj == null ? BindingFlags.Static : BindingFlags.Instance;

            AddFields(type, obj, BindingFlags.Public | instanceOrStatic, publicFields, obsoleteMembers);
            AddFields(type, obj, BindingFlags.NonPublic | instanceOrStatic, privateFields, obsoleteMembers);
            AddProperties(type, obj, BindingFlags.Public | instanceOrStatic, publicProperties, obsoleteMembers);
            AddProperties(type, obj, BindingFlags.NonPublic | instanceOrStatic, privateProperties, obsoleteMembers);
            
            if (publicFields.HasFields)
            {
                container.Add(publicFields);
            }
            if (privateFields.HasFields)
            {
                container.Add(privateFields);
            }
            if (publicProperties.HasFields)
            {
                container.Add(publicProperties);
            }
            if (privateProperties.HasFields)
            {
                container.Add(privateProperties);
            }
            if (obsoleteMembers.HasFields)
            {
                container.Add(obsoleteMembers);
            }
            AddMethods(container, obj ?? type);

            if (obj != null && type
                    .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Any(m => m is PropertyInfo or FieldInfo or MethodInfo))
            {
                var btn = new Button(() =>
                {
                    FindInspectorAndGoToChild(container, obj.GetType());
                });
                btn.text = "View static members >";
                btn.style.width = 150;
                container.Add(btn);
            }
        }

        static void AddFields(Type type, object obj, BindingFlags flags, FieldsFoldOut targetFoldOut, FieldsFoldOut obsoleteMembers = null)
        {
            var hierarchyType = type;
            foreach (var fieldInfo in type.GetFields(flags))
            {
                if (fieldInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true)
                    && fieldInfo.Name.Contains("k__BackingField"))
                {
                    continue;
                }
                if (fieldInfo.IsInitOnly)
                {
                    continue;
                }
                var fieldData = new FieldData()
                {
                    Name = fieldInfo.Name,
                    Type = fieldInfo.FieldType,
                    Getter = () => fieldInfo.GetValue(obj),
                    Setter = (v) => fieldInfo.SetValue(obj, v)
                };
                if (fieldInfo.IsDefined(typeof(ObsoleteAttribute)))
                {
                    if (obsoleteMembers != null)
                    {
                        obsoleteMembers.Add(fieldData);
                    }
                }
                else
                {
                    if (hierarchyType != fieldInfo.DeclaringType)
                    {
                        hierarchyType = fieldInfo.DeclaringType;
                        AddTypeLine(targetFoldOut, hierarchyType);
                    }
                    targetFoldOut.Add(fieldData);
                }
            }
        }

        static void AddProperties(Type type, object obj, BindingFlags flags, FieldsFoldOut targetFoldOut, FieldsFoldOut obsoleteMembers = null)
        {
            var hierarchyType = type;
            foreach (var propertyInfo in type.GetProperties(flags))
            {
                if (!propertyInfo.CanRead || propertyInfo.GetMethod == null)
                {
                    continue;
                }
                var fieldData = new FieldData()
                {
                    Name = propertyInfo.Name,
                    Type = propertyInfo.PropertyType,
                    Getter = () => propertyInfo.GetValue(obj),
                    Setter = propertyInfo.CanWrite ? (v) => propertyInfo.SetValue(obj, v) : null,
                    IsProperty = ShouldUseReadPropertyWrapper(type, propertyInfo)
                };
                if (propertyInfo.IsDefined(typeof(ObsoleteAttribute)))
                {
                    if (obsoleteMembers != null)
                    {
                        obsoleteMembers.Add(fieldData);
                    }
                }
                else
                {
                    if (hierarchyType != propertyInfo.DeclaringType)
                    {
                        hierarchyType = propertyInfo.DeclaringType;
                        AddTypeLine(targetFoldOut, hierarchyType);
                    }
                    targetFoldOut.Add(fieldData);
                }
            }
        }

        static void AddTypeLine(VisualElement container, Type type)
        {
            var lbl = new Label(type.Name);
            lbl.AddToClassList("inspector-base-type");
            container.Add(lbl);
        }

        static void AddMethods(VisualElement container, object obj)
        {
            TypeOrObjectSplit(ref obj, out var type);
            var instanceOrStatic = obj == null ? BindingFlags.Static : BindingFlags.Instance;

            var publicMethods = new Foldout();
            publicMethods.text = obj == null ? "Public Static Methods" : "Public Methods";
            publicMethods.value = false;
            var privateMethods = new Foldout();
            privateMethods.text = obj == null ? "Private Static Methods" : "Private Methods";
            privateMethods.value = false;
            
            var sb = TempStrBuilder;
            var hierarchyType = type;
            foreach (var methodInfoLoop in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | instanceOrStatic))
            {
                var methodInfo = methodInfoLoop;
                var paramInfos = methodInfo.GetParameters();
                VisualElement element = null;
                
                sb.Clear();
                sb.Append(methodInfo.Name).Append('(');;
                for (var i = 0; i < paramInfos.Length; i++)
                {
                    var param = paramInfos[i];
                    sb.Append(param.ParameterType.Name);
                    if (i < paramInfos.Length - 1) sb.Append(", ");
                }
                sb.Append(')');
                if (methodInfo.ReturnType != typeof(void))
                {
                    sb.Append(": ").Append(methodInfo.ReturnType.Name);
                }
                
                var text = sb.ToString();
                if (paramInfos.Length > 0)
                {
                    var foldOut = new FieldsFoldOut(text);
                    foldOut.value = false;
                    var paramObjs = new object[paramInfos.Length];
                    for (var indexL = 0; indexL < paramInfos.Length; indexL++)
                    {
                        var index = indexL;
                        var parameterInfo = paramInfos[index];
                        paramObjs[index] = parameterInfo.ParameterType is { IsValueType: true, ContainsGenericParameters: false }
                            ? Activator.CreateInstance(parameterInfo.ParameterType)
                            : null;
                        var fieldData = new FieldData()
                        {
                            Name = parameterInfo.Name,
                            Type = parameterInfo.ParameterType,
                            Getter = () => paramObjs[index],
                            Setter = (v) => paramObjs[index] = v
                        };
                        foldOut.Add(fieldData);
                    }
                    foldOut.OnChildrenDrawn = () =>
                    {
                        foldOut.Add(new Button(() =>
                        {
                            RunMethod(container, obj, methodInfo, paramObjs);
                        })
                        {
                            text = "Call"
                        });
                    };
                    element = foldOut;
                }
                else
                {
                    element = new Button(() => RunMethod(container, obj, methodInfo))
                    {
                        text = text,
                        style =
                        {
                            unityTextAlign = TextAnchor.MiddleLeft,
                            flexGrow = 0,
                            minWidth = 200
                        }
                    };
                    element.AddToClassList("unity-base-field");
                }
                
                Foldout target;
                if (methodInfo.IsPublic) target = publicMethods;
                else target = privateMethods; 
                if (hierarchyType != methodInfo.DeclaringType)
                {
                    hierarchyType = methodInfo.DeclaringType;
                    AddTypeLine(target, hierarchyType);
                }
                target.Add(element);
            }

            if (publicMethods.contentContainer.childCount > 0)
            {
                container.Add(publicMethods);
            }
            if (privateMethods.contentContainer.childCount > 0)
            {
                container.Add(privateMethods);
            }
        }

        static bool TypeOrObjectSplit(ref object obj, out Type type)
        {
            if (IsNull(obj))
            {
                type = null;
                return false;
            }
            if (obj is Type t)
            {
                type = t;
                obj = null;
            }
            else type = obj.GetType();
            return true;
        }

        void Close()
        {
            RemoveFromHierarchy();
        }


        void IFieldController.GoToChild(object obj)
        {
            PushInspect(obj);
        }

        public interface IFieldController
        {
            void GoToChild(object obj);
        }

        static void FindInspectorAndGoToChild(VisualElement container, object obj)
        {
            ConsoleUIUtils.FindParent<IFieldController>(container)?.GoToChild(obj);
        }
    }
}
#endif