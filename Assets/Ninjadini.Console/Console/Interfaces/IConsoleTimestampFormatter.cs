using System.Text;
using Ninjadini.Logger;

namespace Ninjadini.Console
{
    /// <summary>
    /// This allows you to have your own custom timestamp display in console's logs.<br/>
    /// 1. First create a class that implements both IConsoleTimestampFormatter and IConsoleExtension
    /// 2. Add [Serializable] attribute to the class.
    /// <code>
    /// public class TimestampAsMsSince : IConsoleTimestampFormatter, IConsoleExtension
    /// {
    ///   public void AppendFormatted(LogLine log, StringBuilder stringBuilder)
    ///   {
    ///     var timeSince = log.Time - ConsoleInfoModule.LocalTimeAtStart; 
    ///     // ^ get the time since start (we can't justg use Time.realtimeSinceStartup because we only know the time the log was made)
    ///     NjLoggerUtils.AppendNum(stringBuilder, (long)timeSince.TotalMilliseconds, true);
    ///     // ^ append the total milliseconds to the string builder. the utility function is a way to add numbers to string without allocating.
    ///   }
    /// }
    /// </code>
    /// 3. Go to project settings > NjConsole > Extension Modules > add your new class in the list.<br/>
    /// 4. In console's logs panel, click the timestamp dropdown on top right, choose `Custom Module`.<br/>
    /// Note. if you have multiple IConsoleTimestampFormatter modules added, it might not pick the right one.
    /// </summary>
    public interface IConsoleTimestampFormatter
    {
        void AppendFormatted(LogLine log, StringBuilder stringBuilder);
    }
}