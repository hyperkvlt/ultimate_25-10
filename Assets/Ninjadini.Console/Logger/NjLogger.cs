using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Ninjadini.Logger.Internal;

namespace Ninjadini.Logger
{
    public static class NjLogger
    {
        static NjLogger()
        {
            LoggerUtils.ThreadLocalPool<StrValue.WeakRef>.Constructor = () => new StrValue.WeakRef();
        }
        
        /// <summary>
        /// Logs a message at debug level.  
        /// Debug logs are conditionally excluded from release builds (compiled only in DEBUG mode).
        /// 
        /// Accepts a mix of strings, numbers, and other primitive types without allocations.  
        /// You can freely combine different value types in a single call.  
        /// 
        /// For objects that aren't implicitly convertible to <c>StrValue</c>,  
        /// use <c>obj.AsLogRef()</c> or <c>obj.AsString()</c> to include them explicitly.
        /// <code>
        /// NjLogger.Debug("Profile loaded: ", profile.Name, " object: ", profile.AsLogRef());
        /// NjLogger.Debug("Bytes loaded: ", numBytes, " Profile: ", profile.AsLogRef());
        /// </code>
        /// </summary>
        [Conditional("DEBUG")]
        public static void Debug(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, StrValue v5 = default, Options options = 0, object context = null)
        {
            _Add(v0, v1, v2, v3, v4, v5, WithLevel(options, Options.Debug), context);
        }

        /// <summary>
        /// Logs a message at debug level.  
        /// Debug logs are conditionally excluded from release builds (compiled only in DEBUG mode).  
        /// 
        /// This overload allows logging an object directly without needing <c>AsLogRef()</c>.  
        /// <code>
        /// NjLogger.Debug("Profile loaded: ", profile);
        /// NjLogger.Debug("Profile loaded: ", profile.Name);
        /// NjLogger.Debug("BytesLoaded: ", 123); // uses the other overload with no allocation
        /// </code>
        /// </summary>
        [Conditional("DEBUG")]
        public static void Debug(StrValue str, object obj, Options options = 0, object context = null)
        {
            _Add(str, obj.AsLogRef(), default, default, default, default, WithLevel(options, Options.Debug), context);
        }
        
        /// <summary>
        /// Logs a message at info level. It exists to be similar to Unity's Debug.Log()
        /// 
        /// Accepts a mix of strings, numbers, and other primitive types without allocations.  
        /// You can freely combine different value types in a single call.  
        /// 
        /// For objects that aren't implicitly convertible to <c>StrValue</c>,  
        /// use <c>obj.AsLogRef()</c> or <c>obj.AsString()</c> to include them explicitly.
        /// <code>
        /// NjLogger.Log("Profile loaded: ", profile.Name, " object: ", profile.AsLogRef());
        /// NjLogger.Log("Bytes loaded: ", numBytes, " Profile: ", profile.AsLogRef());
        /// </code>
        /// </summary>
        public static void Log(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, StrValue v5 = default, Options options = 0, object context = null)
        {
            _Add(v0, v1, v2, v3, v4, v5, WithLevel(options, Options.Info), context);
        }

        /// <summary>
        /// Logs a message at info level. It exists to be similar to Unity's Debug.Log() 
        /// 
        /// This overload allows logging an object directly without needing <c>AsLogRef()</c>.  
        /// <code>
        /// NjLogger.Debug("Profile loaded: ", profile);
        /// NjLogger.Debug("Profile loaded: ", profile.Name);
        /// NjLogger.Debug("BytesLoaded: ", 123); // uses the other overload with no allocation
        /// </code>
        /// </summary>
        public static void Log(StrValue str, object obj, Options options = 0, object context = null)
        {
            _Add(str, obj.AsLogRef(), default, default, default, default, WithLevel(options, Options.Info), context);
        }
        
        /// <summary>
        /// Logs a message at info level.
        /// 
        /// Accepts a mix of strings, numbers, and other primitive types without allocations.  
        /// You can freely combine different value types in a single call.  
        /// 
        /// For objects that aren't implicitly convertible to <c>StrValue</c>,  
        /// use <c>obj.AsLogRef()</c> or <c>obj.AsString()</c> to include them explicitly.
        /// <code>
        /// NjLogger.Info("Profile loaded: ", profile.Name, " object: ", profile.AsLogRef());
        /// NjLogger.Info("Bytes loaded: ", numBytes, " Profile: ", profile.AsLogRef());
        /// </code>
        /// </summary>
        public static void Info(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, StrValue v5 = default, Options options = 0, object context = null)
        {
            _Add(v0, v1, v2, v3, v4, v5, WithLevel(options, Options.Info), context);
        }

