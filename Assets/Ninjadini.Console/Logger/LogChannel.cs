using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Options = Ninjadini.Logger.NjLogger.Options;

namespace Ninjadini.Logger
{
    public readonly struct LogChannel : ILogChannel
    {
        public readonly string Name;

        public LogChannel(string nameName)
        {
            Name = nameName;
        }
        
        /// <summary>
        /// Logs a message to the channel at debug level.
        /// Debug logs are conditionally excluded from release builds (compiled only in DEBUG mode).
        /// 
        /// Accepts a mix of strings, numbers, and other primitive types without allocations.  
        /// You can freely combine different value types in a single call.  
        /// 
        /// For objects that aren't implicitly convertible to <c>StrValue</c>,  
        /// use <c>obj.AsLogRef()</c> or <c>obj.AsString()</c> to include them explicitly.
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Debug("Profile loaded: ", profile.Name, " object: ", profile.AsLogRef());
        /// channel.Debug("Bytes loaded: ", numBytes, " Profile: ", profile.AsLogRef());
        /// </code>
        /// </summary>
        public void Debug(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, v0, v1, v2, v3, v4, 
                WithLevel(options | Options.IncludesChannel, Options.Debug), 
                context);
        }
        
        void ILogChannel.Debug(StrValue v0, StrValue v1, StrValue v2, StrValue v3, StrValue v4, Options options, object context)
        {
            #if DEBUG
            NjLogger._Add(Name, v0, v1, v2, v3, v4, 
                WithLevel(options | Options.IncludesChannel, Options.Debug), 
                context);
            #endif
        }


