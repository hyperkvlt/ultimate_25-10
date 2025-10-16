using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.Internal
{
    [Serializable]
    [DisplayName("Press and hold at corner")]
    public class ConsolePressAndHoldTrigger : IConsoleOverlayTrigger
    {
        public Position position;
        
        [Tooltip("Size of the corner position. This is relative to console's ui scale, so maybe need a bit of adjustment based on your target screen resolution/type.")]
        public float size = 35;

        [Tooltip("Hold duration works separately to tap count. You can choose to have both (hold 1 seconds, OR  tap 3 times, whichever you prefer). Set to 0 or less to disable trigger from hold.")]
        public float holdDurationSecs = 0.7f;
        
        [Tooltip("Tap count works separately to hold duration. You can choose to have both (hold 1 seconds, OR  tap 3 times, whichever you prefer). Set to 0 or less to disable trigger from tapping.")]
        public int tapCount = 2;
        
#if !NJCONSOLE_DISABLE
        public void ListenForTriggers(ConsoleOverlay overlay)
        {
            if (size > 0 && (holdDurationSecs > 0f || tapCount > 0))
            {
                var element = new Element(overlay, this);
                overlay.Add(element);
            }
        }

        class Element : VisualElement
        {
            readonly ConsoleOverlay _overlay;
            readonly ConsolePressAndHoldTrigger _trigger;

            float? _holdStartTime;
            float _lastTappedTime;
            int _tappedCount;
            
            public Element(ConsoleOverlay overlay, ConsolePressAndHoldTrigger trigger)
            {
                name = "PressAndHoldTrigger";
                _overlay = overlay;
                _trigger = trigger;
                
                ConsoleUIUtils.AutoApplySafeRegion(this, () => overlay.SafeAreaScale);
                
                style.width = trigger.size + 100;
                style.height = trigger.size + 100;
                style.backgroundColor = new Color(0.5f, 1f, 0.5f, 0.5f);
                style.display = DisplayStyle.None;

                style.position = UnityEngine.UIElements.Position.Absolute;
                switch (trigger.position)
                {
                    case Position.TopLeft:
                        style.top = -100;
                        style.left = -100;
                        break;
                    case Position.BottomLeft:
                        style.bottom = -100;
                        style.left = -100;
                        break;
                    case Position.TopRight:
                        style.top = -100;
                        style.right = -100;
                        break;
                    case Position.BottomRight:
                        style.bottom = -100;
                        style.right = -100;
                        break;
                }
                
                schedule.Execute(Update).Every(0);
            }

            void Update()
            {
                if (_holdStartTime.HasValue)
                {
                    if(IsHittingRightPosition(false))
                    {
                        if (_trigger.holdDurationSecs > 0f && (Time.realtimeSinceStartup - _holdStartTime.Value) >= _trigger.holdDurationSecs)
                        {
                            Triggered();
                        }
                    }
                    else
                    {
                        _holdStartTime = null;
                        style.display = DisplayStyle.None;
                    }
                }
                if (_overlay.Showing || panel == null) return;
                if (!IsHittingRightPosition(true)) return;
                if (_overlay.ShowingAnyChallenge()) return;
                
                var timeNow = Time.realtimeSinceStartup;
                if (_trigger.tapCount > 0)
                {
                    if (_lastTappedTime < timeNow - 0.7f)
                    {
                        _tappedCount = 0;
                    }
                    _lastTappedTime = timeNow;
                    _tappedCount++;
                    if (_tappedCount >= _trigger.tapCount)
                    {
                        _tappedCount = 0;
                        Triggered();
                    }
                }

                _holdStartTime = timeNow;
                style.display = DisplayStyle.Flex;
            }

            bool IsHittingRightPosition(bool needsStart)
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                if (needsStart)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        return IsMouseInRightPosition(Input.mousePosition);
                    }
                }
                else if (Input.GetMouseButton(0))
                {
                        return IsMouseInRightPosition(Input.mousePosition);
                }
                return false;
#elif ENABLE_INPUT_SYSTEM
                var touches = UnityEngine.InputSystem.Touchscreen.current;
                if (touches != null)
                {
                    if (touches.press.isPressed && (!needsStart || touches.press.wasPressedThisFrame))
                    {
                        return IsMouseInRightPosition(touches.position.ReadValue());
                    }
                }
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if ((mouse?.leftButton?.isPressed ?? false) && (!needsStart || mouse.leftButton.wasPressedThisFrame))
                {
                    return IsMouseInRightPosition(mouse.position.ReadValue());
                }
                return false;
#else
                return false;
#endif
            }

            void Triggered()
            {
                _holdStartTime = null;
                style.display = DisplayStyle.None;
                _overlay.ShowWithAccessChallenge();
            }

            bool IsMouseInRightPosition(Vector2 pos)
            {
                if (pos.y < 0f || pos.y > Screen.height)
                {
                    // for some reason input is still captured when you click on the 'Game' tab in editor.
                    return false;
                }
                var safeArea = Screen.safeArea;
                switch (_trigger.position)
                {
                    case Position.TopLeft:
                        pos.x -= safeArea.x;
                        pos.y = Screen.height - pos.y - (Screen.height - safeArea.yMax);
                    break;
                    case Position.TopRight:
                        pos.x = Screen.width - pos.x - (Screen.width - safeArea.xMax);
                        pos.y = Screen.height - pos.y - safeArea.y;
                    break;
                    case Position.BottomLeft:
                        pos.x -= safeArea.x;
                        pos.y -= safeArea.y;
                        break;
                    case Position.BottomRight:
                        pos.x = Screen.width - pos.x - safeArea.x;
                        pos.y -= safeArea.y;
                        break;
                }
                pos = RuntimePanelUtils.ScreenToPanel(panel, pos);
                return pos.x <= _trigger.size && pos.y <= _trigger.size;
            }
        }
#endif
        public enum Position
        {
            TopLeft = 0,
            TopRight = 1,
            BottomRight = 2,
            BottomLeft = 3
        }
    }
}