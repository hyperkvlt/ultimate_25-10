#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.IO;
using Ninjadini.Console.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.Internal
{
    public static class ConsoleUIUtils
    {
        public static VisualElement FindConsoleOrRoot(VisualElement visualSrc)
        {
            return FindParent<ConsoleWindow>(visualSrc) 
                         ?? FindParent<ConsoleOverlay>(visualSrc) 
                         ?? FindRoot(visualSrc);
        }

        public static VisualElement FindRoot(VisualElement visualElement)
        {
            while (visualElement.parent != null)
            {
                visualElement = visualElement.parent;
            }
            return visualElement;
        }
        
        public static T FindParent<T>(VisualElement visualElement)
        {
            while (visualElement != null)
            {
                if (visualElement is T result)
                {
                    return result;
                }
                visualElement = visualElement.parent;
            }
            return default;
        }
        
        public static void RegisterOrientationDirection(VisualElement rootWindow, VisualElement targetElement, FlexDirection landscape, FlexDirection portrait)
        {
            targetElement.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var rect = rootWindow.contentRect;
                var targetDirection = rect.width < rect.height ? portrait : landscape;
                if (targetElement.style.flexDirection != targetDirection)
                {
                    targetElement.style.flexDirection = targetDirection;
                }
            });
        }
        
        public static void RegisterOrientationCallback(VisualElement rootWindow, VisualElement targetElement, Action<bool> isPortraitCallback)
        {
            targetElement.RegisterCallback<GeometryChangedEvent, (VisualElement, Action<bool>)>((evt, obj) =>
            {
                var rect = obj.Item1.contentRect;
                obj.Item2?.Invoke(rect.width < rect.height);
            }, (rootWindow, isPortraitCallback));
        }
        
        public static void RegisterOrientationClass(VisualElement rootWindow, VisualElement targetElement, string landscapeClass, string portraitClass)
        {
            targetElement.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                var rect = rootWindow.contentRect;
                SwitchClass(targetElement, landscapeClass, portraitClass, rect.width < rect.height);
            });
        }
        
        public static void SwitchClass(VisualElement targetElement, string class1, string class2, bool wantsClass2)
        {
            if (wantsClass2)
            {
                targetElement.AddToClassList(class2);
                targetElement.RemoveFromClassList(class1);
            }
            else
            {
                targetElement.AddToClassList(class1);
                targetElement.RemoveFromClassList(class2);
            }
        }

        public static void SendPointerCancelEvent(VisualElement target)
        {  
            var cancelEvent = PointerCancelEvent.GetPooled();
            cancelEvent.target = target;
            target.SendEvent(cancelEvent);
        }

        public static void SetBorderColor(VisualElement element, Color color)
        {
            var style = element.style;
            style.borderTopColor = style.borderRightColor = style.borderBottomColor = style.borderLeftColor = color;
        }

        /// Warning: This is not reusable on the same removing object, it will leak the schedule each time.
        public static void AutoRemoveAWhenBIsRemoved(VisualElement elementAToRemove, VisualElement elementBToCheck)
        {
            IVisualElementScheduledItem schedule = null;
            schedule = elementAToRemove.schedule.Execute(() =>
            {
                if (elementBToCheck.panel == null)
                {
                    elementAToRemove.RemoveFromHierarchy();
                    schedule?.Pause();
                }
            }).Every(0);
        }

        public static void SetSubmissionCallback<T>(TextInputBaseField<T> textField, Action<T> setValue)
        {
            //isDelayed doesn't work on ios and android... so this is the workaround to work with any combo.
            TouchScreenKeyboard keyboard = null;
            var valueBefore = textField.value;
            textField.RegisterCallback<FocusEvent>(_ =>
            {
                valueBefore = textField.value;
#if UNITY_2022_3_OR_NEWER
                keyboard = textField.touchScreenKeyboard;
                if (keyboard == null)
                {
                    textField.isDelayed = true;
                    textField.RegisterValueChangedCallback(ValueChangedCallback);
                }
                else
#endif
                {
                    textField.isDelayed = false;
                    textField.UnregisterValueChangedCallback(ValueChangedCallback);
                }
            });
            textField.RegisterCallback<FocusOutEvent>(_ =>
            {
                textField.UnregisterValueChangedCallback(ValueChangedCallback);
                if (keyboard != null)
                {
                    if (keyboard.status == TouchScreenKeyboard.Status.Done)
                    {
                        valueBefore = textField.value;
                        setValue(textField.value);
                    }
                    else
                    {
                        textField.SetValueWithoutNotify(valueBefore);
                        setValue(valueBefore);
                    }
                    keyboard.active = false;
                }
#if UNITY_2022_3_OR_NEWER
                else
                {
                    textField.SetValueWithoutNotify(valueBefore);
                    textField.schedule.Execute(() =>
                    {
                        textField.SetValueWithoutNotify(valueBefore);
                    });
                }
#else
                else
                {
                    valueBefore = textField.value;
                    setValue(textField.value);
                }
#endif
            });
            void ValueChangedCallback(ChangeEvent<T> evt)
            {
                valueBefore = evt.newValue;
                setValue(evt.newValue);
            }
        }
        
        
        public static void FixDropdownFieldPopupSize(VisualElement dropdown, ConsoleContext context)
        {
            if (context != null && !context.RuntimeUIDocument)
            {
                return;
            }
            dropdown.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (!dropdown.enabledInHierarchy) return;
                dropdown.schedule.Execute(() =>
                {
                    var dropDownRoot = dropdown.panel?.visualTree?.Q(className: "unity-base-dropdown");
                    if (dropDownRoot == null) return;
                    var container = dropDownRoot?.Q<VisualElement>(className:"unity-base-dropdown__container-outer");
                    if (container == null) return;

                    var scrollView = dropDownRoot.Q<ScrollView>();
                    if (scrollView == null) return;

                    var width = container.style.width.value.value;
                    if (float.IsNaN(width) || width < 50f) width = 50f;
                    container.style.minWidth = width;
                    dropDownRoot.schedule.Execute(() =>
                        {
                            width += 15;
                            container.style.minWidth = width;
                        })
                    .Until(() => width > 400 || scrollView.horizontalScroller == null || scrollView.horizontalScroller.style.display != DisplayStyle.Flex);
                    
                }).ExecuteLater(10);
            });
        }

        /// you must call this only BEFORE GeometryChangedEvent
        public static void AutoApplySafeRegion(VisualElement element, Func<float> getScaleCallback = null)
        {
            var drawnSafeArea = new Rect();
            void UpdateSafeRegion()
            {
                var safeArea = Screen.safeArea;
                if (drawnSafeArea == safeArea)
                {
                    return;
                }
                if (ApplySafeArea(element, getScaleCallback?.Invoke() ?? 1f))
                {
                    drawnSafeArea = safeArea;
                }
            }
            element.RegisterCallback<GeometryChangedEvent>((evt) =>
            {
                if (drawnSafeArea != Screen.safeArea)
                {
                    element.schedule.Execute(UpdateSafeRegion);
                }
            });
            UpdateSafeRegion();
        }

        /// you must call this only AFTER GeometryChangedEvent
        /// Also this doesn't work properly if you are calling from an editor code - due to Screen.width returning the editor's window size now the game view.
        public static bool ApplySafeArea(VisualElement element, float scale = 1f)
        {
            var resolvedStyle = element.resolvedStyle;
            if (float.IsNaN(resolvedStyle.width))
            {
                // geometry not ready yet.
                return false;
            }
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2(Screen.width, Screen.height);

            var anchorMin = safeArea.position / screenSize;
            var anchorMax = (safeArea.position + safeArea.size) / screenSize;

            var style = element.style;
            style.marginLeft = Mathf.LerpUnclamped(0, anchorMin.x * resolvedStyle.width, scale);
            style.marginRight = Mathf.LerpUnclamped(0, (1f - anchorMax.x) * resolvedStyle.width, scale);
            style.marginTop = Mathf.LerpUnclamped(0, (1f - anchorMax.y) * resolvedStyle.height, scale);
            style.marginBottom = Mathf.LerpUnclamped(0, anchorMin.y * resolvedStyle.height, scale);
            return true;
        }

        public const float DefaultLongHoldDelaySecs = 0.7f;

        public static void ListenForLongHold(VisualElement btn, Action callback, ILongHoldListener listener = null)
        {
            IVisualElementScheduledItem holdSchedule = null;
            var captureTarget = listener == null ? btn : listener.GetCaptureTarget(btn);
            float? downedTime;
            int pointerId;
            void OnHoldUpdate()
            {
                if(!downedTime.HasValue) return;
                var requiredTime = listener?.GetHoldDuration() ?? DefaultLongHoldDelaySecs;
                if ((Time.realtimeSinceStartup - downedTime) > requiredTime)
                {
                    holdSchedule?.Pause();
                    downedTime = null;
                    
                    callback?.Invoke();
                    listener?.OnLongHoldTriggered(btn, pointerId);
                }
            };
            btn.RegisterCallback<PointerDownEvent>(evt =>
            {
                pointerId = evt.pointerId;
                downedTime = Time.realtimeSinceStartup;
                if(holdSchedule == null)
                {
                    holdSchedule = btn.schedule.Execute(OnHoldUpdate).Every(50);
                }
                else
                {
                    holdSchedule.Resume();
                }
                captureTarget?.CapturePointer(evt.pointerId);
                listener?.OnPointerDown(evt, btn);
            }, TrickleDown.TrickleDown);
            btn.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                holdSchedule?.Pause();
                downedTime = null;
            });
            btn.RegisterCallback<PointerUpEvent>(evt =>
            {
                holdSchedule?.Pause();
                downedTime = null;
                captureTarget?.ReleasePointer(evt.pointerId);
                listener?.OnPointerUp(evt, btn);
            }, TrickleDown.TrickleDown);
            btn.RegisterCallback<ClickEvent>(evt =>
            {
                holdSchedule?.Pause();
                downedTime = null;
                listener?.OnClick(evt, btn);
            }, TrickleDown.TrickleDown);
        }
        
        public interface ILongHoldListener
        {
            float? GetHoldDuration() => null;
            void OnLongHoldTriggered(VisualElement btn, int pointerId) {}
            void OnPointerDown(PointerDownEvent evt, VisualElement btn) {}
            void OnPointerUp(PointerUpEvent evt, VisualElement btn) {}
            void OnClick(ClickEvent evt, VisualElement btn) {}
            
            VisualElement GetCaptureTarget(VisualElement btn) => btn;
        }
