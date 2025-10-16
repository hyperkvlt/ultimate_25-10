using System;
using System.Diagnostics;
using System.Text;

namespace Ninjadini.Logger
{
    /// <summary>
    /// LogLine holds all the information about a log.<br/>
    /// If you want to just get a string, call logLine.GetLineString()<br/>
    /// If you want full details like the stack trace lines, you'll have to populate it via PopulateDetailsString();
    /// </summary>
    public class LogLine
    {
        public int Count { get; private set; }
        public readonly StrValue[] Values;
        
        /// <summary>
        /// Log flags such as level, channel inclusion, stacktrace control.
        /// </summary>
        public NjLogger.Options Options;
        
        /// <summary>
        /// Stacktrace associated with the log.<br/>
        /// It can be:<br/>
        /// - string - if it was produced from Unity<br/>
        /// - StackTrace - if it was produced from NjLogger.
        /// </summary>
        public object StackTrace { get; private set; }
        
        /// <summary>
        /// The value from Unity Debug.Log(..., -context-)
        /// This only works if you have "Pass through to NjLogger" set.
        /// In editor, it can't be set from settings panel.
        /// </summary>
        public WeakReference Context { get; private set; }
        
        /// <summary>
        /// DateTime.Now at the time of log recording
        /// </summary>
        public DateTime Time;

        public NjLogger.Options Level => Options & NjLogger.Options.Error;

        public bool WasFromUnity => (Options & NjLogger.Options.FromUnity) != 0;

        public bool HasValues() => Values?.Length > 0 && Values[0].Type != StrValue.ValueType.None;

        public LogLine(int maxNumParams)
        {
            Values = new StrValue[maxNumParams];
        }

        string _string;

        /// <summary>
        /// Get log message string<br/>
        /// It'll combine the message params into a string if it hasn't been cached yet.
        /// </summary>
        public string GetLineString()
        {
            if (_string == null)
            {
                if (Count == 1 && Values[0].Type == StrValue.ValueType.String)
                {
                    _string = Values[0].Ref as string ?? string.Empty;
                    return _string;
                }
                var sb = LoggerUtils.TempStringBuilder.Clear();
                TryPopulateSingleLineString(sb);
                _string = sb.ToString();
                sb.Clear();
            }
            return _string;
        }

        /// <summary>
        /// Get channel name, if no channel, returns an empty string.
        /// </summary>
        public string GetChannelName()
        {
            if ((Options & NjLogger.Options.IncludesChannel) != 0 && Values[0].Ref is string channel)
            {
                return channel;
            }
            return string.Empty;
        }

        public LogLine GetCopy()
        {
            var result = new LogLine(Values.Length)
            {
                Time = Time,
                Count = Count,
                Options = Options,
                StackTrace = StackTrace,
                Context = Context,
                _string = _string
            };
            Array.Copy(Values, result.Values, Values.Length);
            return result;
        }

        public void Set(ref NjLogger.LogRow logRow)
        {
            _string = null;
            
            Time = DateTime.Now;
            Options = logRow.Options;
            StackTrace = logRow.StackTrace;
            if (logRow.Context != null)
            {
                if (Context == null)
                {
                    Context = new WeakReference(logRow.Context);
                }
                else
                {
                    Context.Target = logRow.Context;
                }
            }
            else if (Context != null)
            {
                Context.Target = null;
            }
            
            var from = logRow.Values;
            var count = from.Length;
            var maxLen = Math.Max(count, Count);
            if(count > maxLen)
            {
                throw new Exception("Too many log params, not supported yet.");
            }
            Count = count;
            for(var i = 0; i < maxLen; i++)
            {
                ref var oldValue = ref Values[i];
                if (oldValue is { Type: StrValue.ValueType.WeakRef, Ref: StrValue.WeakRef oldWeakRef })
                {
                    oldWeakRef.Return();
                }
                if (i < count)
                {
                    ref var strValue = ref from[i];
                    if (strValue is { Type: StrValue.ValueType.Object, Ref: not null and not Exception } && strValue.Ref.GetType().IsClass)
                    {
                        Values[i] = StrValue.AsWeakRef(LoggerUtils.BorrowWeakRef(strValue.Ref));
                    }
                    else
                    {
                        Values[i] = strValue;
                    }
                }
                else
                {
                    Values[i] = default;
                }
            }
        }
        
        void TryPopulateSingleLineString(StringBuilder stringBuilder)
        {
            var args = Values;
            for (int index = 0, l = Count; index < l; index++)
            {
                var arg = args[index];
                if (arg.Type == StrValue.ValueType.None)
                {
                    break;
                }
                if(index == 0 && (Options & NjLogger.Options.IncludesChannel) != 0)
                {
                    stringBuilder.Append("[");
                    arg.Fill(stringBuilder);
                    stringBuilder.Append("] ");
                    continue;
                }
                if (arg.Type == StrValue.ValueType.Color)
                {
                    if (index <= ((Options & NjLogger.Options.IncludesChannel) != 0 ? 1 : 0) && index < l - 1)
                    {
                        // the first item is color and its also not the last item.
                        stringBuilder.Append("<color=");
                        arg.Fill(stringBuilder);
                        stringBuilder.Append(">"); // it doesn't end
                    }
                    else
                    {
                        stringBuilder.Append("<color=");
                        arg.Fill(stringBuilder);
                        stringBuilder.Append(">#</color>");
                        StrValue.FillColor(stringBuilder, arg.Value);
                    }
                }
                else if (arg.IsObjectType())
                {
                    stringBuilder.Append("<b>");
                    arg.Fill(stringBuilder);
                    stringBuilder.Append("</b>");
                }
                else
                {
                    arg.Fill(stringBuilder);
                }
            }
        }
    }
}