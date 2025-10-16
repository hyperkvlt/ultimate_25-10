#if !NJCONSOLE_DISABLE
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleInspector 
    {
        public class DictionaryField : Foldout
        {
            FieldData _fieldData;
            readonly VisualElement _itemsContainer;
            readonly Dictionary<object, VisualElement> _drawnElements = new ();

            IDictionary _value;
            
            public DictionaryField(FieldData fieldData)
            {
                if (!typeof(IDictionary).IsAssignableFrom(fieldData.Type))
                {
                    throw new ArgumentException($"{fieldData.Type} is not an IList");
                }
                AddToClassList("inspector-foldout");
                _fieldData = fieldData;
                _itemsContainer = new VisualElement();
                Add(_itemsContainer);
                schedule.Execute(Update).Every(UpdateEveryMs);
            }

            public void SetValueWithoutNotify(IDictionary newObj)
            {
                _value = newObj;
                Update();
            }

            void Update()
            {
                UpdateFieldName();
                if (_value != null && value)
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
                    text = $"<b>{_fieldData.Name}</b> {{{count}}}";
                }
                else
                {
                    text = $"<b>{_fieldData.Name}</b> (null)";
                }
            }

            void UpdateContent()
            {
                foreach (DictionaryEntry entry in _value)
                {
                    var key = entry.Key;
                    if (_drawnElements.ContainsKey(key))
                    {
                        continue;
                    }

                    if (entry.Value == null)
                    {
                        var nullElem = new Label(entry.Key + " (null)");
                        _drawnElements[key] = nullElem;
                        _itemsContainer.Add(nullElem);
                        // TODO this wont update once its drawn
                        continue;
                    }

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    
                    var element = CreateField(
                        new FieldData()
                        {
                            Name = entry.Key.ToString(),
                            Type = entry.Value.GetType(),
                            Getter = (() => _value != null && _value.Contains(key) ? _value[key] : null),
                            Setter = o =>
                            {
                                if (_value != null && _value.Contains(key))
                                {
                                    _value[key] = o;
                                }
                            },
                            Depth = _fieldData.Depth + 1
                        });
                    element.style.flexGrow = 1;
                    _drawnElements[key] = element;
                    _itemsContainer.Add(element);
                }

                if (_value.Count != _drawnElements.Count)
                {
                    foreach (var key in _drawnElements.Keys.ToArray())
                    {
                        if (_value.Contains(key)) continue;
                        _drawnElements[key]?.RemoveFromHierarchy();
                        _drawnElements.Remove(key);
                    }
                }
            }
        }
    }
}
#endif