#if !NJCONSOLE_DISABLE
using System.ComponentModel;

namespace Ninjadini.Console.UI
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class StandardStorageKeys
    {
        public const string Windowed = "njcWindowed";
        public const string OverlayScale = "njcScale";
        public const string OverlaySafeArea = "njcSafeArea";
        public const string LastPanelTypeName = "njcLastPanel";
        public const string LockedPanelTypeName = "njcWindowPanelLocked";
        public const string LogsFilterCurrent = "njcLogs.filter.current";
        public const string LogsFilterSaves = "njcLogs.filter.saves";
        public const string LogsTimestampDisplay = "njcLogs.timestamp";
        public const string CmdLineShown = "njcCmdLine.Shown";
        public const string CmdLineHistory = "njcCmdLine.History";
        public const string OptionsSearch = "njcOpts.Search";
        public const string OptionsHistory = "njcOpts.H";

        public const string AutoShowPrevious = "njcShortcut.auto";
        public const string ShortcutLastSlot = "njcShortcut.lastSlot";
        public const string ShortcutSlotPrefix = "njcShortcut.slot";
        
        public const string InfoLastScreen = "njcInfo.last";
        public const string PlayerPrefHistory = "njcPlayerPrefHistory";

    }
}
#endif