#if !NJCONSOLE_DISABLE
using System;
using System.Collections.Generic;
using System.Text;
using Ninjadini.Logger;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public class ConsoleGraphingElement : VisualElement
    {
        readonly int _maxHistory;
        readonly float _pixesPerValue = 2;
        
        readonly Dictionary<IConsoleGraphDataProvider, GraphLine> _lines = new();
        protected readonly VisualElement GraphArea;
        protected readonly VisualElement LabelsArea;

        static readonly StringBuilder StringBuilder = new StringBuilder(32);

        public float? ForcedMaxBound;
        public float? ForcedMinBound;
        public string ValueSuffix;

        public ConsoleGraphingElement(int width, float pixesPerValue = 2f, int intervalMs = 0)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException();
            if (pixesPerValue <= 0) throw new ArgumentOutOfRangeException();
            
            _pixesPerValue = pixesPerValue;
            _maxHistory = Mathf.CeilToInt(width / (float)pixesPerValue) + 1;
            GraphArea = new VisualElement();
            GraphArea.generateVisualContent += OnGenerateVisualContent;
            GraphArea.style.width = width;
            
            GraphArea.AddToClassList("graph-area");
            Add(GraphArea);

            LabelsArea = new VisualElement();
            LabelsArea.AddToClassList("graph-labels");
            Add(LabelsArea);
            
            schedule.Execute(Update).Every(intervalMs);
            
            AddToClassList("graph-panel");
        }

        public void Add(IConsoleGraphDataProvider provider)
        {
            if (provider == null)
            {
                return;
            }
            var line = new GraphLine(provider, _maxHistory);
            _lines.Add(provider, line);
            
            var valueLabel = AddValueLabel(provider);
            if (valueLabel != null)
            {
                if (valueLabel.userData != null)
                {
                    throw new Exception("Value label's userData must be null because we need to use it to detect change");
                }
                valueLabel.userData = new PreviousLabelValueUserData();
                line.ValueLabel = valueLabel;
            }
        }

        /// Return the value label - so we can update it for you.
        protected virtual Label AddValueLabel(IConsoleGraphDataProvider provider)
        {
            var label = new Label();
            label.text = provider.Name;
            label.AddToClassList("graph-line-label");
            LabelsArea.Add(label);
            
            label = new Label();
            label.AddToClassList("graph-line-value");
            label.style.color = provider.ValueColor;
            LabelsArea.Add(label);
            return label;
        }

        protected virtual void UpdateLabelValue(Label label, float value)
        {
            if (label.userData is PreviousLabelValueUserData previousLabelValueUserData)
            {
                if(previousLabelValueUserData.Value == null || !Mathf.Approximately(previousLabelValueUserData.Value.Value, value))
                {
                    previousLabelValueUserData.Value = value;
                }
                else
                {
                    return;
                }
            }
            StringBuilder.Clear();
            LoggerUtils.AppendNum(StringBuilder, value, group:true);
            if (ValueSuffix != null)
            {
                StringBuilder.Append(ValueSuffix);
            }
            label.text = StringBuilder.ToString();
        }

        protected virtual void Update()
        {
            if (_lines.Count == 0) return;
            
            foreach (var kv in _lines)
            {
                UpdateLine(kv.Value);
            }
            GraphArea.MarkDirtyRepaint();
        }

        void UpdateLine(GraphLine line)
        {
            var valueBefore = line.LastValue;
            var value = line.UpdateValue();
            if (line.ValueLabel == null)
            {
                return;
            }
            if (Mathf.Approximately(value, valueBefore))
            {
                return;
            }
            UpdateLabelValue(line.ValueLabel, value);
        }

        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var rect = ctx.visualElement.contentRect;
            if (rect.width <= 0 || rect.height <= 0 || _lines.Count == 0) return;

            var minValue = float.MaxValue;
            var maxValue = float.MinValue;
            if (!ForcedMaxBound.HasValue || !ForcedMinBound.HasValue)
            {
                foreach (var kv in _lines)
                {
                    if (!kv.Value.BoundCalculationEnabled) continue;
                    minValue = Mathf.Min(minValue, kv.Value.MinValue);
                    maxValue = Mathf.Max(maxValue, kv.Value.MaxValue);
                }
            }
            if (ForcedMinBound.HasValue)
            {
                minValue = ForcedMinBound.Value;
            }
            if (ForcedMaxBound.HasValue)
            {
                maxValue = ForcedMaxBound.Value;
            }
            
            var range = maxValue - minValue;

            foreach (var kv in _lines)
            {
                var line = kv.Value;
                var points = line.HistoryCount;
                if (points < 2) continue;

                var painter = ctx.painter2D;
                painter.strokeColor = line.Color;
                painter.lineWidth = 1f;
                painter.BeginPath();

                var maxPix = _pixesPerValue * points;
                for (var i = points - 1; i >= 0; i--)
                {
                    var x = (rect.width - maxPix) + _pixesPerValue * i;
                    if (x < 0)
                    {
                        break;
                    }
                    var yNormalized = (line[i] - minValue) / range;
                    var y = rect.height * (1f - yNormalized);
                    if (i == points - 1) painter.MoveTo(new Vector2(x, y));
                    else painter.LineTo(new Vector2(x, y));
                }
                painter.Stroke();
            }
        }

        public float GetAverageValue(IConsoleGraphDataProvider provider)
        {
            return _lines.TryGetValue(provider, out var line) ? line.AverageValue : 0f;
        }

        public float GetMinValue(IConsoleGraphDataProvider provider)
        {
            return _lines.TryGetValue(provider, out var line) ? line.MinValue : 0f;
        }

        public float GetMaxValue(IConsoleGraphDataProvider provider)
        {
            return _lines.TryGetValue(provider, out var line) ? line.MaxValue : 0f;
        }

        protected class PreviousLabelValueUserData
        {
            public float? Value;
        }
        
        protected class GraphLine
        {
            public readonly IConsoleGraphDataProvider Provider;
            public readonly bool BoundCalculationEnabled;
            readonly float[] _buffer;
            int _head;
            int _count;
            float _lastValue;
            
            public Color Color { get; }
            public float AverageValue { get; private set; }
            public float MinValue { get; private set; } = -0.1f;
            public float MaxValue { get; private set; } = 0.1f;

            public Label ValueLabel;

            public GraphLine(IConsoleGraphDataProvider provider ,int capacity)
            {
                Provider = provider;
                Color = provider.GraphColor;
                _buffer = new float[capacity];
                BoundCalculationEnabled = Provider.BoundCalculationEnabled;
            }

            public float LastValue => _lastValue;

            public int HistoryCount => _count;

            public float this[int i]
            {
                get
                {
                    if (i < 0 || i >= _count) throw new IndexOutOfRangeException();
                    return _buffer[(_head - _count + i + _buffer.Length) % _buffer.Length];
                }
            }

            public float UpdateValue()
            { 
                var value = Provider.GetValue();
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    return _lastValue;
                }
                _buffer[_head] = value;
                _head = (_head + 1) % _buffer.Length;
                if (_count < _buffer.Length)
                {
                    _count++;
                }
                if (!Mathf.Approximately(_lastValue, value))
                {
                    if (BoundCalculationEnabled)
                    {
                        UpdateRanges();
                    }
                    _lastValue = value;
                }
                return _lastValue;
            }

            void UpdateRanges()
            {
                var min = float.MaxValue;
                var max = float.MinValue;
                var sum = 0f;
                for (var i = 0; i < _count; i++)
                {
                    var v = this[i];
                    sum += v;
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
                AverageValue = sum / _count;
                if (Mathf.Approximately(min, max))
                {
                    MinValue = min - 0.01f;
                    MaxValue = max + 0.01f;
                }
                else
                {
                    MinValue = min;
                    MaxValue = max;
                }
            }
        }
    }
}
#endif