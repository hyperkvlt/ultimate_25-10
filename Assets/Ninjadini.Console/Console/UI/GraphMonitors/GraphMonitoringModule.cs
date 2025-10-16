#if !NJCONSOLE_DISABLE
using Ninjadini.Console.Internal;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public class GraphMonitoringModule : IConsoleModule, ConsoleScreenShortcuts.IRestorable
    {
        const string RestorePathFps = "fps";
        const string RestorePathMemory = "memory";
        
        string ConsoleScreenShortcuts.IRestorable.FeatureName => "graphMonitors";

        VisualElement ConsoleScreenShortcuts.IRestorable.TryRestore(ConsoleContext context, string path)
        {
            if (path == RestorePathFps)
            {
                return new FpsMonitorElement();
            }
            if (path == RestorePathMemory)
            {
                return new MemoryMonitorElement();
            }
            return null;
        }

        public void AddFpsToShortcuts(ConsoleContext context)
        {
            var shortcuts = context.RuntimeOverlay?.Shortcuts;
            if (shortcuts != null)
            {
                shortcuts.AddNewItem(this, RestorePathFps);
                ConsoleToasts.Show(context, ConsoleUIStrings.GraphMoveInfo);
            }
        }

        public void AddMemoryToShortcuts(ConsoleContext context)
        {
            var shortcuts = context.RuntimeOverlay?.Shortcuts;
            if (shortcuts != null)
            {
                shortcuts.AddNewItem(this, RestorePathMemory);
                ConsoleToasts.Show(context, ConsoleUIStrings.GraphMoveInfo);
            }
        }

        uint ConsoleScreenShortcuts.IRestorable.ChangeIndex => 0;
    }
}
#endif