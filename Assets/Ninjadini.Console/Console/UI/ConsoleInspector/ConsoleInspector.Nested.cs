#if !NJCONSOLE_DISABLE
using System;
using System.Reflection;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleInspector 
    {
        public class ChildObjectField : Foldout
        {
            FieldData _fieldData;
            object _value;
            VisualElement _membersHolder;
            Button _gotoChildBtn;
            
            public Action<object> ValueChanged;
            
            public ChildObjectField(FieldData fieldData)
            {
                _fieldData = fieldData;
                value = false;
                AddToClassList("inspector-foldout");

                _membersHolder = new VisualElement();
                Add(_membersHolder);

                //value = fieldData.Depth < 2 && fieldData.Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length <= 6;
                //value = fieldData.Type.IsValueType;
                //value = fieldData.Depth < 2 && fieldData.Type.IsValueType;
                this.RegisterValueChangedCallback(OnFoldoutValueChange);

                if (DetailInspectorEnabled)
                {
                    _gotoChildBtn = new Button(GotoChildClicked)
                    {
                        text = "\u25b6"
                    };
                    _gotoChildBtn.style.width = 50;
                    _gotoChildBtn.AddToClassList("foldout-header-child");
                    _gotoChildBtn.AddToClassList("nav-btn");
                    _gotoChildBtn.AddToClassList("monoFont");
                    hierarchy.Add(_gotoChildBtn);
                    _gotoChildBtn.style.display = DisplayStyle.None;
                }
                UpdateFieldName();
            }

            void GotoChildClicked()
            {
                if (_value != null)
                {
                    FindInspectorAndGoToChild(this, _value);
                }
            }

            void OnFoldoutValueChange(ChangeEvent<bool> evt)
            {
                AddMemberFieldsIfRequired();
            }


            public void SetValueWithoutNotify(object newObj)
            {
                _value = newObj;
                UpdateFieldName();
                if (_gotoChildBtn != null)
                {
                    _gotoChildBtn.style.display = !_fieldData.Type.IsValueType && !IsNull(newObj)? DisplayStyle.Flex : DisplayStyle.None;
                }
                _membersHolder.Clear();
                if (value)
                {
                    schedule.Execute(AddMemberFieldsIfRequired);
                }
            }

            void AddMemberFieldsIfRequired()
            {
                if (IsNull(_value) || _membersHolder.childCount != 0)
                {
                    return;
                }

                if (_value is GameObject gameObject)
                {
                    var fieldElement = CreateField(new FieldData()
                    {
                        Name = "Active",
                        Type = typeof(bool),
                        Getter = () => gameObject && gameObject.activeSelf,
                        Setter = (v) =>
                        {
                            if (gameObject)
                            {
                                gameObject.SetActive((bool)v);
                            }
                        }
                    });
                    _membersHolder.Add(fieldElement);
                    gameObject.GetComponents(tempComps);
                    foreach (var component in tempComps)
                    {
                        if (!component)
                        {
                            continue;
                        }
                        var btn = new Button(() =>
                        {
                            FindInspectorAndGoToChild(this, component);
                        })
                        {
                            text = component.GetType().Name +" >"
                        };
                        btn.style.alignSelf = Align.FlexStart;
                        _membersHolder.Add(btn);
                    }
                    tempComps.Clear();
                }
                else if (_value is Component component)
                {
                    AddForComponent(component, (fieldData) =>
                    {
                        var fieldElement = CreateField(fieldData);
                        if (fieldElement != null)
                        {
                            _membersHolder.Add(fieldElement);
                        }
                    });
                }
                else
                {
                    AddFields();
                }
            }

            void AddFields()
            {
                var depth = _fieldData.Depth + 1;
                var type = _value.GetType();
                foreach (var fieldInfo in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    Action<object> setter = null;
                    if (_fieldData.Setter != null)
                    {
                        setter = (v) => fieldInfo.SetValue(_value, v);
                        if (type.IsValueType)
                        {
                            var fieldSetter = setter;
                            setter = (v) =>
                            {
                                fieldSetter.Invoke(v);
                                _fieldData.Setter?.Invoke(_value);
                            };
                        }
                    }
                    var fieldData = new FieldData()
                    {
                        Name = fieldInfo.Name,
                        Type = fieldInfo.FieldType,
                        Getter = () => fieldInfo.GetValue(_value),
                        Setter = setter,
                        Depth = depth
                    };
                    var fieldElement = CreateField(fieldData);
                    if (fieldElement != null)
                    {
                        _membersHolder.Add(fieldElement);
                    }
                }
            }

            void UpdateFieldName()
            {
                if (IsNull(_value))
                {
                    text = $"<b>{_fieldData.Name}</b> (null)";
                    return;
                }
                if (HasUserDefinedToString(_fieldData.Type))
                {
                    text = $"<b>{_fieldData.Name}</b> \t({LoggerUtils.GetSingleShortenedLine(_value.ToString(), 64)})";
                }
                else if(_value is Object obj)
                {
                    text = $"<b>{_fieldData.Name}</b> \t{obj.GetType().Name} \"{obj.name}\"";
                }
                else
                {
                    text = $"<b>{_fieldData.Name}</b>";
                }
            }
        }

        static bool HasUserDefinedToString(Type type)
        {
            try
            {
                var method = type.GetMethod(nameof(ToString), BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
                if (method == null)
                {
                    return false;
                }
                if (method.DeclaringType != type)
                {
                    return false;
                }
                var baseToString = typeof(ValueType).GetMethod(nameof(ToString), BindingFlags.Instance | BindingFlags.Public);
                return method.GetBaseDefinition() != baseToString;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
#endif