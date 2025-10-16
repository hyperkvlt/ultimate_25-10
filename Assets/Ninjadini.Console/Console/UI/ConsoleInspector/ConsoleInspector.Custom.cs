#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ninjadini.Console.UI
{
    public partial class ConsoleInspector
    {
        static Dictionary<Type, CustomDrawerDelegate> _drawers;
        
        public delegate void CustomDrawerDelegate(object obj, FieldsFoldOut fieldsFoldOut);
        public static void RegisterCustomDrawer(Type type, CustomDrawerDelegate drawerDelegate)
        {
            EnsureCustomDrawersInit();
            _drawers[type] = drawerDelegate;
        }
        
        public static CustomDrawerDelegate GetCustomDrawer(Type type)
        {
            EnsureCustomDrawersInit();
            return _drawers?.GetValueOrDefault(type);
        }

        static void EnsureCustomDrawersInit()
        {
            if (_drawers != null) return;
            _drawers = new Dictionary<Type, CustomDrawerDelegate>(16);
            RegisterDefaultDrawers();
        }

        static void RegisterDefaultDrawers()
        {
            // this is required because some unity component fields doesn't show up if you have stripping level to high.
            _drawers[typeof(Transform)] = (obj, foldOut) =>
            {
                var t = (Transform)obj;
                foldOut.Add(CreateField(nameof(Transform.position), typeof(Vector3), () => t.position, (v) => t.position = (Vector3)v));
                foldOut.Add(CreateField(nameof(Transform.localPosition), typeof(Vector3), () => t.localPosition, (v) => t.localPosition = (Vector3)v));
                foldOut.Add(CreateField(nameof(Transform.eulerAngles), typeof(Vector3), () => t.eulerAngles, (v) => t.eulerAngles = (Vector3)v));
                foldOut.Add(CreateField(nameof(Transform.localEulerAngles), typeof(Vector3), () => t.localEulerAngles, (v) => t.localEulerAngles = (Vector3)v));
                foldOut.Add(CreateField(nameof(Transform.rotation), typeof(Quaternion), () => t.rotation));
                foldOut.Add(CreateField(nameof(Transform.forward), typeof(Vector3), () => t.forward));
                foldOut.Add(CreateField(nameof(Transform.localScale), typeof(Vector3), () => t.localScale, (v) => t.localScale = (Vector3)v));
                foldOut.Add(CreateField(nameof(Transform.lossyScale), typeof(Vector3), () => t.lossyScale));
                foldOut.Add(CreateField(nameof(Transform.parent), typeof(Transform), () => t.parent));
                foldOut.Add(CreateField(nameof(Transform.childCount), typeof(int), () => t.childCount));
            };
            _drawers[typeof(RectTransform)] = (obj, foldOut) =>
            {
                var t = (RectTransform)obj;
                foldOut.Add(CreateField(nameof(RectTransform.position), typeof(Vector3), () => t.position, (v) => t.position = (Vector3)v));
                foldOut.Add(CreateField(nameof(RectTransform.localPosition), typeof(Vector3), () => t.localPosition, (v) => t.localPosition = (Vector3)v));
                foldOut.Add(CreateField(nameof(RectTransform.anchoredPosition), typeof(Vector2), () => t.anchoredPosition, (v) => t.anchoredPosition = (Vector2)v));
                foldOut.Add(CreateField(nameof(RectTransform.anchorMin), typeof(Vector2), () => t.anchorMin, (v) => t.anchorMin = (Vector2)v));
                foldOut.Add(CreateField(nameof(RectTransform.anchorMax), typeof(Vector2), () => t.anchorMax, (v) => t.anchorMax = (Vector2)v));
                foldOut.Add(CreateField(nameof(RectTransform.pivot), typeof(Vector2), () => t.pivot, (v) => t.pivot = (Vector2)v));
                foldOut.Add(CreateField(nameof(RectTransform.sizeDelta), typeof(Vector2), () => t.sizeDelta, (v) => t.sizeDelta = (Vector2)v));
                foldOut.Add(CreateField(nameof(RectTransform.localEulerAngles), typeof(Vector3), () => t.localEulerAngles, (v) => t.localEulerAngles = (Vector3)v));
                foldOut.Add(CreateField(nameof(RectTransform.rotation), typeof(Quaternion), () => t.rotation));
                foldOut.Add(CreateField(nameof(RectTransform.localScale), typeof(Vector3), () => t.localScale, (v) => t.localScale = (Vector3)v));
                foldOut.Add(CreateField(nameof(RectTransform.parent), typeof(Transform), () => t.parent));
                foldOut.Add(CreateField(nameof(RectTransform.childCount), typeof(int), () => t.childCount));
            };
        }
    }
}
#endif