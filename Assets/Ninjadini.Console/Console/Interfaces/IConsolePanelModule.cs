using System;
using Ninjadini.Logger;
using UnityEngine.UIElements;

namespace Ninjadini.Console
{
    /// <summary>
    /// Add your own custom panel.<br/>
    /// You can use IConsoleExtension to auto start or start manually like this:<br/>
    /// NjConsole.Modules.AddModule(new DemoPanelModule());<br/>
    /// See documentation for more info about IConsoleExtension.
    /// <code>
    /// public class DemoPanelModule : IConsolePanelModule
    /// {
    ///  public string Name => "DemoPanel";
    /// 
    ///  public float SideBarOrder => 12;
    /// 
    ///  public VisualElement CreateElement(ConsoleContext context)
    ///  {
    ///      return new BasicDemoPanel();
    ///  }
    /// }
    /// 
    /// class BasicDemoPanel : VisualElement
    /// {
    ///     public BasicDemoPanel()
    ///     {
    ///         AddToClassList("panel"); // just adds a background
    ///         Add(new Label("Hello from demo panel"));
    ///     }
    /// }
    /// </code>
    /// </summary>
    public interface IConsolePanelModule : IConsoleModule
    {
        /// <summary>
        /// Name to display on the side panel
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// The order of the button to appear on the side panel.<br/>
        /// It is the actual order, so 1, 2, 3, 4 will show up in top to bottom order.<br/>
        /// Why is it a float value? It is in case you want to put your custom panel in between the build-in panels, for example, between logs and options would be 0.5.
        /// </summary>
        float SideBarOrder { get; }

        bool PanelFeatureEnabled => true;

        /// <summary>
        /// Create the VisualElement for the panel - this is for that specific ConsoleContext.
        /// Remember you can have multiple editor console windows and at runtime too. The context will be unique per window.
        /// </summary>
        VisualElement CreateElement(ConsoleContext context);

        VisualElement CreateSideButton(ConsoleContext context, Action clickedCallback)
        {
            return CreateDefaultSideButton(context, clickedCallback);
        }
        
        VisualElement CreateDefaultSideButton(ConsoleContext context, Action clickedCallback)
        {
            var btn = new Button(clickedCallback);
            btn.AddToClassList("main-sidebar-btn");
            var name = Name ?? GetType().Name;
            btn.text = name;
            if (LoggerUtils.GetFirstLineBreakIndex(name) >= 1)
            {
                btn.style.fontSize = 10;
            }
            return btn;
        }

        /// Your panel element can optionally implement this interface to get callbacks from Console window.
        public interface IElement
        {
            /// user clicked on the panel button even tho it was already selected. usually we reset the panel to initial state.
            void OnReselected(){ }
        }
    }
}