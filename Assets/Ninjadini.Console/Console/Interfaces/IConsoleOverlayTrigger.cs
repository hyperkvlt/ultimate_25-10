namespace Ninjadini.Console
{
    /// <summary>
    /// This allows you to have your own custom way to trigger the console overlay in play mode.<br/>
    /// The default triggers are done via ConsoleKeyPressTrigger and ConsolePressAndHoldTrigger, you can refer to them as example.<br/>
    /// 1. First create a class that implements both IConsoleOverlayTrigger and IConsoleExtension.<br/>
    /// 2. Add [Serializable] attribute to the class.<br/>
    /// 3. Go to project settings > NjConsole > Playmode Overlay > Add Trigger > add your new class<br/>
    /// Example code below toggles the overlay on shift + right mouse click.
    /// <code>
    /// [System.Serializable]
    /// public class ShiftRightClickConsoleTrigger : IConsoleOverlayTrigger, IConsoleExtension
    /// {
    ///     ConsoleOverlay _overlay;
    ///     public void ListenForTriggers(ConsoleOverlay overlay)
    ///     {
    ///         _overlay = overlay;
    ///         overlay.schedule.Execute(Update).Every(1);
    ///     }
    /// 
    ///     void Update()
    ///     {
    ///         // using old input manager...
    ///         if (Input.GetMouseButtonDown(1) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
    ///             _overlay.Toggle();
    ///         }
    ///     }
    /// }
    /// </code>
    /// </summary>
    public interface IConsoleOverlayTrigger : IConsoleModule
    {
#if !NJCONSOLE_DISABLE
        void ListenForTriggers(ConsoleOverlay overlay);
#endif
    }
}