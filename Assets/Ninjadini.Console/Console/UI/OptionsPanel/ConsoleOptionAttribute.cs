using System;

namespace Ninjadini.Console
{ 
    /// <summary>
    /// Add controls to Console options menu.
    /// This is the alternative path to creating menus manually.
    /// Example code:
    ///<code>
    /// 
    /// public class TestOptions : MonoBehaviour {
    ///
    /// void Start() {
    ///     NjConsole.Options.CreateCatalogFrom(this, "TestOptions");
    ///     // ^ second param `TestOptions` is optional, it puts all the items inside the `TestOptions` group in this example.
    ///     // If the item being created from is a MonoBehaviour, it will automatically remove the options when the monobehaviour is destroyed.
    /// }
    /// 
    /// [ConsoleOption]
    /// void SayHello() {
    ///     Debug.Log("Hello");
    /// }
    ///
    /// [ConsoleOption("Set Player Name")]
    /// public void SetName(string newName) {
    ///     Debug.Log("SetName caleld: " + newName);
    /// }
    ///
    /// [ConsoleOption(key:UnityEngine.InputSystem.Key.I, header:"Player")] // bind to keyboard key I
    /// bool InfiniteLives;
    ///
    /// [ConsoleOption(increments:1, header:"Player")]
    /// int Health {get; set;}
    ///
    /// [ConsoleOption(increments:1, header:"Player")]
    /// [Range(1, 10)]
    /// float Speed;
    ///
    /// }
    /// 
    /// </code>
    /// </summary>
    /// <param name="path">Path of the button. You can put in child directories by separating with '/'. e.g. ADirectory/Child Directory/Child Button</param>
    /// <param name="callback">The action to call when button is pressed</param>
    /// <returns>A chain result so you can add Key bindings or auto close to that button. e.g. '.BindToKeyboard(KeyCode.Space).AutoCloseOverlay();'</returns>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public class ConsoleOptionAttribute : Attribute
    {
        public readonly string Path;
        public readonly string Header;
        public readonly double Increments;
        
        /// Keybinding for editor - this only works for button and toggle types.
#if ENABLE_LEGACY_INPUT_MANAGER
        public UnityEngine.KeyCode Key;
#elif ENABLE_INPUT_SYSTEM
// if you are seeing an error here, it means you have enabled the new input system but haven't installed the package.
// Install InputSystem package in package manager.
// Alternatively, go to Project Settings > Player > Other Settings > Active Input Handling > choose Input Manager (old)
        public UnityEngine.InputSystem.Key Key;
#endif
        public ConsoleKeyBindings.Modifier KeyModifier;
        
        /// Warning: this only works for button and toggle types.
        public bool AutoClose;

        public ConsoleOptionAttribute(string path = null, 
            string header = null, 
            double increments = 0, 
            
#if ENABLE_LEGACY_INPUT_MANAGER
            UnityEngine.KeyCode key = 0, 
#elif ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.Key key = 0, 
#endif
            
            ConsoleKeyBindings.Modifier keyModifier = 0,
            bool autoClose = false)
        {
            Path = path;
            Header = header;
            Increments = increments;
#if ENABLE_LEGACY_INPUT_MANAGER || ENABLE_INPUT_SYSTEM
            Key = key;
#endif
            KeyModifier = keyModifier;
            AutoClose = autoClose;
        }
    }
}