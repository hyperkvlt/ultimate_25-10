using System;
using System.Collections.Generic;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
// if you are seeing an error here, it means you have enabled the new input system but haven't installed the package.
// Install InputSystem package in package manager.
// Alternatively, go to Project Settings > Player > Other Settings > Active Input Handling > choose Input Manager (old)
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace Ninjadini.Console
{
    public class ConsoleKeyBindings : IConsoleModule
    {
#if !NJCONSOLE_DISABLE
        ConsoleOverlay _overlay;
        bool _disabled;
#if UNITY_EDITOR
        bool _warnedAboutPlayMode;
#endif
        void TryInit()
        {
            if (_overlay?.UIDocument) return;

            _overlay = ConsoleOverlay.Instance;
            
            if (!_overlay?.UIDocument) return;
            if (!Application.isEditor && (_overlay == null || !_overlay.Context.Settings.inPlayerKeyBindings))
            {
                _disabled = true;
                //NjLogger.Info(nameof(ConsoleKeyBindings), "did not start because it is disabled");
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying && !_warnedAboutPlayMode)
            {
                _warnedAboutPlayMode = true;
                Debug.LogWarning($"{nameof(ConsoleKeyBindings)}.{nameof(BindKeyDown)}() only works in play mode...");
            }
#endif
            Init();
        }
 
// we prioritise legacy because:
// In Unity 2022, you can have input set to both, but not have the actual input package installed.
// We want those cases to still work
#if ENABLE_LEGACY_INPUT_MANAGER
        readonly Dictionary<(KeyCode, Modifier), Action> _downBinding = new();
        
        public void BindKeyDown(Action callback, KeyCode key, Modifier mods = default)
        {
            if (_disabled) return;
            
            TryInit();
#if UNITY_EDITOR
            if (!Application.isPlaying && !_warnedAboutPlayMode)
            {
                _warnedAboutPlayMode = true;
                Debug.LogWarning($"{nameof(ConsoleKeyBindings)}.{nameof(BindKeyDown)}() only works in play mode...");
            }
#endif
            _downBinding[(key, mods)] = callback;
        }

        public bool IsBoundToKeyDown(KeyCode key, Modifier mods = default, Action requiredCallback = null)
        {
            if (_disabled) return false;
            if (!_downBinding.TryGetValue((key, mods), out var existingCallback))
            {
                return false;
            }
            return requiredCallback == null || requiredCallback == existingCallback;
        }
        
        public void UnbindKeyDown(KeyCode key, Modifier mods, Action requiredCallback = null)
        {
            UnBind(_downBinding, key, mods, requiredCallback);
        }
        
        public void FindAndUnbindKeyDown(Action requiredCallback)
        {
            UnBind(_downBinding, requiredCallback);
        }

        public static string GetKeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Return: return "Enter";
                case KeyCode.Escape: return "Esc";
                case KeyCode.Space: return "Space";
                case KeyCode.LeftArrow: return "←";
                case KeyCode.RightArrow: return "→";
                case KeyCode.UpArrow: return "↑";
                case KeyCode.DownArrow: return "↓";
                default: return key.ToString();
            }
        }

        void Init()
        {
            _overlay.schedule.Execute(Update).Every(0);
        }

        void Bind(Dictionary<(KeyCode, Modifier), Action> dict, Action callback, KeyCode key, Modifier mods)
        {
            TryInit();
            dict[(key, mods)] = callback;
        }

        void UnBind(Dictionary<(KeyCode, Modifier), Action> dict, KeyCode key, Modifier mods, Action requiredCallback = null)
        {
            if (requiredCallback != null)
            {
                if(dict.TryGetValue((key, mods), out var existingCallback) && existingCallback == requiredCallback)
                {
                    dict.Remove((key, mods));
                }
            }
            else
            {
                dict.Remove((key, mods));
            }
        }

        void UnBind(Dictionary<(KeyCode, Modifier), Action> dict, Action requiredCallback)
        {
            var list = LoggerUtils.ThreadLocalPool<List<(KeyCode, Modifier)>>.Borrow();
            list.Clear();
            foreach (var kv in dict)
            {
                if (kv.Value == requiredCallback)
                {
                    list.Add(kv.Key);
                }
            }
            foreach (var key in list)
            {
                dict.Remove(key);
            }
            list.Clear();
            LoggerUtils.ThreadLocalPool<List<(KeyCode, Modifier)>>.Return(list);
        }
        
        void Update()
        {
            if (!Application.isEditor && _overlay.IsAccessChallengeRequired())
            {
                return;
            }
            if (!Input.anyKeyDown)
            {
                return;
            }
            foreach (var kv in _downBinding)
            {
                if (Input.GetKeyDown(kv.Key.Item1) && ModifiersMatch(kv.Key.Item2))
                {
                    kv.Value?.Invoke();
                }
            }
        }

        bool ModifiersMatch(Modifier mods)
        {
            return  ((mods & Modifier.Ctrl) != 0) == (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    && ((mods & Modifier.Shift) != 0) == (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    && ((mods & Modifier.Alt) != 0) == (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                    && ((mods & Modifier.Cmd) != 0) == (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand));
        }
#elif ENABLE_INPUT_SYSTEM
        
        readonly Dictionary<(Key, Modifier), Action> _downBinding = new();
        
        public void BindKeyDown(Action callback, Key key, Modifier mods = default)
        {
            if (_disabled) return;
            TryInit();
            _downBinding[(key, mods)] = callback;
        }
        public bool IsBoundToKeyDown(Key key, Modifier mods = default, Action requiredCallback = null)
        {
            if (_disabled) return false;
            if (!_downBinding.TryGetValue((key, mods), out var existingCallback))
            {
                return false;
            }
            return requiredCallback == null || requiredCallback == existingCallback;
        }
        
        public void UnbindKeyDown(Key key, Modifier mods, Action requiredCallback = null)
        {
            UnBind(_downBinding, key, mods, requiredCallback);
        }
        
        public void FindAndUnbindKeyDown(Action requiredCallback)
        {
            UnBind(_downBinding, requiredCallback);
        }

        public static string GetKeyName(Key key)
        {
            switch (key)
            {
                case Key.Enter: return "Enter";
                case Key.Escape: return "Esc";
                case Key.Space: return "Space";
                case Key.LeftArrow: return "←";
                case Key.RightArrow: return "→";
                case Key.UpArrow: return "↑";
                case Key.DownArrow: return "↓";
                default: return key.ToString();
            }
        }
        void Init()
        {
            InputSystem.onEvent -= OnInputEvent;
            InputSystem.onEvent += OnInputEvent;
        }

        void IConsoleModule.Dispose()
        {
            InputSystem.onEvent -= OnInputEvent;
        }

        void UnBind(Dictionary<(Key, Modifier), Action> dict, Key key, Modifier mods, Action requiredCallback = null)
        {
            if (requiredCallback != null)
            {
                if(dict.TryGetValue((key, mods), out var existingCallback) && existingCallback == requiredCallback)
                {
                    dict.Remove((key, mods));
                }
            }
            else
            {
                dict.Remove((key, mods));
            }
        }

        void UnBind(Dictionary<(Key, Modifier), Action> dict, Action requiredCallback)
        {
            var list = LoggerUtils.ThreadLocalPool<List<(Key, Modifier)>>.Borrow();
            list.Clear();
            foreach (var kv in dict)
            {
                if (kv.Value == requiredCallback)
                {
                    list.Add(kv.Key);
                }
            }
            foreach (var key in list)
            {
                dict.Remove(key);
            }
            list.Clear();
            LoggerUtils.ThreadLocalPool<List<(Key, Modifier)>>.Return(list);
        }
        
        void OnInputEvent(InputEventPtr inputEvent, InputDevice inputDevice)
        {
            if (inputDevice is not Keyboard keyboard || !keyboard.anyKey.isPressed)
            {
                return;
            }
            if (!Application.isEditor && _overlay.IsAccessChallengeRequired())
            {
                return;
            }
            if(IsUserTyping(_overlay))
            {
                return;
            }
            foreach (var bindingKv in _downBinding)
            {
                var keyControl = keyboard[bindingKv.Key.Item1];
                if (keyControl != null && keyControl.wasPressedThisFrame)
                {
                    if (ModifiersMatch(keyboard, bindingKv.Key.Item2))
                    {
                        bindingKv.Value?.Invoke();
                    }
                }
            }
        }

        bool ModifiersMatch(Keyboard keyboard, Modifier mods)
        {
            return  ((mods & Modifier.Ctrl) != 0) == keyboard.ctrlKey?.isPressed
                    && ((mods & Modifier.Shift) != 0) == keyboard.shiftKey?.isPressed
                    && ((mods & Modifier.Alt) != 0) == keyboard.altKey?.isPressed
                    && ((mods & Modifier.Cmd) != 0) == ((keyboard.leftCommandKey?.isPressed ?? false) || (keyboard.rightCommandKey?.isPressed ?? false));
        }
#else
        void Init()
        {
            
        }
#endif

        public static bool IsUserTyping(VisualElement rootElement = null)
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected)
            {
                if (selected.GetComponent<InputField>() != null)
                {
                    return true;
                }
                if (selected.GetComponent("TMP_InputField") != null)
                {
                    return true;
                }
            }
#if UNITY_EDITOR
            if (GUI.GetNameOfFocusedControl() != string.Empty)
            {
                return true;
            }
#endif
            var focusedElement = rootElement?.panel?.focusController?.focusedElement;
            return focusedElement is TextInputBaseField<string>;
        }
        
