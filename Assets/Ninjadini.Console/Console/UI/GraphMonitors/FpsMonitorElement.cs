#if !NJCONSOLE_DISABLE
using Ninjadini.Console.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ninjadini.Console.UI
{
    public class FpsMonitorElement : ConsoleGraphingElement, IConsoleGraphDataProvider
    {
        static readonly Color FPSColor = new Color(0.2f, 1f, 0.2f);
        static readonly Color AvgColor = Color.gray;
        
        readonly Label _fpsLabel;
        readonly Label _avgLabel;
        readonly Label _minLabel;
        readonly IConsoleGraphDataProvider _avgProvider;

        static readonly ConsoleUIUtils.IntStringCache IntStringCache = new (64);
        
        public FpsMonitorElement() : base(150)
        {
            ForcedMinBound = 0;
            style.height = 60;
            
            _fpsLabel = new Label();
            _avgLabel = new Label();
            _minLabel = new Label();
            
            _fpsLabel.AddToClassList("graph-fps-value");
            _fpsLabel.style.color = FPSColor;
            
            _avgLabel.AddToClassList("graph-line-value");
            _avgLabel.style.color = AvgColor;
            
            _minLabel.AddToClassList("graph-line-value");
            _minLabel.style.color = AvgColor;

            LabelsArea.Add(_fpsLabel);
            
            var lbl = new Label(ConsoleUIStrings.GraphFpsAvg);
            lbl.AddToClassList("graph-line-label");
            LabelsArea.Add(lbl);
            LabelsArea.Add(_avgLabel);
            
            lbl = new Label(ConsoleUIStrings.GraphFpsMin);
            lbl.AddToClassList("graph-line-label");
            LabelsArea.Add(lbl);
            LabelsArea.Add(_minLabel);

            _avgProvider = new IConsoleGraphDataProvider.Simple("avg", AvgColor, GetAverage)
            {
                BoundCalculationEnabled = false
            };
            Add(_avgProvider);
            Add(this);
        }
        
        protected override Label AddValueLabel(IConsoleGraphDataProvider provider)
        {
            if (provider == this) return _fpsLabel;
            if (provider == _avgProvider) return _avgLabel;
            return null;
        }

        protected override void Update()
        {
            base.Update();
            var minValue = GetMinValue(this);
            UpdateLabelValue(_minLabel, Mathf.Round(minValue));
        }

        protected override void UpdateLabelValue(Label label, float value)
        {
            label.text = IntStringCache.Get(Mathf.RoundToInt(value));
        }

        string IConsoleGraphDataProvider.Name => "FPS"; // not actually used

        Color IConsoleGraphDataProvider.GraphColor => FPSColor;

        public float GetValue()
        {
            return Mathf.Round(1f / Time.unscaledDeltaTime);
        }
        
        float GetAverage()
        {
            var avg = GetAverageValue(this);
            return Mathf.Round(avg == 0f ? GetValue() : avg);
        }
    }
}
#endif