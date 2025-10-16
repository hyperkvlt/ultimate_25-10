using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ninjadini.Logger
{
    /// <summary>
    /// Wrapper to allow converting primitive values into logs without allocating.<br/>
    /// You should never need to deal with this type while using NjLogger or NjConsole.<br/>
    /// It will do implicit type conversion for most types.<br/>
    /// If you are trying to log an object, you may want to call obj.AsLogRef() or obj.AsString()<br/>
    /// </summary>
    public struct StrValue
    {
        public ValueType Type;
        public long Value;
        public object Ref;

        const string DateTimeLocal = "l";
        const string DateTimeUtc = "u";
        const string HexChars = "0123456789ABCDEF";
        
        public static implicit operator StrValue(bool value) => new StrValue()
        {
            Type = ValueType.Bool,
            Value = value ? 1 : 0
        };
        
        public static implicit operator StrValue(int value) => new StrValue()
        {
            Type = ValueType.Int,
            Value = value
        };
            
        public static implicit operator StrValue(uint value) => new StrValue()
        {
            Type = ValueType.Uint,
            Value = value
        };
            
        public static implicit operator StrValue(long value) => new StrValue()
        {
            Type = ValueType.Long,
            Value = value
        };
            
        public static implicit operator StrValue(ulong value) => new StrValue()
        {
            Type = ValueType.ULong,
            Value = (long)value
        };
            
        public static implicit operator StrValue(float value) => new StrValue()
        {
            Type = ValueType.Float,
            Value = BitConverter.SingleToInt32Bits(value)
        };
            
        public static implicit operator StrValue(double value) => new StrValue()
        {
            Type = ValueType.Double,
            Value = BitConverter.DoubleToInt64Bits(value)
        };
            
        public static implicit operator StrValue(DateTime value) => new StrValue()
        {
            Type = ValueType.DateTime,
            Value = value.Ticks,
            Ref = value.Kind switch
            {
                DateTimeKind.Local => DateTimeLocal,
                DateTimeKind.Utc => DateTimeUtc,
                _ => string.Empty
            }
        };
            
        public static implicit operator StrValue(TimeSpan value) => new StrValue()
        {
            Type = ValueType.TimeSpan,
            Value = value.Ticks
        };

        public static implicit operator StrValue(string value) => new StrValue()
        {
            Type = ValueType.String,
            Ref = value
        };
            
        public static implicit operator StrValue(Exception value) => new StrValue()
        {
            Type = ValueType.Object,
            Ref = value
        };
        
#if UNITY_2022_1_OR_NEWER
        public static implicit operator StrValue(UnityEngine.Object value) => new StrValue()
        {
            Type = ValueType.Object,
            Ref = value
        };

        public static implicit operator StrValue(UnityEngine.Color value)
        {
            UnityEngine.Color32 c = value;
            return new StrValue()
            {
                Type = ValueType.Color,
                Value = ((long)c.r << 24) | ((long)c.g << 16) | ((long)c.b << 8) | c.a
            };
        }

        public static implicit operator StrValue(UnityEngine.Color32 value)
        {
            return new StrValue()
            {
                Type = ValueType.Color,
                Value = ((long)value.r << 24) | ((long)value.g << 16) | ((long)value.b << 8) | value.a
            };
        }
#endif

        public static StrValue FromObject(object value) => new StrValue()
            {
                Type = ValueType.Object,
                Ref = value
            };
        
        public static StrValue AsWeakRef(WeakRef value) => new StrValue()
        {
            Type = ValueType.WeakRef,
            Ref = value
        };
        
        public static StrValue AsStrongRef(object value) => new StrValue()
        {
            Type = ValueType.StrongRef,
            Ref = value
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetFloat()
        {
            return BitConverter.Int32BitsToSingle((int)Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetDouble()
        {
            return BitConverter.Int64BitsToDouble(Value);
        }

        public DateTime GetDateTime()
        {
            return new DateTime(Value, Ref switch
            {
                DateTimeLocal => DateTimeKind.Local,
                DateTimeUtc => DateTimeKind.Utc,
                _ => DateTimeKind.Unspecified
            });
        }

        public (object, Type) GetObjectAndType()
        {
            if (Type is ValueType.Object or ValueType.StrongRef)
            {
                return (Ref, Ref?.GetType());
            }
            if (Type == ValueType.WeakRef)
            {
                var weakRef = (WeakRef)Ref;
                return (weakRef.Ref.Target, weakRef.Type);
            }
            return (null, null);
        }

        public void Fill(StringBuilder stringBuilder)
        {
            switch (Type)
            {
                case ValueType.Int:
                    LoggerUtils.AppendNum(stringBuilder, Value);
                    break;
                case ValueType.Uint:
                    LoggerUtils.AppendNum(stringBuilder, (ulong)Value);
                    break;
                case ValueType.Long:
                    LoggerUtils.AppendNum(stringBuilder, Value);
                    break;
                case ValueType.ULong:
                    LoggerUtils.AppendNum(stringBuilder, (ulong)Value);
                    break;
                case ValueType.Float:
                    LoggerUtils.AppendNum(stringBuilder, GetFloat());
                    break;
                case ValueType.Double:
                    LoggerUtils.AppendNum(stringBuilder, GetDouble());
                    break;
                case ValueType.String:
                    stringBuilder.Append(Ref);
                    break;
                case ValueType.DateTime:
                    var dateTime = GetDateTime();
                    LoggerUtils.AppendNumWithZeroPadding(stringBuilder, dateTime.Year, 4);
                    stringBuilder.Append("-");
                    LoggerUtils.AppendNumWithZeroPadding(stringBuilder, dateTime.Month, 2);
                    stringBuilder.Append("-");
                    LoggerUtils.AppendNumWithZeroPadding(stringBuilder, dateTime.Day, 2);
                    stringBuilder.Append(dateTime.Kind switch
                    {
                        DateTimeKind.Local => "L",
                        DateTimeKind.Utc => "U",
                        _ => "T"
                    });
                    LoggerUtils.AppendNumWithZeroPadding(stringBuilder, dateTime.Hour, 2);
                    stringBuilder.Append(":");
                    LoggerUtils.AppendNumWithZeroPadding(stringBuilder, dateTime.Minute, 2);
                    stringBuilder.Append(":");
                    LoggerUtils.AppendNumWithZeroPadding(stringBuilder, dateTime.Second, 2);
                    stringBuilder.Append(".");
                    LoggerUtils.AppendNumWithZeroPadding(stringBuilder, dateTime.Millisecond, 3);
                    break;
                case ValueType.TimeSpan:
                    stringBuilder.Append(new TimeSpan(Value));
                    break;
                case ValueType.Bool:
                    stringBuilder.Append(Value == 0L ? "false" : "true");
                    break;
                case ValueType.Color:
                    stringBuilder.Append("#");
                    FillColor(stringBuilder, Value);
                    break;
                case ValueType.WeakRef:
                {
                    var weakRef = (WeakRef)Ref;
                    FillObject(stringBuilder, weakRef.Ref.Target, weakRef.Type);
                    break;
                }
                case ValueType.StrongRef:
                {
                    FillObject(stringBuilder, Ref, null);
                    break;
                }
                case ValueType.None:
                    break;
                default:
                    stringBuilder.Append(Ref);
                    break;
            }
        }

        public static void FillObject(StringBuilder stringBuilder, object value, Type type)
        {
            var str = value?.ToString();
            if (str != null && str != "null")
            {
                if (type != null)
                {
                    if (str == type.FullName) // default C# ToString()
                    {
                        stringBuilder.Append("[");
                        stringBuilder.Append(type.Name);
                        stringBuilder.Append("]");
                        return;
                    }
#if UNITY_2022_1_OR_NEWER
                    if (value is UnityEngine.Object unityObj && unityObj)
                    {
                        var objName = unityObj.name;
                        var fullname = type.FullName;
                        if (str.StartsWith(objName) 
                            && str.EndsWith(")") 
                            && str.Length == objName.Length + 3 + fullname?.Length
                            && str.AsSpan(objName.Length + 2).StartsWith(fullname.AsSpan()))
                        {
                            stringBuilder.Append("[");
                            stringBuilder.Append(type.Name);
                            stringBuilder.Append(": ");
                            stringBuilder.Append(unityObj.name);
                            stringBuilder.Append("]");
                            return;
                        }
                    }
#endif
                }
                stringBuilder.Append(str);
            }
            else if(type != null)
            {
                stringBuilder.Append("null (");
                stringBuilder.Append(type.Name);
                stringBuilder.Append(")");
            }
            else
            {
                stringBuilder.Append("null");
            }
        }

        public static void FillColor(StringBuilder stringBuilder, long col)
        {
            for (var shift = 28; shift >= 0; shift -= 4)
            {
                var nibble = (int)((col >> shift) & 0xF);
                stringBuilder.Append(HexChars[nibble]);
            }
        }

        public bool IsObjectType()
        {
            return Type is ValueType.Object or ValueType.WeakRef or ValueType.StrongRef;
        }
            
        public enum ValueType : byte
        {
            None,
            String,
            Int,
            Uint,
            Long,
            ULong,
            Float,
            Double,
            DateTime,
            TimeSpan,
            Object,
            Bool,
            WeakRef, // this is a version where it can be a weak reference, but also caches the name so even if its gone we can print what it was.
            StrongRef, // kinda same as Object but tells nj logger not to convert to weak
            Color,
        }

        public class WeakRef
        {
            public WeakReference Ref;
            public Type Type;
        }
    }
}