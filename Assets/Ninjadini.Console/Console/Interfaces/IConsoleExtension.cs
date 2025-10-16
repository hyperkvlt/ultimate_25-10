namespace Ninjadini.Console
{
    /// Warning: Just because you extend from this interface it doesn't start automatically.
    /// 1. Add `[System.Serializable]` attribute on the class to serialize the fields.
    /// 2. Ensure it is a basic data class and not a Unity Object type like MonoBehaviour or ScriptableObject
    /// 3. Add the module in Project Settings > NjConsole > Extension Modules.
    public interface IConsoleExtension : IConsoleModule
    {
    }
}