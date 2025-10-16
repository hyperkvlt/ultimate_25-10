using System;
using UnityEngine.UIElements;

namespace Ninjadini.Console
{
    /// Static convenience accessor to console's build-in modules and runtime overlay.
    public static class NjConsole
    {
        static ConsoleModules _modules;
        
        /// <summary>
        /// Access to Console Options to add option menu items in console window's 'Options' tab.<br/>
        /// This is for runtime items only, if you want to add menu items for edit mode, use 'EditModeOptions'.
        /// Example code:
        /// <code>
        /// var catalog = NjConsole.Options.CreateCatalog();
        /// catalog.AddButton("My First Button", () => Debug.Log("Clicked my first button"));
        /// catalog.AddButton("My Space Bound Button", () => Debug.Log("Clicked my space bound button"))
        ///   .BindToKeyboard(KeyCode.Space)
        ///   .AutoCloseOverlay();
        /// // ^ Here we also bind to keyboard and set it to auto close the overlay on click (if it's a button)
        /// 
        /// catalog.RemoveAll();
        /// // ^ Call to remove both buttons when no longer needed.
        /// </code>
        /// </summary>
        public static ConsoleOptions Options => Modules.GetOrCreateModule<ConsoleOptions>();
        
        /// <summary>
        /// Access to Edit mode version of Console Options. Add option menu items in console window's 'Editor Options' tab.<br/>
        /// Example:
        /// <code>
        /// var catalog = NjConsole.EditModeOptions.CreateCatalog();
        /// catalog.AddButton("My Editor Button", () => Debug.Log("Clicked my editor button"));
        /// </code>
        /// </summary>
        public static ConsoleOptionsForEditMode EditModeOptions => Modules.GetOrCreateModule<ConsoleOptionsForEditMode>();
        
        /// <summary>
        /// Access to CommandLine dedicated version of Console Options -  invisible in UI.<br/>
        /// By default, other console options types will automatically show up in commandline (everything shows up mixed).<br/>
        /// To prefix default option modules to a child group in CommandLine, set `NjConsole.Options.CommandLinePath = "child group name here"`. <br/>
        /// To hide other option modules in CommandLine, set `CommandLinePath` to null. e.g. `NjConsole.Options.CommandLinePath = null`.
        /// Example:
        /// <code>
        /// var catalog = NjConsole.CommandLineOptions.CreateCatalog();
        /// catalog.AddButton("myCommand", () => Debug.Log("Clicked my editor button"));
        /// </code>
        /// </summary>
        public static ConsoleOptionsForCommands CommandLineOptions => Modules.GetOrCreateModule<ConsoleOptionsForCommands>();
        
        /// <summary>
        /// Access to key bindings module<br/>
        /// By default, key bindings are disabled in player builds. You can change that in Project Settings > Ninjadini Console > Features > In Player Key Bindings<br/>
        /// Normally you set up the keybinding when setting up the ConsoleOptions.
        /// Example:
        /// <code>
        /// NjConsole.KeyBindings.BindKeyDown(() => Debug.Log("C key was pressed"), Key.C);
        /// NjConsole.KeyBindings.BindKeyDown(() => Debug.Log("Shift + C key was pressed"), Key.C, ConsoleKeyBindings.Modifier.Shift);
        /// </code>
        /// </summary>
        public static ConsoleKeyBindings KeyBindings => Modules.GetOrCreateModule<ConsoleKeyBindings>();
        
        /// <summary>
        /// Access to shared ConsoleModules<br/>
        /// Central place where all console features/modules are registered. - see ConsoleModules class doc for details.
        /// </summary>
        public static ConsoleModules Modules
        {
            get
            {
                if (_modules == null)
                {
                    var settings = ConsoleSettings.Get();
                    _modules ??= new ConsoleModules(settings);
                }
                return _modules;
            }
            internal set => _modules = value;
        }

        /// <summary>
        /// Access to check if console modules have been created
        /// </summary>
        public static bool HasStartedModules => _modules != null;
        
        /// Static convenience accessor to ConsoleOverlay<br/>
        /// ConsoleOverlay is the console UI that shows up in runtime. For editor console, see NjConsoleEditorWindow
        public static class Overlay
        {
#if !NJCONSOLE_DISABLE
            /// Ensure console overlay is started and waiting for activating triggers.
            /// You need to call this manually if you don't have autoStartOverlay turned on in settings.
            public static void EnsureStarted()
            {
                if (!HasOverlayInstance)
                {
                    ConsoleOverlay.GetOrCreateInstance().WaitForTriggersToShow();
                }
            }

            /// Access to determine if the overlay instance exists
            public static bool HasOverlayInstance => ConsoleOverlay.HasInstance;
            
            /// Get access to ConsoleOverlay instance.
            public static ConsoleOverlay Instance => ConsoleOverlay.Instance;
            
            /// Show the overlay without showing the access challenge even if the user haven't passed it.
            /// If overlay did not exist, this call will auto create it first.
            /// See settings to set up access challenge.
            public static void Show() => ConsoleOverlay.GetOrCreateInstance().ShowWithoutAccessChallenge();
            
            /// Show the overlay. If the user still need to pass the access challenge it will show the challenge screen instead.
            /// If overlay did not exist, this call will auto create it first.
            /// See settings to set up access challenge.
            public static void ShowWithAccessChallenge() => ConsoleOverlay.GetOrCreateInstance().ShowWithAccessChallenge();

