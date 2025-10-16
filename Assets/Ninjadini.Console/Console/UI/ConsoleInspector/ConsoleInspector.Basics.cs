#if !NJCONSOLE_DISABLE
using System;
using System.Collections;
using System.Reflection;
using Ninjadini.Console.Internal;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleInspector
    {
        public const int UpdateEveryMs = 50;
        
        public struct FieldData
        {
            public Type Type;
            public string Name;
            public Func<object> Getter;
            public Action<object> Setter;
            public int Depth;
            public bool IsProperty;
        }
        
        public static VisualElement CreateField(FieldData data)
        {
            if (data.Type == null || data.Getter == null)
            {
                return null;
            }
            if (data.IsProperty)
            {
                return CreatePropertyWrapper(data);
            }
            var customField = CustomFieldCreator?.Invoke((data.Name, data.Type, data.Getter, data.Setter));
            if (customField != null)
            {
                return customField;
            }
            
            var prevValue = ForcedUpdate;
            
            if (data.Type == typeof(bool))
            {
                var field = new Toggle(data.Name);
                field.AddToClassList("toggle-field");
                field.SetEnabled(data.Setter != null);
                field.RegisterValueChangedCallback(evt => ValueWriter(evt.newValue));
                field.style.alignSelf = Align.FlexStart;
                return SetupField(field, (v) => field.SetValueWithoutNotify((bool)v));
            }
            if (data.Type == typeof(short))
            {
                var field = new IntegerField(data.Name);
                field.isReadOnly = data.Setter == null;
                field.RegisterValueChangedCallback(evt => ValueWriter(Mathf.Clamp(evt.newValue, short.MinValue, short.MaxValue)));
#if UNITY_2022_3_OR_NEWER
                field.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
#endif
                return SetupField(field, (v) => field.SetValueWithoutNotify((short)v));
            }
            if (data.Type == typeof(int))
            {
                return SetupBasicField(new IntegerField(data.Name), isNumber: true);
            }
            if (data.Type == typeof(uint))
            {
                #if UNITY_2023_1_OR_NEWER
                return SetupBasicField(new UnsignedIntegerField(data.Name), isNumber: true);
                #else
                var field = new LongField(data.Name);
                field.isReadOnly = data.Setter == null;
                field.RegisterValueChangedCallback(evt => ValueWriter((uint)Math.Clamp(evt.newValue, uint.MinValue, uint.MaxValue)));
                return SetupField(field, (v) => field.SetValueWithoutNotify((long)v));
                #endif
            }
            if (data.Type == typeof(float))
            {
                return SetupBasicField(new FloatField(data.Name), isNumber: true);
            }
            if (data.Type == typeof(double))
            {
                return SetupBasicField(new DoubleField(data.Name), isNumber: true);
            }
            if (data.Type == typeof(long))
            {
                return SetupBasicField(new LongField(data.Name), isNumber: true);
            }
            if (data.Type == typeof(ulong))
            {
#if UNITY_2023_1_OR_NEWER
                return SetupBasicField(new UnsignedLongField(data.Name), isNumber: true);
#else
                var field = new LongField(data.Name);
                field.SetEnabled(data.Setter != null);
                field.RegisterValueChangedCallback(evt => ValueWriter((ulong)evt.newValue));
                return SetupField(field, (v) => field.SetValueWithoutNotify((long)v));
                // Sorry, this won't work fully past long value.
#endif
            }
            if (data.Type == typeof(Hash128))
            {
                return SetupBasicField(new Hash128Field(data.Name));
            }
            if (data.Type == typeof(string))
            {
                return SetupBasicField(new TextField(data.Name));
            }
#if UNITY_EDITOR
            // some of these works on device but it's quite busy + keyboard submission etc doesn't work the same way
            if (data.Type == typeof(Vector2))
            {
                return SetupBasicField(new Vector2Field(data.Name));
            }
            if (data.Type == typeof(Vector2Int))
            {
                return SetupBasicField(new Vector2IntField(data.Name));
            }
            if (data.Type == typeof(Vector3))
            {
                return SetupBasicField(new Vector3Field(data.Name));
            }
            if (data.Type == typeof(Vector3Int))
            {
                return SetupBasicField(new Vector3IntField(data.Name));
            }
            if (data.Type == typeof(Vector4))
            {
                return SetupBasicField(new Vector4Field(data.Name));
            }
            if (data.Type == typeof(Bounds))
            {
                return SetupBasicField(new BoundsField(data.Name));
            }
            if (data.Type == typeof(BoundsInt))
            {
                return SetupBasicField(new BoundsIntField(data.Name));
            }
            if (data.Type == typeof(Color))
            {
                return SetupBasicField(new UnityEditor.UIElements.ColorField(data.Name));
            }
            if (data.Type == typeof(Gradient))
            {
                return SetupBasicField(new UnityEditor.UIElements.GradientField(data.Name));
            }
            if (data.Type == typeof(AnimationCurve))
            {
                return SetupBasicField(new UnityEditor.UIElements.CurveField(data.Name));
            }
#endif
            if (data.Type.IsEnum)
            {
                if (data.Type.IsDefined(typeof(FlagsAttribute)))
                {
#if UNITY_EDITOR
                    return SetupBasicField(new UnityEditor.UIElements.EnumFlagsField(data.Name, (Enum)Activator.CreateInstance(data.Type)));
#endif
                }
                else
                {
                    var field = new EnumField((Enum)Activator.CreateInstance(data.Type))
                    {
                        label = data.Name
                    };
                    ConsoleUIUtils.FixDropdownFieldPopupSize(field, null);
                    return SetupBasicField(field);
                }
            }
            if (typeof(IList).IsAssignableFrom(data.Type))
            {
                var field = new ListField(data);
                return SetupField(field, (v) => field.SetValueWithoutNotify((IList)v));
            }
            if (typeof(IDictionary).IsAssignableFrom(data.Type))
            {
                var field = new DictionaryField(data);
                return SetupField(field, (v) => field.SetValueWithoutNotify((IDictionary)v));
            }
            if (data.Type.IsClass || (data.Type.IsValueType && !data.Type.IsPrimitive))
            {
                var field = new ChildObjectField(data);
                return SetupField(field, (v) => field.SetValueWithoutNotify(v));
            }
            return CreateFallback(data);

            VisualElement SetupBasicField<T>(BaseField<T> field, bool isNumber = false)
            {
                if (field is TextInputBaseField<T> txtField)
                {
#if UNITY_2022_3_OR_NEWER
                    if (isNumber)
                    {
                        txtField.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                    }
#endif
                    txtField.isReadOnly = data.Setter == null;
                    if (data.Setter == null)
                    {
                        field.AddToClassList("field-readonly");
                    }
                    ConsoleUIUtils.SetSubmissionCallback(txtField, (v) => ValueWriter(v));
                    return SetupField(field, (v) => field.SetValueWithoutNotify((T)v));
                }
                field.SetEnabled(data.Setter != null);
                field.RegisterValueChangedCallback(evt => ValueWriter(evt.newValue));
                return SetupField(field, (v) => field.SetValueWithoutNotify((T)v));
            }
            
            /*
            VisualElement SetupCompositeNumberField<TValueType, TField, TFieldValue>(BaseCompositeField<TValueType, TField, TFieldValue> field) where TField : TextValueField<TFieldValue>, new()
            {
                var visualElement = SetupBasicField(field, true);
#if UNITY_2022_3_OR_NEWER
                field.Query<TextInputBaseField<TFieldValue>>().ForEach(f =>
                {
                    f.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                });
#endif
                return visualElement;
            }*/

            VisualElement SetupField(VisualElement fieldElement, Action<object> fieldValueSetter)
            {
                IVisualElementScheduledItem updateSchedule = null;
                updateSchedule = fieldElement.schedule.Execute(Update).Every(UpdateEveryMs);
                Update();
                return fieldElement;

                void Update()
                {
                    try
                    {
                        if (data.Getter == null) return;
                        var valueNow = data.Getter();
                        if (!Equals(valueNow, prevValue))
                        {
                            prevValue = valueNow;
                            fieldValueSetter.Invoke(valueNow);
                        }
                    }
                    catch (Exception err)
                    {
                        updateSchedule?.Pause();
                        NjLogger.Warn("Failed to update `",data.Name,"`. Stopped updating that field now. Error: ", err);
                    }
                }
            }
            
            void ValueWriter(object value)
            {
                try
                {
                    prevValue = ForcedUpdate;
                    data.Setter?.Invoke(value);
                }
                catch (Exception err)
                {
                    NjLogger.Warn("Failed to set `",data.Name,"`. Error:", err);
                }
            }
        }

        static VisualElement CreatePropertyWrapper(FieldData data)
        {
            return new WrappedPropertyField(data);
        }

        class WrappedPropertyField : VisualElement
        {
            FieldData _data;
            
            public WrappedPropertyField(FieldData data)
            {
                _data = data;
                AddToClassList("horizontal");
                var lbl = new Label(data.Name);
                lbl.style.minWidth = 120;
                Add(lbl);
                var btn = new Button(Unwrap);
                btn.text = "Read Property";
                btn.tooltip = ConsoleUIStrings.InspectReadProp;
                Add(btn);
            }

            public void Unwrap()
            {
                RemoveFromClassList("horizontal");
                Clear();
                _data.IsProperty = false;
                var fieldElement = CreateField(_data);
                if (fieldElement != null)
                {
                    Add(fieldElement);
                }
            }
        }

        static VisualElement CreateFallback(FieldData data)
        {
            var textField = new TextField(data.Name);
            string strFallback;
            try
            {
                strFallback = data.Getter()?.ToString() ?? "null";
            }
            catch (Exception err)
            {
                strFallback = $"**Error: " + (err.InnerException?.Message ?? err.Message);
            }
            textField.value = strFallback;
            textField.SetEnabled(false);
            return textField;
        }

        static readonly object ForcedUpdate = new ();

        public static Func<(string fieldName, Type type, Func<object> getter, Action<object> setter), VisualElement> CustomFieldCreator;
    }
}
#endif