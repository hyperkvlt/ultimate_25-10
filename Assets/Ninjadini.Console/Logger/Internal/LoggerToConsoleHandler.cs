using System;

namespace Ninjadini.Logger.Internal
{
    public class LoggerToConsoleHandler : NjLogger.IHandler
    {
        public void HandleLog(ref NjLogger.LogRow logRow)
        {
            switch (logRow.Options.GetLevel())
            {
                case 0:
                    System.Console.Write("Debug:\t");
                    break;
                case NjLogger.Level.Info:
                    System.Console.Write("Info:\t");
                    break;
                case NjLogger.Level.Warn:
                    System.Console.Write("Warn:\t");
                    break;
                case NjLogger.Level.Error:
                    System.Console.Write("Error:\t");
                    break;
            }
            System.Console.WriteLine(logRow.GetString(LoggerUtils.TempStringBuilder));
        }

        public void HandleException(Exception exception, ref NjLogger.LogRow logRow)
        {
            System.Console.Write("Exception:\t");
            System.Console.WriteLine(exception.ToString());
        }
    }
}