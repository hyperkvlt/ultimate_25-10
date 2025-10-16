using System;
using UnityEngine;

namespace Ninjadini.Logger.Internal
{
    public class LoggerToUnityHandler : NjLogger.IHandler
    {
        public void HandleLog(ref NjLogger.LogRow logRow)
        {
            switch (logRow.Options.GetLevel())
            {
                case 0:
                case NjLogger.Level.Info:
                    Debug.Log(logRow.GetString(LoggerUtils.TempStringBuilder), logRow.Context as UnityEngine.Object);
                    break;
                case NjLogger.Level.Warn:
                    Debug.LogWarning(logRow.GetString(LoggerUtils.TempStringBuilder), logRow.Context as UnityEngine.Object);
                    break;
                case NjLogger.Level.Error:
                    Debug.LogError(logRow.GetString(LoggerUtils.TempStringBuilder), logRow.Context as UnityEngine.Object);
                    break;
            }
        }

        public void HandleException(Exception exception, ref NjLogger.LogRow logRow)
        {
            Debug.LogException(exception, logRow.Context as UnityEngine.Object);
        }
    }
}