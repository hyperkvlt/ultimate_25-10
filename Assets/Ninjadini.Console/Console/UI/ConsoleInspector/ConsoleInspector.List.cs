#if !NJCONSOLE_DISABLE
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleInspector 
    {
        public class ListField : Foldout
        {
            FieldData _fieldData;
            readonly Type _elementType;
            readonly VisualElement _itemsContainer;
            readonly List<VisualElement> _drawnElements = new ();

            IList _value;
            
            public ListField(FieldData fieldData)
            {
                if (!typeof(IList).IsAssignableFrom(fieldData.Type))
                {
                    throw new ArgumentException($"{fieldData.Type} is not an IList");
                }
                AddToClassList("inspector-foldout");
                _fieldData = fieldData;
                if (fieldData.Type.IsArray)
                {
                    _elementType = fieldData.Type.GetElementType();
                }
                else if (fieldData.Type.IsGenericType)
                {
                    var genericArgs = fieldData.Type.GetGenericArguments();
                    if (genericArgs.Length == 1)
                    {
                        _elementType = genericArgs[0];
                    }
                }

                if (_elementType == null)
                {
                    return;
                }
                _itemsContainer = new VisualElement();
                Add(_itemsContainer);

                if (!_fieldData.Type.IsArray || _fieldData.Setter != null)
                {
                    var horizontal = new VisualElement();
                    horizontal.style.flexDirection = FlexDirection.Row;
                    Add(horizontal);
                    AddAddRemoveBtns(horizontal, -1);
                }
                schedule.Execute(Update).Every(UpdateEveryMs);
            }

            void AddAddRemoveBtns(VisualElement container, int forIndex)
            {
                var removeBtn = new Button(() => RemoveClicked(forIndex))
                {
                    text = "-"
                };
                var addBtn = new Button(() => AddClicked(forIndex))
                {
                    text = "+"
                };
                container.Add(removeBtn);
                container.Add(addBtn);
            }

            public void SetValueWithoutNotify(IList newObj)
            {
                _value = newObj;
                Update();
            }

            void Update()
            {
                UpdateFieldName();
                if (value)
                {
                    UpdateContent();
                }
            }


            int _drawnCount = -2;
            void UpdateFieldName()
            {
                var count = _value?.Count ?? -1;
                if (count == _drawnCount) return;
                _drawnCount = count;
                if (count >= 0)
                {
                    text = $"<b>{_fieldData.Name}</b> [{count}]";
                }
                else
                {
                    text = $"<b>{_fieldData.Name}</b> (null)";
                }
            }

            void UpdateContent()
            {
                var count = _value?.Count ?? 0;
                while (_drawnElements.Count < count)
                {
                    var index = _drawnElements.Count;
                    var element = CreateField(
                        new FieldData()
                        {
                            Name = index.ToString(),
                            Type = _elementType,
                            Getter = (() =>
                            {
                                if (_value != null && index < _value.Count)
                                {
                                    return _value[index];
                                }

                                return null;
                            }),
                            Setter = o =>
                            {
                                if (_value != null && index < _value.Count)
                                {
                                    _value[index] = o;
                                }
                            },
                            Depth = _fieldData.Depth + 1
                        });
                    var horizontal = new VisualElement();
                    horizontal.style.flexDirection = FlexDirection.Row;
                    element.style.flexGrow = 1;
                    horizontal.Add(element);
                    if (!_fieldData.Type.IsArray || _fieldData.Setter != null)
                    {
                        AddAddRemoveBtns(horizontal, index);
                    }

                    _drawnElements.Add(horizontal);
                    _itemsContainer.Add(horizontal);
                }
                while (_drawnElements.Count > count)
                {
                    var item = _drawnElements[^1];
                    item.RemoveFromHierarchy();
                    _drawnElements.RemoveAt(_drawnElements.Count - 1);
                }
            }

            void RemoveClicked(int index)
            {
                if (IsNull(_value))
                {
                    return;
                }
                if (_value.Count > 0)
                {
                    RemoveAtIndex(index);
                }
                else
                {
                    _value = null;
                }
                Update();
                _fieldData.Setter?.Invoke(_value);
            }

            void AddClicked(int index)
            {
                if (IsNull(_value))
                {
                    if (_fieldData.Setter == null)
                    {
                        return;
                    }
                    if (_fieldData.Type.IsArray)
                    {
                        _value = (IList)Activator.CreateInstance(_fieldData.Type, 0);
                    }
                    else
                    {
                        _value = (IList)Activator.CreateInstance(_fieldData.Type);
                    }
                }
                else
                {
                    AddAtIndex(index);
                    Update();
                }
                _fieldData.Setter?.Invoke(_value);
            }

            void RemoveAtIndex(int index)
            {
                if (index < 0) index = _value.Count - 1;
                if (_value is Array array)
                {
                    if (_fieldData.Setter == null)
                    {
                        return;
                    }
                    var length = _value.Count;
                    var newArray = Array.CreateInstance(_elementType, length - 1);
                    Array.Copy(array, 0, newArray, 0, index);
                    Array.Copy(array, index + 1, newArray, index, length - index - 1);
                    _value = newArray;
                }
                else if (!_value.IsFixedSize)
                {
                    _value.RemoveAt(index);
                }
            }

            void AddAtIndex(int index)
            {
                if (index < 0) index = _value.Count;
                var newElement = _elementType.IsValueType ? Activator.CreateInstance(_elementType) : null;
                if (_value is Array array)
                {
                    if (_fieldData.Setter == null)
                    {
                        return;
                    }
                    var length = _value.Count;
                    var newArray = Array.CreateInstance(_elementType, length + 1);
                    Array.Copy(array, 0, newArray, 0, index);
                    newArray.SetValue(newElement, index);
                    Array.Copy(array, index, newArray, index + 1, length - index);
                    _value = newArray;
                }
                else if (!_value.IsFixedSize)
                {
                    _value.Insert(index, newElement);
                }
            }
        }
    }
}
#endif