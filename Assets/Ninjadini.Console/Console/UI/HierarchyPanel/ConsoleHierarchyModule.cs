#if !NJCONSOLE_DISABLE
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public class ConsoleHierarchyModule : IConsolePanelModule
    {
        public bool ShowInEditMode = true;
        
        public float SideBarOrder { get; set; } = 3;

        public bool PanelFeatureEnabled { get; private set; }
        
        string IConsolePanelModule.Name => "Hierarchy";
        
        bool IConsoleModule.PersistInEditMode => ShowInEditMode;

        void IConsoleModule.OnAdded(ConsoleModules modules)
        {
            PanelFeatureEnabled = Application.isEditor || modules.Settings.inPlayerHierarchyPanel;
        }

        VisualElement IConsolePanelModule.CreateElement(ConsoleContext context)
        {
            return new ConsoleHierarchyPanel(context);
        }
    }
}
#endif