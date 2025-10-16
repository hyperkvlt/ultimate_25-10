#if !NJCONSOLE_DISABLE
using System;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console
{
    public class ConsoleToasts : VisualElement
    {
        /// If the current console context can't be found it'll only print it to the log.
        public static void TryShow(string message, Action ctaCallback = null, string ctaBtnName = null)
        {
            var context = ConsoleContext.TryGetFocusedContext();
            if (context != null)
            {
                Show(context, message, ctaCallback, ctaBtnName);
            }
            else
            {
                NjLogger.Info("Console toast message:", message);
            }
        }
        
        public static void Show(ConsoleContext console, string message, Action ctaCallback = null, string ctaBtnName = null)
        {
            var toasts = console.GetData<ConsoleToasts>();
            if (toasts == null)
            {
                toasts = new ConsoleToasts();
                console.RootElement.Add(toasts);
                console.SetData(toasts);
            }
            toasts.Show(message, ctaCallback, ctaBtnName);
        }

        public ConsoleToasts()
        {
            AddToClassList("toasts-container");
            pickingMode = PickingMode.Ignore;
        }
        
        public void Show(string message, Action ctaCallback = null, string ctaBtnName = null)
        {
            var isMouseOver = false;
            var toast = new ScrollView();
            toast.AddToClassList("toasts-item");
            toast.RegisterCallback<PointerDownEvent>(_ => OnMouseOver(), TrickleDown.TrickleDown);
            toast.RegisterCallback<PointerEnterEvent>(_ => OnMouseOver());
            
            toast.RegisterCallback<PointerCancelEvent>(_ => OnMouseOut());
            toast.RegisterCallback<PointerLeaveEvent>(_ => OnMouseOut());
            toast.RegisterCallback<PointerUpEvent>(_ => OnMouseOut(), TrickleDown.TrickleDown);

            void OnMouseOver()
            {
                isMouseOver = true;
                toast.AddToClassList("toasts-item-waiting");
            }
            void OnMouseOut()
            {
                isMouseOver = false;
                toast.RemoveFromClassList("toasts-item-waiting");
            }
            var label = new Label(message);
            label.AddToClassList("toasts-item-label");
            toast.Add(label);
            var hideAfterMs = Math.Min(2000 + message.Length * 15, 10000);
            
            if (ctaCallback != null)
            {
                hideAfterMs = Math.Max(hideAfterMs, 5);
                
                var btn = new Button(() =>
                {
                    toast.RemoveFromHierarchy();
                    ctaCallback();
                })
                {
                    text = string.IsNullOrEmpty(ctaBtnName) ? "Action" : ctaBtnName
                };
                btn.AddToClassList("toasts-item-actbtn");
                toast.hierarchy.Add(btn);
            }

            var rightVertical = new VisualElement();
            rightVertical.AddToClassList("toasts-item-sideBtns");
            toast.hierarchy.Add(rightVertical);
            var closeBtn = new Button(() =>
            {
                toast.RemoveFromHierarchy();
            })
            {
                text = "X"
            };
            rightVertical.Add(closeBtn);

            var endTime = DateTime.UtcNow.AddMilliseconds(hideAfterMs);
            // because things are supposed to work in editor, it is more complicated than usual time delta stuff.
            toast.schedule.Execute(() =>
            {
                var timeNow = DateTime.UtcNow;
                if (timeNow < endTime)
                {
                    return;
                }
                if (isMouseOver)
                {
                    endTime = timeNow;
                    return;
                }
                var opacity = Mathf.Lerp(1f, 0f, (float)(timeNow-endTime).TotalSeconds * 3f);
                toast.style.opacity = opacity;
                if (opacity <= 0.0001f)
                {
                    ctaCallback = null;
                    toast.RemoveFromHierarchy();
                }
            }).Every(0);
            
            Add(toast);
        }
    }
}
#endif