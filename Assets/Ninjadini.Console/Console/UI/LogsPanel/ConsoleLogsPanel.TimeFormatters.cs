#if !NJCONSOLE_DISABLE
using System.Collections.Generic;
using System.Text;
using Ninjadini.Logger;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleLogsPanel
    {
        static readonly List<string> TimeStampFormats = new List<string>()
        {
            "No Timestamp",
            "Since start",
            "HH:MM:SS.ms",
            "UTC HH:MM:SS.ms",
            "Custom Module"
        };

        static readonly List<IConsoleTimestampFormatter> DefaultFormats = new List<IConsoleTimestampFormatter>()
        {
            null,
            new TimeSinceStart(),
            new ClockTimeFormatter(false),
            new ClockTimeFormatter(true),
            null
        };
        
        public class ClockTimeFormatter : IConsoleTimestampFormatter
        {
            readonly bool _utcTime;
            public ClockTimeFormatter(bool utcTime)
            {
                _utcTime = utcTime;
            }
            
            public void AppendFormatted(LogLine log, StringBuilder stringBuilder)
            {
                var time = _utcTime ? log.Time.ToUniversalTime() : log.Time;
                LoggerUtils.AppendNumWithZeroPadding(stringBuilder, time.Hour, 2);
                stringBuilder.Append(":");
                LoggerUtils.AppendNumWithZeroPadding(stringBuilder, time.Minute, 2);
                stringBuilder.Append(":");
                LoggerUtils.AppendNumWithZeroPadding(stringBuilder, time.Second, 2);
                stringBuilder.Append(".");
                LoggerUtils.AppendNumWithZeroPadding(stringBuilder, time.Millisecond, 3);
            }
        }
        
        public class TimeSinceStart : IConsoleTimestampFormatter
        {
            public void AppendFormatted(LogLine log, StringBuilder stringBuilder)
            {
                var time = log.Time - ConsoleUtilitiesModule.LocalTimeAtStart;

                if (time.TotalHours >= 1)
                {
                    LoggerUtils.AppendNum(stringBuilder, (int)time.TotalHours);
                    stringBuilder.Append(":");
                }
                LoggerUtils.AppendNumWithZeroPadding(stringBuilder, time.Minutes, 2);
                stringBuilder.Append(":");
                LoggerUtils.AppendNumWithZeroPadding(stringBuilder, time.Seconds, 2);
                stringBuilder.Append(".");
                LoggerUtils.AppendNumWithZeroPadding(stringBuilder, time.Milliseconds, 3);
            }
        }
    }
}
#endif