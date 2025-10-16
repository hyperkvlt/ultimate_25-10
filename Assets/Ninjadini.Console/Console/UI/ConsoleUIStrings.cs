#if !NJCONSOLE_DISABLE
using System.ComponentModel;

namespace Ninjadini.Console.Internal
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class ConsoleUIStrings
    {
        public const string OptsShowShortcuts = "Shortcuts";
        public const string OptsPlaceShortcutHint = "Press and hold an item to create a shortcut";
        public const string CopiedToClipboard = "Copied to clipboard";
        public const string CopiedToClipboardMayNotWork = "Copied to clipboard\n(but it may not work on this platform)";
        public const string TooManyBacklog = "Because you have one of the console windows scrolled up, we have many logs ({number}) cached in memory, please scroll to bottom to reduce performacne impact.";
        public const string TooManyBacklogIgnore = "Ignore";
        
        public const string LogsNeedSaveSearchName = "Enter save name first";
        public const string LogsSavedSearch = "Search terms saved to `{name}`";
        public const string LogsLoadedSearch = "Search terms loaded from `{name}`";
        public const string LogsDeletedSearch = "Search terms `{name}` deleted";
        public const string LogsFilterInfo = "Info";
        public const string LogsFilterWarn = "Warn";
        public const string LogsFilterError = "Error";
        public const string LogsSearch = "Search";
        public const string LogsChannels = "Channels";
        public const string LogsAddTerm = "Add";
        public const string LogsSave = "Save";
        public const string LogsLoad = "Load";
        public const string LogsDelete = "Delete";
        public const string LogsSearchEnabled = "Search enabled";
        
        public const string LogsChAllTooltip = "Remove channel filtering";
        public const string LogsChNoChTooltip = "Filter to logs with no channel";
        public const string LogsChNoChannelsHelpPlayer = "No channels found in logs yet.";
        public const string LogsChNoChannelsHelpEditor = "No channels found in logs yet. This is how you log to a channel:\n\n<color=#c5e1a5>LogChannel _logs = new LogChannel(\"MyChannel\");\nvoid SayHi()\n{\n   _logs.Info(\"Hello world\");\n}</color>";
        
        public static readonly string OptsHowToSetup = $"This is how you set up:\n\n<color=#c5e1a5>var options = {nameof(NjConsole)}.{nameof(NjConsole.Options)}.{nameof(ConsoleOptions.CreateCatalog)}();\n" +
                                                       "options.AddButton(\"SayHello\", () => {\n" +
                                                       "   Debug.Log(\"Hello world\");\n" +
                                                       "})\n" +
                                                       ".BindToKeyboard(KeyCode.H);</color> // < this binds it to keyboard shortcut 'H' - editor only.";
        
        
        public const string InfoFpsMonitor = "FPS\nMonitor";
        public const string InfoMemoryMonitor = "Memory\nMonitor";
        public const string InfoGc = "GC\n.Collect()";
        public const string InfoNoRuntimeOverlay = "No runtime console overlay active.\n(this feature doesn't work in console editor window yet)";
        public const string InfoEmailTextLog = "Email\nText Logs";
        public const string InfoExportTextLog = "Export\nText Logs";
        public const string InfoCopyTextLog = "Copy\nText Logs";
        //public const string InfoExportHtmlLog = "Export\nHTML Logs";
        
        public const string PlayerPrefsDeleteAll = "Delete All";
        public const string PlayerPrefsDeleteAll1 = "Are you sure you want to delete all?\nThis could be destructive...";
        public const string PlayerPrefsDeleteAll2 = "Really really sure?";
        

        public const string InspectReadProp = "Reading a property might change some states.\nFor example, Renderer.material duplicates the sharedMaterial.\nThis is why we avoid auto reading some properties.";

        public const string GraphFpsAvg = "avg";
        public const string GraphFpsMin = "min";
        public const string GraphMemManaged = "managed";
        public const string GraphMemNative = "native";
        public const string GraphMoveInfo = "Press and hold on the monitor to enter shortscuts edit mode.\nYou can then move or remove items from shortcuts screen.";
        
        public const string ShortcutsAlreadyExists = "Item already exists";
        public const string ShortcutsMaxReached = "Maximum number of shortcuts ({MaxItems}) reached.";
    }
}
#endif