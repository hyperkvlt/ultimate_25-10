using UnityEngine;

namespace Ninjadini.Console
{
    public partial class ConsoleOptions
    {
        
#if !NJCONSOLE_DISABLE
        public ref struct ItemResult
        {
            internal OptionItem OptionItem;

            internal ItemResult(OptionItem optionItem)
            {
                OptionItem = optionItem;
            }
            
            public ItemResult SetHeader(string header)
            {
                if (OptionItem != null)
                {
                    OptionItem.Header = header;
                }
                return this;
            }

            public ItemResult SetTooltip(string tooltip)
            {
                if (OptionItem != null)
                {
                    OptionItem.Tooltip = tooltip;
                }
                return this;
            }
        }

        public ref struct ItemResultWithKeyBind
        {
            internal ConsoleModules Console;
            internal OptionItem OptionItem;
            
#if ENABLE_LEGACY_INPUT_MANAGER
            public ItemResultWithKeyBind BindToKeyboard(KeyCode key, ConsoleKeyBindings.Modifier modifiers = 0)
#else
// if you are seeing an error here, it means you have enabled the new input system but haven't installed the package.
// Install InputSystem package in package manager.
// Alternatively, go to Project Settings > Player > Other Settings > Active Input Handling > choose Input Manager (old)
            public ItemResultWithKeyBind BindToKeyboard(UnityEngine.InputSystem.Key key, ConsoleKeyBindings.Modifier modifiers = 0)
#endif
            {
                if (Console != null && OptionItem != null)
                {
                    var keyBindings = Console.GetOrCreateModule<ConsoleKeyBindings>();
                    OptionItem.BindToKeyboard(keyBindings, key, modifiers);
                }
                return this;
            }
            
            public ItemResultWithKeyBind AutoCloseOverlay()
            {
                if (OptionItem is ButtonItem btnItem)
                {
                    btnItem.AutoCloseOverlay = true;
                }
                else if (OptionItem is ToggleItem toggleItem)
                {
                    toggleItem.AutoCloseOverlay = true;
                }
                return this;
            }
            
            public ItemResultWithKeyBind SetHeader(string header)
            {
                if (OptionItem != null)
                {
                    OptionItem.Header = header;
                }
                return this;
            }

            public ItemResultWithKeyBind SetTooltip(string tooltip)
            {
                if (OptionItem != null)
                {
                    OptionItem.Tooltip = tooltip;
                }
                return this;
            }
        }
#else
        public ref struct ItemResult
        {
            public ItemResult SetHeader(string header) => this;
            public ItemResult SetTooltip(string tooltip) => this;
        }

        public ref struct ItemResultWithKeyBind
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            public ItemResultWithKeyBind BindToKeyboard(KeyCode key, ConsoleKeyBindings.Modifier modifiers = 0)
#else
            public ItemResultWithKeyBind BindToKeyboard(UnityEngine.InputSystem.Key key, ConsoleKeyBindings.Modifier modifiers = 0)
#endif
            {
                return this;
            }
            
            public ItemResultWithKeyBind AutoCloseOverlay() => this;
            
            public ItemResultWithKeyBind SetHeader(string header) => this;

            public ItemResultWithKeyBind SetTooltip(string tooltip) => this;
        }
#endif
    }
}