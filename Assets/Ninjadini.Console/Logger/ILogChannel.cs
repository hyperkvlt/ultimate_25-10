using System;
using System.Diagnostics;

namespace Ninjadini.Logger
{
    public interface ILogChannel
    {
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
        void Debug(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, NjLogger.Options options = 0, object context = null);

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
        void Debug(StrValue str, object obj, NjLogger.Options options = 0, object context = null)
        {
            Debug(str, obj.AsLogRef(), options:options, context:context);
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
        void Log(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, NjLogger.Options options = 0, object context = null);

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
        void Log(StrValue str, object obj, NjLogger.Options options = 0, object context = null)
        {
            Log(str, obj.AsLogRef(), options:options, context:context);
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
        void Info(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, NjLogger.Options options = 0, object context = null);

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
        void Info(StrValue str, object obj, NjLogger.Options options = 0, object context = null)
        {
            Info(str, obj.AsLogRef(), options:options, context:context);
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
        void Warn(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, NjLogger.Options options = 0, object context = null);

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
        void Warn(StrValue str, object obj, NjLogger.Options options = 0, object context = null)
        {
            Warn(str, obj.AsLogRef(), options:options, context:context);
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
        void Error(StrValue v0, StrValue v1 = default, StrValue v2 = default, StrValue v3 = default, StrValue v4 = default, NjLogger.Options options = 0, object context = null);

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
        void Error(StrValue str, object obj, NjLogger.Options options = 0, object context = null)
        {
            Error(str, obj.AsLogRef(), options:options, context:context);
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
        void Exception(Exception exception, StrValue v0 = default, object context = null);
    }
}