/*
        public static void StartDragging(VisualElement element, EventCallback<PointerUpEvent> upRelease, Action<PointerMoveEvent> onMove = null)
        {
            var wasEnabled = element.enabledSelf;
            element.SetEnabled(false);

            var rect = element.worldBound;
            var prevPos = element.style.position;
            var prevLeft = element.style.left;
            var prevTop = element.style.top;
            element.style.position = Position.Absolute;
            element.style.left = rect.x;
            element.style.top = rect.y;
            
            EventCallback<PointerMoveEvent> onPointerMove = (evt) => 
            {
                element.style.left = element.style.left.value.value + evt.deltaPosition.x;
                element.style.top = element.style.top.value.value + evt.deltaPosition.y;
                onMove?.Invoke(evt);
            };
            EventCallback<PointerUpEvent> onPointerUp = null;
            onPointerUp = (evt) => 
            {
                element.style.position = prevPos;
                element.style.left = prevLeft;
                element.style.top = prevTop;
                element.SetEnabled(wasEnabled);
                Unregister();
                upRelease?.Invoke(evt);
            };

            element.RegisterCallback(onPointerMove);
            element.RegisterCallback(onPointerUp);
            return;

            void Unregister()
            {
                element.UnregisterCallback(onPointerMove);
                element.UnregisterCallback(onPointerUp);
            }
        }*/

        public class IntStringCache
        {
            // this is used to reduce allocation from repeatedly printing similar numbers
            private readonly int _maxSize;
            private readonly Dictionary<int, LinkedListNode<(int, string)>> _map;
            private readonly LinkedList<(int, string)> _lru;

            public IntStringCache(int maxSize = 128)
            {
                _maxSize = maxSize;
                _map = new Dictionary<int, LinkedListNode<(int, string)>>();
                _lru = new LinkedList<(int, string)>();
            }

            public string Get(int value)
            {
                if (_map.TryGetValue(value, out var node))
                {
                    _lru.Remove(node);
                    _lru.AddFirst(node);
                    return node.Value.Item2;
                }

                string str = value.ToString();
                var newNode = new LinkedListNode<(int, string)>((value, str));
                _lru.AddFirst(newNode);
                _map[value] = newNode;

                if (_map.Count > _maxSize)
                {
                    var last = _lru.Last;
                    _lru.RemoveLast();
                    _map.Remove(last.Value.Item1);
                }

                return str;
            }
        }

        public class PlayerPrefHistoryStrings
        {
            public readonly string PrefKey;
            public readonly int MaxItems;
            readonly string _split;
            List<string> _items;
            
            public PlayerPrefHistoryStrings(string prefKey, int maxItems, string split)
            {
                PrefKey = prefKey;
                MaxItems = maxItems;
                _split = split;
            }
            
            public List<string> Items
            {
                get
                {
                    if (_items == null)
                    {
                        _items = new List<string>();
                        var str = PlayerPrefs.GetString(PrefKey);
                        if (!string.IsNullOrEmpty(str))
                        {
                            _items.AddRange(str.Split(_split));
                        }
                    }
                    return _items;
                }
            }
            
            public void Add(string item)
            {
                var items = Items;
                items.Remove(item);
                if (items.Count >= MaxItems)
                {
                    items.RemoveAt(0);
                }
                items.Add(item);
                ApplyToPref();
            }

            public void ApplyToPref()
            {
                if (_items != null)
                {
                    PlayerPrefs.SetString(PrefKey, string.Join(_split, _items));
                }
            }
        }

        public static void GotoEditorLocalDoc()
        {
#if UNITY_EDITOR
            const string guid = "5f1a68c738795435d84969965b83c759";
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (!asset)
            {
                UnityEditor.EditorUtility.DisplayDialog("", "Sorry. Couldn't find the documentation file in the project.", "OK");
                return;
            }
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Project");
            UnityEditor.EditorApplication.delayCall += () =>
            {
                UnityEditor.Selection.activeObject = asset;
                UnityEditor.EditorGUIUtility.PingObject(asset);
            };
#endif
        }

        public static void CopyText(string text, ConsoleContext contextForToast)
        {
            GUIUtility.systemCopyBuffer = text;
            if (contextForToast == null) return;
            var probablyWorked = Application.platform != RuntimePlatform.WebGLPlayer;
            ConsoleToasts.Show(contextForToast, probablyWorked ? ConsoleUIStrings.CopiedToClipboard : ConsoleUIStrings.CopiedToClipboardMayNotWork);
        }

        public static void MailTo(string to, string subject, string body)
        {
            subject = Uri.EscapeDataString(subject);
            body = Uri.EscapeDataString(body);
            var mailto = $"mailto:{to}?subject={subject}&body={body}";
            Application.OpenURL(mailto);
        }
        
