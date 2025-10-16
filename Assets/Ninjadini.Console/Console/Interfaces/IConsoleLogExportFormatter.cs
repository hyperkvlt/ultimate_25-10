using System.Text;
using Ninjadini.Logger;

namespace Ninjadini.Console
{
    /// <summary>
    /// This allows you to have your own custom log export data when using Utilities > Options > Email/Export text logs.<br/>
    /// 1. First create a class that implements both IConsoleLogExportFormatter and IConsoleExtension.
    /// 2. Add [Serializable] attribute to the class.
    /// <code>
    /// [Serializable]
    /// public class ExampleLogExportDetailsAppender : IConsoleLogExportFormatter, IConsoleExtension
    /// {
    ///     public void AppendHeader(StringBuilder stringBuilder)
    ///     {
    ///         stringBuilder.AppendLine("Log generated at @ " + System.DateTime.Now);
    ///         stringBuilder.AppendLine("This is where you put some info, like whats the player's id, which scene they were at, etc...");
    ///     }
    ///     public void AppendFooter(StringBuilder stringBuilder)
    ///     {
    ///         stringBuilder.AppendLine("This is my footer text");
    ///     }
    /// }
    /// </code>
    /// 3. Go to project settings > NjConsole > Extension Modules > add your new class in the list.<br/>
    /// 4. Try it out via NjConsole > Utilities > Options > Email/Export text logs
    /// Note. if you have multiple IConsoleLogExportFormatter modules added, Header and footer will all get combined together, but only the first one with HasLogFormatter will be used to format the log.
    /// </summary>
    public interface IConsoleLogExportFormatter : IConsoleModule
    {
        void AppendHeader(StringBuilder stringBuilder);
        void AppendFooter(StringBuilder stringBuilder);

        /// Only the first formatter which return true will be used as the log formatter.
        /// AppendHeader and AppendFooter can stack with multiple modules.
        bool HasLogFormatter => false;
        
        // Optional, this only gets called if you return true to HasLogFormatte.
        /// Only the first formatter which return true will be used as the log formatter.
        void AppendFormatted(LogLine logLine, StringBuilder stringBuilder)
        {
        }
    }
}