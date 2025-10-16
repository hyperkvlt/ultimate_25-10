namespace Ninjadini.Console
{
    /// Console modules are sort of features/modules/plugins of console.<br/>
    /// Modules are added to ConsoleModules class (which is normally accessed via `NjConsole.Modules`).<br/>
    /// A module is central place to store the features, but not the state of UI.<br/>
    /// Even tho you can have multiple windows of console open, there will be just 1 module instance per type.<br/>
    /// If you are looking to store/minipulate the state of a particular window, see ConsoleContext or store directly in the view element.<br/>
    /// See IConsolePanelModule to add your own custom side-bar-linked-panel in console.
    public interface IConsoleModule
    {
        bool PersistInEditMode => false;

        void OnAdded(ConsoleModules modules)
        {
            
        }

        void Dispose()
        {
        }
    }
}