#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void NjConsole_ShareFile(string message);
#endif
        
        public static void ExportText(string filename, string content, string subject = null)
        {
            var njDir = Path.Combine(Application.temporaryCachePath, "NjConsole");
            if (!Directory.Exists(njDir))
            {
                Directory.CreateDirectory(njDir);
            }
            var path = Path.Combine(njDir, filename);
            File.WriteAllText(path, content);
            
#if UNITY_EDITOR_WIN || (!UNITY_EDITOR && UNITY_STANDALONE_WIN)
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{path.Replace('/', '\\')}\"",
                UseShellExecute = true
            });
#elif UNITY_EDITOR_OSX || (!UNITY_EDITOR && UNITY_STANDALONE_OSX)
            System.Diagnostics.Process.Start("open", $"-R \"{path}\"");
#elif !UNITY_EDITOR && UNITY_IOS
            NjConsole_ShareFile(path);
#elif !UNITY_EDITOR && UNITY_ANDROID
            using var intentClass = new AndroidJavaClass("android.content.Intent");
            using var intent = new AndroidJavaObject("android.content.Intent");

            intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            intent.Call<AndroidJavaObject>("setType", "text/plain");
            if(!string.IsNullOrEmpty(subject))
            {
                intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), filename);
            }
            intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), content);

            using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");

            using var chooser = intentClass.CallStatic<AndroidJavaObject>( "createChooser", intent, "Share via");
            activity.Call("startActivity", chooser);
#else
            Application.OpenURL("file://" + path);
#endif
        }

        public static bool CanExportText()
        {
            return Application.platform != RuntimePlatform.WebGLPlayer;
        }
    }
}
#endif