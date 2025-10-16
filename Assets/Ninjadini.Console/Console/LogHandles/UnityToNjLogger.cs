using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Ninjadini.Logger.Internal
{
    public class UnityToNjLogger : ILogHandler
    {
        public enum Modes
        {
            [Tooltip("Recommended for player/builds. All logs will go directly to NjLogger only to increase performance.")]
            PassThroughToNjLogger = 1,
            
            [Tooltip("Recommended for Editor. Unity's logs are also sent to NjLogger via `Application.logMessageReceived`, context param from Unity log will not be captured.")]
            BothUnityAndNjLogger = 2,
        
            [Tooltip("Experimental: this passes the context param from Unity log to NjLogger, however the stack trace tracking my be broken")]
            BothUnityAndNjLoggerWithContext = 3,
        
            [Tooltip("Logs in unity (e.g. Debug.Log()) will not show up in NjLogger/Console")]
            StayInUnityConsole = 4
        }

        public static readonly string ChannelName = "unity";

        public static Modes? LogsMode { get; private set; }
        
        public static void Start(Modes unityLogMode, int maxLogs)
        {
            if (LogsMode.HasValue)
            {
                return;
            }
            LogsMode = unityLogMode;
            
            LogsHistory.DesiredStartingLogsHistorySize = maxLogs;
            NjLogger.LogsHistory.SetMaxHistoryCount(maxLogs);
            
            if (unityLogMode == Modes.StayInUnityConsole)
            {
                // nothing :'(
            }
            else if (unityLogMode == Modes.BothUnityAndNjLogger)
            {
                var unityLogger = new UnityToNjLogger(Debug.unityLogger.logHandler);
                Application.logMessageReceivedThreaded += unityLogger.Thread_OnLogMessageReceived;
            }
            else // BothUnityAndNjLoggerWithContext or PassThroughToNjLogger
            {
                if (Debug.unityLogger.logHandler is not UnityToNjLogger)
                {
                    var unityLogger = new UnityToNjLogger(Debug.unityLogger.logHandler);
                    Debug.unityLogger.logHandler = unityLogger;
                    Application.logMessageReceivedThreaded += unityLogger.Thread_OnLogMessageReceived;
                }
            }
        }

        public static bool SendToChannel;

        readonly ILogHandler _originalLogHandler;
        Object _lastContext; // not the best way but there was no way as it work with a mix of threaded log messages. but there was no way to fish out the context from logs.
        
        UnityToNjLogger(ILogHandler originalLogHandler)
        {
            _originalLogHandler = originalLogHandler;
        }

#if UNITY_2023_1_OR_NEWER
        [HideInCallstack]
#endif
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            _lastContext = context;
            if (LogsMode == Modes.PassThroughToNjLogger)
            {
                var str = args.Length switch
                {
                    0 => format,
                    1 when format == "{0}" => args[0] as string ?? args[0]?.ToString(),
                    _ => string.Format(format, args)
                };
                HandleLog(str, null, logType);
            }
            else
            {
                _originalLogHandler.LogFormat(logType, context, format, args);
            }
        }
        
#if UNITY_2023_1_OR_NEWER
        [HideInCallstack]
#endif
        public void LogException(Exception exception, Object context)
        {
            _lastContext = null;
            if (LogsMode != Modes.PassThroughToNjLogger)
            {
                _originalLogHandler.LogException(exception, context);
            }
            NjLogger.Exception(exception, context:context);
        }

        void Thread_OnLogMessageReceived(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Exception && LogsMode is Modes.PassThroughToNjLogger or Modes.BothUnityAndNjLoggerWithContext)
            {
                // exception is ignored because it's handled by ILogHandler.LogException();
                return;
            }
            HandleLog(condition, stacktrace, type);
            _lastContext = null;
        }
        
        static void HandleLog(string condition, string stacktrace, LogType type, Object context = null)
        {
            var options = GetLevelOption(type) | NjLogger.Options.FromUnity;
            object stacktraceObj = stacktrace;
            if (string.IsNullOrEmpty(stacktrace) && NjLogger.IsStackTraceEnabled(options))
            {
                stacktraceObj = new StackTrace(7, true);
            }
            _strValues ??= new StrValue[2];
            Span<StrValue> values;
            if (SendToChannel)
            {
                _strValues[0] = ChannelName;
                _strValues[1] = condition;
                values = new Span<StrValue>(_strValues, 0, 2);
                options |= NjLogger.Options.IncludesChannel;
            }
            else
            {
                _strValues[0] = condition;
                values = new Span<StrValue>(_strValues, 0, 1);
            }
            NjLogger.HandleUnityLog(new NjLogger.LogRow()
            {
                Values = values,
                StackTrace = stacktraceObj,
                Context = context,
                Options = options
            });
            Array.Clear(_strValues, 0, _strValues.Length);
        }
        
        [ThreadStatic]
        static StrValue[] _strValues;

        public static NjLogger.Options GetLevelOption(LogType logType)
        {
            switch (logType)
            {
                case LogType.Log:
                    return NjLogger.Options.Info;
                case LogType.Warning:
                    return NjLogger.Options.Warn;
                default:
                    return NjLogger.Options.Error;
            }
        }
    }
    
}