        /// <summary>
        /// Logs a message at info level.
        /// 
        /// This overload allows logging an object directly without needing <c>AsLogRef()</c>.  
        /// <code>
        /// NjLogger.Info("Profile loaded: ", profile);
        /// NjLogger.Info("Profile loaded: ", profile.Name);
        /// NjLogger.Info("BytesLoaded: ", 123); // uses the other overload with no allocation
        /// </code>
        /// </summary>
        public static void Info(StrValue str, object obj, Options options = 0, object context = null)
        {
            _Add(str, obj.AsLogRef(), default, default, default, default, WithLevel(options, Options.Info), context);
        }

        /// <summary>
        /// Logs a message at warning level.
        /// 
        /// Accepts a mix of strings, numbers, and other primitive types without allocations.  
        /// You can freely combine different value types in a single call.  
        /// 
        /// For objects that aren't implicitly convertible to <c>StrValue</c>,  
        /// use <c>obj.AsLogRef()</c> or <c>obj.AsString()</c> to include them explicitly.
        /// <code>
        /// NjLogger.Warn("Profile loaded: ", profile.Name, " object: ", profile.AsLogRef());
        /// NjLogger.Warn("Bytes loaded: ", numBytes, " Profile: ", profile.AsLogRef());
        /// </code>
        /// </summary>
        public static void Warn(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, StrValue v5 = default, Options options = 0, object context = null)
        {
            _Add(v0, v1, v2, v3, v4, v5, WithLevel(options, Options.Warn), context);
        }

        /// <summary>
        /// Logs a message at warning level.
        /// 
        /// This overload allows logging an object directly without needing <c>AsLogRef()</c>.  
        /// <code>
        /// NjLogger.Warn("Profile loaded: ", profile);
        /// NjLogger.Warn("Profile loaded: ", profile.Name);
        /// NjLogger.Warn("BytesLoaded: ", 123); // uses the other overload with no allocation
        /// </code>
        /// </summary>
        public static void Warn(StrValue str, object obj, Options options = 0, object context = null)
        {
            _Add(str, obj.AsLogRef(), default, default, default, default, WithLevel(options, Options.Warn), context);
        }


        /// <summary>
        /// Logs a message at the error level.  
        /// By default, this triggers a visible error prompt at the top of the screen.  
        /// This behavior can be configured via <c>Project Settings &gt; NjConsole &gt; Logging &gt; Behaviour On Error</c>.  
        /// 
        /// Accepts a mix of strings, numbers, and other primitive types without allocations.  
        /// You can freely combine different value types in a single call.  
        /// 
        /// For objects that aren't implicitly convertible to <c>StrValue</c>,  
        /// use <c>obj.AsLogRef()</c> or <c>obj.AsString()</c> to include them explicitly.
        /// <code>
        /// NjLogger.Error("Incomplete profile loaded: ", profile.Name, " object: ", profile.AsLogRef());
        /// </code>
        /// </summary>
        public static void Error(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, StrValue v5 = default, Options options = 0, object context = null)
        {
            _Add(v0, v1, v2, v3, v4, v5, WithLevel(options, Options.Error), context);
        }

        /// <summary>
        /// Logs a message at the error level.  
        /// By default, this triggers a visible error prompt at the top of the screen.  
        /// This behavior can be configured via <c>Project Settings &gt; NjConsole &gt; Logging &gt; Behaviour On Error</c>.  
        /// 
        /// This overload allows logging an object directly without needing <c>AsLogRef()</c>.  
        /// <code>
        /// NjLogger.Error("Incomplete profile loaded: ", profile);
        /// </code>
        /// </summary>
        public static void Error(StrValue str, object obj, Options options = 0, object context = null)
        {
            _Add(str, obj.AsLogRef(), default, default, default, default, WithLevel(options, Options.Error), context);
        }