            /// hide the overlay if it exists and showing.
            public static void Hide() => ConsoleOverlay.Instance?.Hide();
            
            /// Determine if the overlay window is showing.
            /// This will not return true if the user is on the access challenge screen.
            /// You can call ShowingAccessChallengeChallenge() if you need to check.
            public static bool Showing => ConsoleOverlay.Instance?.Showing ?? false;
            
            /// <summary>
            /// Show the panel of module T. This only sets the active panel, it doesn't force the overlay to show.
            /// </summary>
            /// <typeparam name="T">The panel module to show</typeparam>
            /// <returns>Returns false if overlay instance doesn't exist or the panel doesn't exist.</returns>
            public static bool SetActivePanel<T>() where T : IConsolePanelModule
            {
                return ConsoleOverlay.Instance?.Window?.SetActivePanel<T>() ?? false;
            }
            
            /// <summary>
            /// Show the panel of module type. This only sets the active panel, it doesn't force the overlay to show.
            /// </summary>
            /// <param name="panelModule">a type of IConsolePanelModule to show</param>
            /// <returns>Returns false if overlay instance doesn't exist or the panel doesn't exist.</returns>
            public static bool SetActivePanel(Type panelModule)
            {
                return ConsoleOverlay.Instance?.Window?.SetActivePanel(panelModule) ?? false;
            }

            /// <summary>
            /// Show the panel of module type. This only sets the active panel, it doesn't force the overlay to show.
            /// </summary>
            /// <param name="panelName">Name of the panel on the side-bar</param>
            /// <returns>Returns false if overlay instance doesn't exist or the panel doesn't exist.</returns>
            public static bool SetActivePanel(string panelName)
            {
                return ConsoleOverlay.Instance?.Window?.SetActivePanel(panelName) ?? false;
            }
            
            /// Access to the currently active panel module.
            public static IConsolePanelModule ActivePanel => ConsoleOverlay.Instance?.Window?.ActivePanel;

            /// Access to the currently active panel UI element.
            public static VisualElement ActivePanelElement => ConsoleOverlay.Instance?.Window?.ActivePanelElement;
            
            /// <summary>
            /// Console context of the overlay.
            /// See the class document of ConsoleContext for more details.
            /// </summary>
            public static ConsoleContext Context => ConsoleOverlay.Instance?.Context;
            
            /// Access to main Console window
            public static Ninjadini.Console.UI.ConsoleWindow Window => ConsoleOverlay.Instance?.Window;
            
            // Determine if the user will need to do access challenge
            public static bool IsAccessChallengeRequired() => ConsoleOverlay.Instance?.IsAccessChallengeRequired() ?? true;

            // Determine if the access challenge is showing right now
            public static bool ShowingAccessChallengeChallenge() => ConsoleOverlay.Instance?.ShowingAnyChallenge() ?? false;

            /// Destroy the console overlay.<br/>
            /// This will stop all runtime console features, such as KeyBindings and shortcuts.
            /// You can create the console overlay again by calling EnsureStarted().<br/>
            /// Destroying the overlay does not stop the NjLogger logging history to stop recording.
            public static void Destroy()
            {
                if (ConsoleOverlay.HasInstance)
                {
                    UnityEngine.Object.Destroy(ConsoleOverlay.Instance.GameObject);
                }
            }
#else
            /// Does nothing. Console is disabled.
            public static void EnsureStarted() { }

            /// Returns false. Console is disabled.
            public static bool HasOverlayInstance => false;
            
            /// Returns a stub ConsoleOverlay because Console is disabled.
            public static ConsoleOverlay Instance => ConsoleOverlay.Instance;

            /// Does nothing. Console is disabled.
            public static void Show() { }
            
            /// Does nothing. Console is disabled.
            public static void ShowWithAccessChallenge() { }

            /// Does nothing. Console is disabled.
            public static void Hide() { }
            
            /// Returns false. Console is disabled.
            public static bool Showing => false;
            
            /// Does nothing. Returns false. Console is disabled.
            public static bool SetActivePanel<T>() where T : IConsolePanelModule
            {
                return false;
            }
            
            /// Does nothing. Returns false. Console is disabled.
            public static bool SetActivePanel(Type panelModule)
            {
                return false;
            }

            /// Does nothing. Returns false. Console is disabled.
            public static bool SetActivePanel(string panelName)
            {
                return false;
            }
            
            /// Returns null. Console is disabled.
            public static IConsolePanelModule ActivePanel => null;

            /// Returns null. Console is disabled.
            public static VisualElement ActivePanelElement => null;
            
            /// Returns null. Console is disabled.
            public static ConsoleContext Context => null;
            
            /// Returns false. Console is disabled.
            public static bool IsAccessChallengeRequired() => false;

            /// Returns false. Console is disabled.
            public static bool ShowingAccessChallengeChallenge() => false;

            /// Does nothing. Console is disabled.
            public static void Destroy() { }
#endif
        }
    }
}
#if !UNITY_2022_1_OR_NEWER
#error (｡•́︿•̀｡) Sorry! NjConsole needs Unity 2022.1+ because it relies on UI Toolkit features only fully supported from that version. (╯°□°)╯︵ ┻━┻
#endif