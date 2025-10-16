#if !NJCONSOLE_DISABLE
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleUtilitiesModule : IConsolePanelModule
    {
        public static DateTime LocalTimeAtStart { get; internal set; }
        
        public float SideBarOrder { get; set; } = 4;
        
        string IConsolePanelModule.Name => "Utilities";

        public bool PanelFeatureEnabled { get; private set; }
        
        bool IConsoleModule.PersistInEditMode => true;

        void IConsoleModule.OnAdded(ConsoleModules modules)
        {
            PanelFeatureEnabled = Application.isEditor || modules.Settings.inPlayerUtilitiesPanel;
        }

        VisualElement IConsolePanelModule.CreateElement(ConsoleContext context)
        {
            return new ConsoleUtilitiesElement(context);
        }
    }
}
#endif