        /// <summary>
        /// Logs an exception at the error level. 
        /// By default, this triggers a visible error prompt at the top of the screen.  
        /// This behavior can be configured via <c>Project Settings &gt; NjConsole &gt; Logging &gt; Behaviour On Error</c>.  
        /// 
        /// <code>
        /// NjLogger.Exception(anException, "Exception while loading profile");
        /// </code>
        /// </summary>
        public static void Exception(Exception exception, StrValue v1 = default, StrValue v2 = default, Options options = 0, object context = null)
        {
            var strValues = Borrow();
            var index = -1;
            if(v1.Type != StrValue.ValueType.None) strValues[++index] = v1;
            if(v2.Type != StrValue.ValueType.None) strValues[++index] = v2;
            strValues[++index] = exception;
            object stackTrace = exception.StackTrace;
            if (string.IsNullOrEmpty(exception.StackTrace) && IsStackTraceEnabled(Options.Error))
            {
                stackTrace = new StackTrace(1, true);
            }
            var row = new LogRow()
            {
                Values = new Span<StrValue>(strValues, 0, index + 1),
                StackTrace = stackTrace,
                Context = context,
                Options = WithLevel(options, Options.Error)
            };
            LogsHistory.Add(ref row);
            foreach (var handler in Handlers)
            {
                handler.HandleException(exception, ref row);
            }
            Return(strValues);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, StrValue v5 = default, Options options = 0, object context = null)
        {
            _Add(v0, v1, v2, v3, v4, v5, options, context);
        }
        
        internal static void _Add(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, StrValue v5 = default, Options options = 0, object context = null)
        {
            var strValues = Borrow();
            var index = 0;
            strValues[0] = v0;
            if(v1.Type != StrValue.ValueType.None) strValues[++index] = v1;
            if(v2.Type != StrValue.ValueType.None) strValues[++index] = v2;
            if(v3.Type != StrValue.ValueType.None) strValues[++index] = v3;
            if(v4.Type != StrValue.ValueType.None) strValues[++index] = v4;
            if(v5.Type != StrValue.ValueType.None) strValues[++index] = v5;
            var stacktrace = IsStackTraceEnabled(options) ? new StackTrace(2, true) : null;
            HandleLog(new LogRow()
            {
                Values = new Span<StrValue>(strValues, 0, index + 1),
                StackTrace = stacktrace,
                Context = context,
                Options = options
            });
            Return(strValues);
        }

        /// <summary>
        /// For internal use only.
        /// This is where unity logs get passed through to NjLogger
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void HandleUnityLog(LogRow row)
        {
            LogsHistory.Add(ref row);
            foreach (var handler in Handlers)
            {
                handler.HandleLog(ref row);
            }
        }
        
        /// <summary>
        /// Access to Logs History.<br/>
        /// Logs are stored in a rotating ring - the size of the ring depends on what you set in project settings for player or editor.
        /// The easiest way for you to pull logs out of history is by using ForEachLogNewestToOldest() or ForEachLogOldestToNewest()<br/>
        /// See GenerateHistoryNewestToOldest for example.
        /// </summary>
        public static readonly LogsHistory LogsHistory = new LogsHistory(Math.Max(128, LogsHistory.DesiredStartingLogsHistorySize));

