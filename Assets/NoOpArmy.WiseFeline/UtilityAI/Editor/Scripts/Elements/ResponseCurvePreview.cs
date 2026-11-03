using UnityEngine;
using UnityEngine.UIElements;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// Draws a response curve over its zero to one input range, with an optional marker at the
    /// input and output a consideration produced most recently. It is meant for the properties
    /// panel and the debugger, where seeing the curve next to the live value explains a score much
    /// faster than reading two numbers.
    /// </summary>
    /// <remarks>
    /// This element works with the UnityEngine.AnimationCurve which the non DOTS considerations
    /// use. The DOTS debugger has its own preview for the DOTS ResponseCurve type.
    /// The vertical range always covers zero to one and grows when the curve goes above one or
    /// below zero, so a curve whose output leaves the usual range is still drawn completely.
    /// The element repaints itself on a timer while it is attached to a panel, so a curve which is
    /// being edited in the inspector updates without the owning window forcing a repaint. The timer
    /// stops when the element leaves the panel.
    /// All colors come from the stylesheet. The element adds DebugElements.uss to itself when it is
    /// constructed, which sets the theme colors for both editor skins. If that stylesheet cannot be
    /// found the element falls back to the resolved text color, so it still draws something
    /// visible.
    /// </remarks>
    [UxmlElement]
    public partial class ResponseCurvePreview : VisualElement
    {
        /// <summary>The USS class name which every ResponseCurvePreview carries.</summary>
        public static readonly string ussClassName = "wf-response-curve-preview";

        /// <summary>The USS class name of the child element the curve is drawn into.</summary>
        public static readonly string canvasUssClassName = "wf-response-curve-preview__canvas";

        /// <summary>The USS class name of the label under the curve which shows the input and the output.</summary>
        public static readonly string captionUssClassName = "wf-response-curve-preview__caption";

        /// <summary>The USS custom property which sets the color of the curve itself.</summary>
        public static readonly string curveColorPropertyName = "--wf-curve-line";

        /// <summary>The USS custom property which sets the color of the frame and the axis lines.</summary>
        public static readonly string axisColorPropertyName = "--wf-curve-axis";

        /// <summary>The USS custom property which sets the color of the marker at the current input.</summary>
        public static readonly string markerColorPropertyName = "--wf-curve-marker";

        /// <summary>The USS custom property which sets the color of the thin guide lines running from the marker to the axes.</summary>
        public static readonly string guideColorPropertyName = "--wf-curve-guide";

        /// <summary>How often the element repaints itself while it is attached to a panel, in milliseconds.</summary>
        public const long RepaintIntervalMilliseconds = 250;

        private const int DefaultSampleCount = 48;
        private const float MinimumDrawSize = 16f;
        private const float CurveWidth = 2f;
        private const float AxisWidth = 1f;
        private const float GuideWidth = 1f;
        private const float MarkerRadius = 4f;
        private const float AxisAlphaFactor = 0.5f;
        private const float GuideAlphaFactor = 0.35f;

        private static readonly CustomStyleProperty<Color> CurveColorProperty =
            new CustomStyleProperty<Color>("--wf-curve-line");
        private static readonly CustomStyleProperty<Color> AxisColorProperty =
            new CustomStyleProperty<Color>("--wf-curve-axis");
        private static readonly CustomStyleProperty<Color> MarkerColorProperty =
            new CustomStyleProperty<Color>("--wf-curve-marker");
        private static readonly CustomStyleProperty<Color> GuideColorProperty =
            new CustomStyleProperty<Color>("--wf-curve-guide");

        private readonly VisualElement _canvas;
        private readonly Label _caption;

        private AnimationCurve _curve;
        private int _sampleCount = DefaultSampleCount;
        private float _input;
        private float _output;
        private bool _hasInput;

        private Color? _curveColor;
        private Color? _axisColor;
        private Color? _markerColor;
        private Color? _guideColor;

        private IVisualElementScheduledItem _repaintItem;

        /// <summary>
        /// Creates an empty preview. The element works with no further setup: add it to any
        /// container, then call <see cref="SetCurve"/> and, in play mode, <see cref="SetInput"/>.
        /// </summary>
        public ResponseCurvePreview()
        {
            AddToClassList(ussClassName);
            WiseFelineDebugElementStyles.Apply(this);

            _canvas = new VisualElement();
            _canvas.AddToClassList(canvasUssClassName);
            _canvas.pickingMode = PickingMode.Ignore;
            _canvas.generateVisualContent += OnGenerateVisualContent;
            Add(_canvas);

            _caption = new Label(string.Empty);
            _caption.AddToClassList(captionUssClassName);
            _caption.pickingMode = PickingMode.Ignore;
            Add(_caption);

            _canvas.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <summary>
        /// How many points the curve is sampled at when it is drawn. Higher numbers make a sharp
        /// curve smoother at the cost of a little more work per repaint. Values below two are
        /// treated as two.
        /// </summary>
        [UxmlAttribute]
        public int SampleCount
        {
            get => _sampleCount;
            set
            {
                int wanted = Mathf.Max(2, value);
                if (wanted == _sampleCount)
                    return;
                _sampleCount = wanted;
                _canvas.MarkDirtyRepaint();
            }
        }

        /// <summary>The curve which is being drawn, or null when none has been set.</summary>
        public AnimationCurve Curve => _curve;

        /// <summary>True while a marker is shown, which is between a call to <see cref="SetInput"/> and the next call to <see cref="ClearInput"/>.</summary>
        public bool HasInput => _hasInput;

        /// <summary>The input the marker sits at, from zero to one. Meaningless while <see cref="HasInput"/> is false.</summary>
        public float Input => _input;

        /// <summary>The output the marker sits at. Meaningless while <see cref="HasInput"/> is false.</summary>
        public float Output => _output;

        /// <summary>
        /// Sets the curve to draw. Passing null leaves an empty frame, which is what an element
        /// bound to a consideration with no curve should show.
        /// </summary>
        /// <param name="curve">The response curve of a consideration, or null to draw nothing.</param>
        public void SetCurve(AnimationCurve curve)
        {
            _curve = curve;
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>
        /// Shows a marker at one point on the curve, normally the value a consideration produced on
        /// its last evaluation.
        /// </summary>
        /// <param name="normalizedInput">The input the consideration fed into the curve. It is clamped to the zero to one range the curve is drawn over.</param>
        /// <param name="output">The value the curve produced. It may go above one or below zero; the vertical range grows to fit it.</param>
        public void SetInput(float normalizedInput, float output)
        {
            _input = Mathf.Clamp01(normalizedInput);
            _output = output;
            _hasInput = true;
            _caption.text = $"Input {_input:0.000}, output {_output:0.000}";
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>
        /// Hides the marker and clears the caption, which is what to call when leaving play mode or
        /// when the selected consideration has not been evaluated yet.
        /// </summary>
        public void ClearInput()
        {
            _hasInput = false;
            _caption.text = string.Empty;
            _canvas.MarkDirtyRepaint();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (_repaintItem == null)
                _repaintItem = schedule.Execute(_canvas.MarkDirtyRepaint).Every(RepaintIntervalMilliseconds);
            else
                _repaintItem.Resume();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (_repaintItem != null)
                _repaintItem.Pause();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            _curveColor = evt.customStyle.TryGetValue(CurveColorProperty, out var curve) ? curve : (Color?)null;
            _axisColor = evt.customStyle.TryGetValue(AxisColorProperty, out var axis) ? axis : (Color?)null;
            _markerColor = evt.customStyle.TryGetValue(MarkerColorProperty, out var marker) ? marker : (Color?)null;
            _guideColor = evt.customStyle.TryGetValue(GuideColorProperty, out var guide) ? guide : (Color?)null;
            _canvas.MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var rect = _canvas.contentRect;
            if (rect.width < MinimumDrawSize || rect.height < MinimumDrawSize)
                return;

            Color curveColor = _curveColor ?? WiseFelineDebugElementStyles.ReadableColor(
                _canvas.resolvedStyle.color, _canvas.resolvedStyle.backgroundColor);
            Color axisColor = _axisColor ?? WiseFelineDebugElementStyles.WithAlphaFactor(curveColor, AxisAlphaFactor);
            Color guideColor = _guideColor ?? WiseFelineDebugElementStyles.WithAlphaFactor(curveColor, GuideAlphaFactor);
            Color markerColor = _markerColor ?? curveColor;

            var painter = context.painter2D;

            // The vertical range always contains zero to one so two previews next to each other can
            // be compared, and it grows when the curve leaves that range.
            float lowest = 0f;
            float highest = 1f;
            bool hasCurve = _curve != null && _curve.length > 0;
            if (hasCurve)
            {
                for (int i = 0; i <= _sampleCount; i++)
                {
                    float value = _curve.Evaluate(i / (float)_sampleCount);
                    lowest = Mathf.Min(lowest, value);
                    highest = Mathf.Max(highest, value);
                }
            }
            if (_hasInput)
            {
                lowest = Mathf.Min(lowest, _output);
                highest = Mathf.Max(highest, _output);
            }
            float span = Mathf.Max(Mathf.Epsilon, highest - lowest);

            DrawFrame(painter, rect, axisColor);

            // A line at the value one, drawn only when the range is taller than zero to one, so the
            // usual range stays readable on a curve which overshoots.
            if (highest > 1f + Mathf.Epsilon)
                DrawHorizontalLine(painter, rect, ValueToY(rect, 1f, lowest, span), axisColor, AxisWidth);
            // A line at the value zero, drawn only when the curve dips below it.
            if (lowest < -Mathf.Epsilon)
                DrawHorizontalLine(painter, rect, ValueToY(rect, 0f, lowest, span), axisColor, AxisWidth);

            if (hasCurve)
            {
                painter.strokeColor = curveColor;
                painter.lineWidth = CurveWidth;
                painter.BeginPath();
                for (int i = 0; i <= _sampleCount; i++)
                {
                    float x = i / (float)_sampleCount;
                    var point = new Vector2(
                        rect.xMin + (x * rect.width),
                        ValueToY(rect, _curve.Evaluate(x), lowest, span));
                    if (i == 0)
                        painter.MoveTo(point);
                    else
                        painter.LineTo(point);
                }
                painter.Stroke();
            }

            if (!_hasInput)
                return;

            var markerPoint = new Vector2(
                rect.xMin + (_input * rect.width),
                ValueToY(rect, _output, lowest, span));

            painter.strokeColor = guideColor;
            painter.lineWidth = GuideWidth;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, markerPoint.y));
            painter.LineTo(markerPoint);
            painter.MoveTo(new Vector2(markerPoint.x, rect.yMax));
            painter.LineTo(markerPoint);
            painter.Stroke();

            painter.fillColor = markerColor;
            painter.BeginPath();
            painter.Arc(markerPoint, MarkerRadius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();
        }

        private static void DrawFrame(Painter2D painter, Rect rect, Color color)
        {
            painter.strokeColor = color;
            painter.lineWidth = AxisWidth;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Stroke();
        }

        private static void DrawHorizontalLine(Painter2D painter, Rect rect, float y, Color color, float width)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, y));
            painter.LineTo(new Vector2(rect.xMax, y));
            painter.Stroke();
        }

        private static float ValueToY(Rect rect, float value, float lowest, float span) =>
            rect.yMax - (((value - lowest) / span) * rect.height);
    }
}
