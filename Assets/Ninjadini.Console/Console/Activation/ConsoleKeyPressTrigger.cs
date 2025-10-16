using System;
using System.ComponentModel;
using UnityEngine;

namespace Ninjadini.Console.Internal
{
    [Serializable]
    [DisplayName("Press a keyboard key")]
    [Tooltip("Combine with shift key to force show the Console auto focused to Commandline")]
    public class ConsoleKeyPressTrigger : IConsoleOverlayTrigger
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        [Tooltip("Keyboard key to press - This is used if Unity's new input system is enabled and legacy is disabled.")]
        public UnityEngine.InputSystem.Key key = UnityEngine.InputSystem.Key.Backquote;
#endif
        [Tooltip("Keyboard key to press - keyCode value is used if Unity's legacy input manager is enabled.")]
        public KeyCode keyCode = KeyCode.BackQuote;

        public bool TriggerInPlayerBuilds = true;

#if !NJCONSOLE_DISABLE 
        public void ListenForTriggers(ConsoleOverlay overlay)
        {
            if (!TriggerInPlayerBuilds && !Application.isEditor)
            {
                return;
            }
            var cmp = overlay.GameObject.AddComponent<ConsoleKeyPressListenerComponent>();
            cmp.trigger = this;
            cmp.Overlay = overlay;
        }

        public class ConsoleKeyPressListenerComponent : MonoBehaviour
        {
            public ConsoleKeyPressTrigger trigger;
            public ConsoleOverlay Overlay;

            void Update()
            {
                if (trigger == null) return;
                var shiftPressed = false;
#if ENABLE_LEGACY_INPUT_MANAGER
                if (!Input.GetKeyDown(trigger.keyCode))
                {
                    return;
                }
                shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#elif ENABLE_INPUT_SYSTEM
                var pressed = UnityEngine.InputSystem.Keyboard.current?[trigger.key].wasPressedThisFrame ?? false;
                if (!pressed)
                {
                    return;
                }
                shiftPressed = UnityEngine.InputSystem.Keyboard.current?.shiftKey?.isPressed ?? false;
#else
                return;
#endif
                if (ConsoleKeyBindings.IsUserTyping(Overlay))
                {
                    return;
                }
                if (shiftPressed)
                {
                    Overlay.ShowWithAccessChallenge();
                    if (Overlay.Window != null)
                    {
                        var cmdElement = Overlay.Window.OpenAndFocusOnCommandLine();
                        cmdElement?.schedule.Execute(() =>
                        {
                            cmdElement.SetInputAndSelectEnd("");
                        }).ExecuteLater(5);
                    }
                }
                else
                {
                    Overlay.Toggle();
                }
            }
        }
#endif
    }
}