using System;
using UnityEngine;

namespace Ninjadini.Console.UI
{
    public interface IConsoleGraphDataProvider
    {
        string Name { get; }
        Color GraphColor { get; }
        Color ValueColor => GraphColor;
        float GetValue();
        public bool BoundCalculationEnabled => true;

        public class Simple : IConsoleGraphDataProvider
        {
            public string Name { get; }
            public Color GraphColor { get; }
            readonly Func<float> _getValue;

            public bool BoundCalculationEnabled { get; set; } = true;
            
            public Simple(string name, Color color, Func<float> getValue)
            {
                Name = name ?? string.Empty;
                GraphColor = color;
                _getValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
            }

            public float GetValue() => _getValue();
        }
    }
}