        /// <summary>
        /// Logs a message to the channel at debug level.
        /// Debug logs are conditionally excluded from release builds (compiled only in DEBUG mode).  
        /// 
        /// This overload allows logging an object directly without needing <c>AsLogRef()</c>.  
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Debug("Profile loaded: ", profile);
        /// channel.Debug("Profile loaded: ", profile.Name);
        /// channel.Debug("BytesLoaded: ", 123); // uses the other overload with no allocation
        /// </code>
        /// </summary>
        [Conditional("DEBUG")]
        public void Debug(StrValue str, object obj, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, str, obj.AsLogRef(), default, default, default, 
                WithLevel(options | Options.IncludesChannel, Options.Debug), 
                context);
        }
        
        void ILogChannel.Debug(StrValue str, object obj, Options options, object context)
        {
#if DEBUG
            NjLogger._Add(Name, str, obj.AsLogRef(), default, default, default, 
                WithLevel(options | Options.IncludesChannel, Options.Debug), 
                context);
#endif
        }
        
        /// <summary>
        /// Logs a message to the channel at info level. It exists to be similar to Unity's Debug.Log()
        /// 
        /// Accepts a mix of strings, numbers, and other primitive types without allocations.  
        /// You can freely combine different value types in a single call.  
        /// 
        /// For objects that aren't implicitly convertible to <c>StrValue</c>,  
        /// use <c>obj.AsLogRef()</c> or <c>obj.AsString()</c> to include them explicitly.
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Log("Profile loaded: ", profile.Name, " object: ", profile.AsLogRef());
        /// channel.Log("Bytes loaded: ", numBytes, " Profile: ", profile.AsLogRef());
        /// </code>
        /// </summary>
        public void Log(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, v0, v1, v2, v3, v4, 
                WithLevel(options | Options.IncludesChannel, Options.Info), 
                context);
        }

        /// <summary>
        /// Logs a message to the channel at info level. It exists to be similar to Unity's Debug.Log() 
        /// 
        /// This overload allows logging an object directly without needing <c>AsLogRef()</c>.  
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Debug("Profile loaded: ", profile);
        /// channel.Debug("Profile loaded: ", profile.Name);
        /// channel.Debug("BytesLoaded: ", 123); // uses the other overload with no allocation
        /// </code>
        /// </summary>
        public void Log(StrValue str, object obj, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, str, obj.AsLogRef(), default, default, default, 
                WithLevel(options | Options.IncludesChannel, Options.Info), 
                context);
        }
        
        /// <summary>
        /// Logs a message to the channel at info level.
        /// 
        /// Accepts a mix of strings, numbers, and other primitive types without allocations.  
        /// You can freely combine different value types in a single call.  
        /// 
        /// For objects that aren't implicitly convertible to <c>StrValue</c>,  
        /// use <c>obj.AsLogRef()</c> or <c>obj.AsString()</c> to include them explicitly.
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Info("Profile loaded: ", profile.Name, " object: ", profile.AsLogRef());
        /// channel.Info("Bytes loaded: ", numBytes, " Profile: ", profile.AsLogRef());
        /// </code>
        /// </summary>
        public void Info(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, v0, v1, v2, v3, v4, 
                WithLevel(options | Options.IncludesChannel, Options.Info), 
                context);
        }


        /// <summary>
        /// Logs a message to the channel at info level.
        /// 
        /// This overload allows logging an object directly without needing <c>AsLogRef()</c>.  
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Info("Profile loaded: ", profile);
        /// channel.Info("Profile loaded: ", profile.Name);
        /// channel.Info("BytesLoaded: ", 123); // uses the other overload with no allocation
        /// </code>
        /// </summary>
        public void Info(StrValue str, object obj, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, str, obj.AsLogRef(), default, default, default, 
                WithLevel(options | Options.IncludesChannel, Options.Info), 
                context);
        }

        /// <summary>
        /// Logs a message to the channel at warning level.
        /// 
        /// Accepts a mix of strings, numbers, and other primitive types without allocations.  
        /// You can freely combine different value types in a single call.  
        /// 
        /// For objects that aren't implicitly convertible to <c>StrValue</c>,  
        /// use <c>obj.AsLogRef()</c> or <c>obj.AsString()</c> to include them explicitly.
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Warn("Profile loaded: ", profile.Name, " object: ", profile.AsLogRef());
        /// channel.Warn("Bytes loaded: ", numBytes, " Profile: ", profile.AsLogRef());
        /// </code>
        /// </summary>
        public void Warn(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, v0, v1, v2, v3, v4, 
                WithLevel(options | Options.IncludesChannel, Options.Warn), 
                context);
        }

        /// <summary>
        /// Logs a message to the channel at warning level.
        /// 
        /// This overload allows logging an object directly without needing <c>AsLogRef()</c>.  
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Warn("Profile loaded: ", profile);
        /// channel.Warn("Profile loaded: ", profile.Name);
        /// channel.Warn("BytesLoaded: ", 123); // uses the other overload with no allocation
        /// </code>
        /// </summary>
        public void Warn(StrValue str, object obj, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, str, obj.AsLogRef(), default, default, default, 
                WithLevel(options | Options.IncludesChannel, Options.Warn), 
                context);
        }



        /// <summary>
        /// Logs a message to the channel at the error level.  
        /// By default, this triggers a visible error prompt at the top of the screen.  
        /// This behavior can be configured via <c>Project Settings &gt; NjConsole &gt; Logging &gt; Behaviour On Error</c>.  
        /// 
        /// Accepts a mix of strings, numbers, and other primitive types without allocations.  
        /// You can freely combine different value types in a single call.  
        /// 
        /// For objects that aren't implicitly convertible to <c>StrValue</c>,  
        /// use <c>obj.AsLogRef()</c> or <c>obj.AsString()</c> to include them explicitly.
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Error("Incomplete profile loaded: ", profile.Name, " object: ", profile.AsLogRef());
        /// </code>
        /// </summary>
        public void Error(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, v0, v1, v2, v3, v4, 
                WithLevel(options | Options.IncludesChannel, Options.Error), 
                context);
        }


        /// <summary>
        /// Logs a message to the channel at the error level.  
        /// By default, this triggers a visible error prompt at the top of the screen.  
        /// This behavior can be configured via <c>Project Settings &gt; NjConsole &gt; Logging &gt; Behaviour On Error</c>.  
        /// 
        /// This overload allows logging an object directly without needing <c>AsLogRef()</c>.  
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Error("Incomplete profile loaded: ", profile);
        /// </code>
        /// </summary>
        public void Error(StrValue str, object obj, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, str, obj.AsLogRef(), default, default, default, 
                WithLevel(options | Options.IncludesChannel, Options.Error), 
                context);
        }


        /// <summary>
        /// Logs an exception to the channel at the error level. 
        /// By default, this triggers a visible error prompt at the top of the screen.  
        /// This behavior can be configured via <c>Project Settings &gt; NjConsole &gt; Logging &gt; Behaviour On Error</c>.  
        /// 
        /// <code>
        /// static LogChannel channel = new LogChannel("myChannel");
        /// channel.Exception(anException, "Exception while loading profile");
        /// </code>
        /// </summary>
        public void Exception(Exception exception, StrValue v0 = default, object context = null)
        {
            NjLogger.Exception(exception, Name, v0, options: Options.IncludesChannel, context: context);
        }

        /// <summary>
        /// A lower level log messaging control.
        /// Not recommended for general use.
        /// You must declare the Options values otherwise it'll just go to debug level (without conditional removal)
        /// </summary>
        public void Add(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, Options options = 0, object context = null)
        {
            NjLogger._Add(Name, v0, v1, v2, v3, v4, 
                options | Options.IncludesChannel, 
                context);
        }

        public static implicit operator LogChannel(string value) => new LogChannel(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Options WithLevel(Options input, Options level)
        {
            return (Options)((int)input & ~LoggerUtils.LevelsMask) | level;
        }
    }
}