        static readonly List<IHandler> Handlers = new List<IHandler>()
        {
#if !UNITY_2021_1_OR_NEWER
            // in case you are outside Unity, log to System.Console.
            new NjLoggerToConsoleHandler(),
#endif
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void HandleLog(LogRow row)
        {
            LogsHistory.Add(ref row);
            foreach (var handler in Handlers)
            {
                handler.HandleLog(ref row);
            }
        }
        
        /// <summary>
        /// Get all registered custom log handlers
        /// </summary>
        public static IEnumerable<IHandler> GetHandlers() => Handlers;
        
        /// <summary>
        /// Add a custom log handler.
        /// The IHandler will receive all logs that gets through to NjLogger.  
        /// The handlers are stacked and called in the order they were added to NjLogger.  
        /// </summary>
        public static void AddHandler(IHandler handler)
        {
            if (!Handlers.Contains(handler))
            {
                if (Handlers.Count > 16)
                {
                    // in case something crazy happening...
                    throw new Exception($"Too many NjLogger handlers are being added ({Handlers.Count})... please remove previous ones before adding more.");
                }
                Handlers.Add(handler);
            }
        }
        
        
        /// <summary>
        /// Remove a custom log handler
        /// </summary>
        public static void RemoveHandler(IHandler handler)
        {
            Handlers.Remove(handler);
        }
        
        static int _minStackTraceLevel;
        
        /// <summary>
        /// Set minimum required log level to produce a stacktrace
        /// This can be configured via <c>Project Settings &gt; NjConsole &gt; Logging &gt; Stack Trace Min Level</c>.  
        /// </summary>
        public static void SetMinSetTraceLevel(int levelAsInt)
        {
            _minStackTraceLevel = levelAsInt;
        }

        /// <summary>
        /// Check if a particular log option should produce stack traces.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsStackTraceEnabled(Options options)
        {
            return ((options & Options.ForceStackTrace) != 0 ||  ((int)options & LoggerUtils.LevelsMask) >= _minStackTraceLevel)
                   && (options & Options.ForceNoStackTrace) == 0;
        }

        [ThreadStatic]
        static Stack<StrValue[]> _strValuePool; // can't use ThreadLocalPool cause the array length might not match.
        static Stack<StrValue[]> StrValuePool => _strValuePool ??= new Stack<StrValue[]>(8);
        const int MaxStrValuePoolCount = 4; // really only need multiple of these if you are calling HandleLog() from within it self.

        static StrValue[] Borrow()
        {
            if (StrValuePool.Count > 0)
            {
                return StrValuePool.Pop();
            }
            return new StrValue[LogsHistory.MaxNumParams];
        }
        
        static void Return(StrValue[] strValues)
        {
            if (StrValuePool.Count < MaxStrValuePoolCount)
            {
                Array.Clear(strValues, 0, strValues.Length);
                StrValuePool.Push(strValues);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Options WithLevel(Options input, Options level)
        {
            return (Options)((int)input & ~LoggerUtils.LevelsMask) | level;
        }
        

        /// <summary>
        /// Custom log handler.
        /// The handlers are stacked and called in the order they were added to NjLogger
        /// </summary>
        public interface IHandler
        {
            /// <summary>
            /// Received log.
            /// Modifying the contents of <c>LogRow</c> may affect other log handlers. Use with caution.
            /// </summary>
            void HandleLog(ref LogRow logRow);
            
            /// <summary>
            /// Received exception. (called Logger.Exception()).
            /// Modifying the contents of <c>LogRow</c> may affect other log handlers. Use with caution.
            /// </summary>
            void HandleException(Exception exception, ref LogRow logRow);
        }
        
        /// <summary>
        /// LogRow holds all the information about a log in temporary ref struct<br/>
        /// If you just want a string for the log, call logRow.GetString(LoggerUtils.TempStringBuilder)<br/>
        /// </summary>
        public ref struct LogRow
        {
            /// <summary>
            /// Values associated with the log. It can be holding multiple values of strings, int, objects etc
            /// If you just want a string for the log, call logRow.GetString(LoggerUtils.TempStringBuilder)<br/>
            /// </summary>
            public Span<StrValue> Values;
        
            /// <summary>
            /// Log flags such as level, channel inclusion, stacktrace control.
            /// </summary>
            public Options Options;
        
            /// <summary>
            /// Stacktrace associated with the log.<br/>
            /// It can be:<br/>
            /// - string - if it was produced from Unity<br/>
            /// - StackTrace - if it was produced from NjLogger.
            /// </summary>
            public object StackTrace;
            
            /// <summary>
            /// The value from Unity Debug.Log(..., -context-)
            /// This only works if you have "Pass through to NjLogger" set.
            /// In editor, it can't be set from settings panel.
            /// </summary>
            public object Context;
            
            string _str;
            
            public Level Level => Options.GetLevel();
            
            public bool HasChannel => (Options & Options.IncludesChannel) != 0;

            public bool WasFromUnity => (Options & Options.FromUnity) != 0;
            
            public string GetString(StringBuilder stringBuilderPool)
            {
                if (_str != null)
                {
                    return _str;
                }
                var count = Values.Length;
                if (count == 1 && Values[0].Type == StrValue.ValueType.String)
                {
                    _str = Values[0].Ref as string;
                    return _str;
                }
                stringBuilderPool.Length = 0;
                    
                if((Options & Options.IncludesChannel) != 0)
                {
                    stringBuilderPool.Append("[");
                    Values[0].Fill(stringBuilderPool);
                    stringBuilderPool.Append("] ");
                }
                else
                {
                    Values[0].Fill(stringBuilderPool);
                }
                for (var i = 1; i < count; i++)
                {
                    Values[i].Fill(stringBuilderPool);
                }
                _str = stringBuilderPool.ToString();
                stringBuilderPool.Length = 0;
                return _str;
            }
        }

        [Flags]
        public enum Options : short
        {
            Debug = 0,
            Info = 1,
            Warn = 2,
            Error = 3,
            Repeating = 1 << 4, // not supported yet
            ForceStackTrace = 1 << 5,
            ForceNoStackTrace = 1 << 6,
            IncludesChannel = 1 << 7,
            FromUnity = 1 << 8
        }
        
        public enum Level : byte
        {
            Debug = (byte)Options.Debug,
            Info = (byte)Options.Info,
            Warn = (byte)Options.Warn,
            Error = (byte)Options.Error
        }
    }
}