#elif ENABLE_LEGACY_INPUT_MANAGER
        // disabled case
        public void BindKeyDown(Action callback, KeyCode key, Modifier mods = default) { }
        public bool IsBoundToKeyDown(KeyCode key, Modifier mods = default, Action requiredCallback = null) => false;
        public void UnbindKeyDown(KeyCode key, Modifier mods, Action requiredCallback = null) { }
        public void FindAndUnbindKeyDown(Action requiredCallback) { }
        public static string GetKeyName(KeyCode key) => string.Empty;
        
#elif ENABLE_INPUT_SYSTEM
        // disabled case
        public void BindKeyDown(Action callback, Key key, Modifier mods = default) { }
        public bool IsBoundToKeyDown(Key key, Modifier mods = default, Action requiredCallback = null) => false;

        public void UnbindKeyDown(Key key, Modifier mods, Action requiredCallback = null) { }
        public void FindAndUnbindKeyDown(Action requiredCallback) { }
        public static string GetKeyName(Key key) => string.Empty;
#endif
        
        public static string GetModKeysName(Modifier modifiers)
        {
            var mod = "";
            if ((modifiers & Modifier.Ctrl) != 0)  mod += "^";
            if ((modifiers & Modifier.Shift) != 0) mod += "s";
            if ((modifiers & Modifier.Alt) != 0)   mod += "a";
            if ((modifiers & Modifier.Cmd) != 0)   mod += "c";
            return mod;
        }
        
        [Flags]
        public enum Modifier : byte
        {
            None = 0,
            Shift = 1 << 0,
            Ctrl  = 1 << 1,
            Alt   = 1 << 2,
            Cmd   = 1 << 3
        }
    }
}