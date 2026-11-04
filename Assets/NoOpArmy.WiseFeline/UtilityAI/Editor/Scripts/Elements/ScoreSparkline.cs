using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// A small chart which shows the recent history of a single score as a polyline.
    /// Feed it one sample at a time with <see cref="Add"/> and it keeps the last
    /// <see cref="Capacity"/> samples in a ring buffer, drawing the oldest sample at the left
    /// edge and the newest one at the right edge.
    /// </summary>
    /// <remarks>
    /// The element repaints itself on a timer while it is attached to a panel, so a line which is
    /// fed from an editor update loop keeps moving without the owning window forcing a repaint.
    /// The timer is stopped when the element leaves the panel, so closing a window leaves nothing
    /// running.
    /// All colors come from the stylesheet. The element adds DebugElements.uss to itself when it is
    /// constructed, which sets the theme colors for both editor skins. If that stylesheet cannot be
    /// found the element falls back to the resolved text color of the element, so it still draws
    /// something visible.
    /// Picking is turned off by default so that clicks pass through to whatever row the sparkline
    /// sits in. Set pickingMode back to PickingMode.Position if you need the element itself to
    /// receive pointer events.
    /// </remarks>
    [UxmlElement]
    public partial class ScoreSparkline : VisualElement
    {
        /// <summary>The USS class name which every ScoreSparkline carries.</summary>
        public static readonly string ussClassName = "wf-score-sparkline";

        /// <summary>The USS custom property which sets the color of the history line.</summary>
        public static readonly string lineColorPropertyName = "--wf-sparkline-line";

        /// <summary>The USS custom property which sets the color of the baseline.</summary>
        public static readonly string baselineColorPropertyName = "--wf-sparkline-baseline";

        /// <summary>
        /// The USS custom property which sets the color of the area under the line. The area is
        /// only drawn when this color has an alpha above zero, so it is off unless a stylesheet
        /// asks for it.
        /// </summary>
        public static readonly string fillColorPropertyName = "--wf-sparkline-fill";

        /// <summary>How often the element repaints itself while it is attached to a panel, in milliseconds.</summary>
        public const long RepaintIntervalMilliseconds = 250;

        private const int DefaultCapacity = 64;
        private const float MinimumDrawWidth = 8f;
        private const float MinimumDrawHeight = 8f;
        private const float LineWidth = 1.5f;
        private const float BaselineWidth = 1f;
        private const float BaselineAlphaFactor = 0.25f;

        private static readonly CustomStyleProperty<Color> LineColorProperty =
            new CustomStyleProperty<Color>("--wf-sparkline-line");
        private static readonly CustomStyleProperty<Color> BaselineColorProperty =
            new CustomStyleProperty<Color>("--wf-sparkline-baseline");
        private static readonly CustomStyleProperty<Color> FillColorProperty =
            new CustomStyleProperty<Color>("--wf-sparkline-fill");

        private float[] _values = new float[DefaultCapacity];
        private int _head;
        private int _count;

        private float _max = 1f;
        private bool _autoScale;

        private Color? _lineColor;
        private Color? _baselineColor;
        private Color? _fillColor;

        private IVisualElementScheduledItem _repaintItem;

        /// <summary>
        /// Creates an empty sparkline. The element works with no further setup: add it to any
        /// container and start calling <see cref="Add"/>.
        /// </summary>
        public ScoreSparkline()
        {
            AddToClassList(ussClassName);
            WiseFelineDebugElementStyles.Apply(this);

            pickingMode = PickingMode.Ignore;

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <summary>
        /// How many samples the history keeps. Older samples are dropped once the buffer is full.
        /// Setting this discards the samples recorded so far. Values below one are treated as one.
        /// </summary>
        [UxmlAttribute]
        public int Capacity
        {
            get => _values.Length;
            set
            {
                int wanted = Mathf.Max(1, value);
                if (wanted == _values.Length)
                    return;
                _values = new float[wanted];
                _head = 0;
                _count = 0;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// The value which maps to the top edge of the chart. Zero maps to the bottom edge.
        /// Ignored while <see cref="AutoScale"/> is on. Values of zero or less are treated as a
        /// very small positive number so the drawing code never divides by zero.
        /// </summary>
        [UxmlAttribute]
        public float Max
        {
            get => _max;
            set
            {
                float wanted = Mathf.Max(Mathf.Epsilon, value);
                if (Mathf.Approximately(wanted, _max))
                    return;
                _max = wanted;
                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// When true the top edge of the chart follows the largest sample currently in the buffer
        /// instead of <see cref="Max"/>, so a line which never gets close to the fixed maximum
        /// still fills the chart. Off by default, because a fixed range makes two sparklines next
        /// to each other comparable.
        /// </summary>
        [UxmlAttribute]
        public bool AutoScale
        {
            get => _autoScale;
            set
            {
                if (_autoScale == value)
                    return;
                _autoScale = value;
                MarkDirtyRepaint();
            }
        }

        /// <summary>How many samples are currently recorded. Never larger than <see cref="Capacity"/>.</summary>
        public int Count => _count;

        /// <summary>
        /// Records one sample. When the buffer is full the oldest sample is dropped.
        /// </summary>
        /// <param name="value">The score to record. Values outside the drawn range are clamped when drawn, not when stored.</param>
        public void Add(float value)
        {
            _values[_head] = value;
            _head = (_head + 1) % _values.Length;
            if (_count < _values.Length)
                _count++;
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Drops every recorded sample and leaves an empty chart. This hides the Clear method of
        /// VisualElement, which removes child elements. A sparkline never has children, so nothing
        /// is lost by hiding it.
        /// </summary>
        public new void Clear()
        {
            _head = 0;
            _count = 0;
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Reads back one recorded sample. Index zero is the oldest sample which is still in the
        /// buffer and index <see cref="Count"/> minus one is the newest one.
        /// </summary>
        /// <param name="index">The position in the history, from zero to <see cref="Count"/> minus one.</param>
        /// <returns>The recorded value, or zero when the index is out of range.</returns>
        public float GetValue(int index)
        {
            if (index < 0 || index >= _count)
                return 0f;
            int start = _count < _values.Length ? 0 : _head;
            return _values[(start + index) % _values.Length];
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (_repaintItem == null)
                _repaintItem = schedule.Execute(MarkDirtyRepaint).Every(RepaintIntervalMilliseconds);
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
            _lineColor = evt.customStyle.TryGetValue(LineColorProperty, out var line) ? line : (Color?)null;
            _baselineColor = evt.customStyle.TryGetValue(BaselineColorProperty, out var baseline) ? baseline : (Color?)null;
            _fillColor = evt.customStyle.TryGetValue(FillColorProperty, out var fill) ? fill : (Color?)null;
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var rect = contentRect;
            if (rect.width < MinimumDrawWidth || rect.height < MinimumDrawHeight)
                return;

            Color lineColor = _lineColor ?? WiseFelineDebugElementStyles.ReadableColor(resolvedStyle.color, resolvedStyle.backgroundColor);
            Color baselineColor = _baselineColor ?? WiseFelineDebugElementStyles.WithAlphaFactor(lineColor, BaselineAlphaFactor);

            var painter = context.painter2D;

            // The baseline sits at half of the drawn range so a score can be read as above or below
            // the middle without counting pixels.
            painter.lineWidth = BaselineWidth;
            painter.strokeColor = baselineColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.center.y));
            painter.LineTo(new Vector2(rect.xMax, rect.center.y));
            painter.Stroke();

            if (_count < 2)
                return;

            float range = _max;
            if (AutoScale)
            {
                float largest = 0f;
                for (int i = 0; i < _count; i++)
                    largest = Mathf.Max(largest, GetValue(i));
                range = Mathf.Max(Mathf.Epsilon, largest);
            }

            // The line grows from the left edge until the buffer is full, then it scrolls, so the
            // horizontal spacing between two samples stays the same for the life of the element.
            int slots = Mathf.Max(1, _values.Length - 1);

            if (_fillColor.HasValue && _fillColor.Value.a > 0f)
            {
                painter.fillColor = _fillColor.Value;
                painter.BeginPath();
                painter.MoveTo(new Vector2(SampleX(rect, 0, slots), rect.yMax));
                for (int i = 0; i < _count; i++)
                    painter.LineTo(new Vector2(SampleX(rect, i, slots), SampleY(rect, i, range)));
                painter.LineTo(new Vector2(SampleX(rect, _count - 1, slots), rect.yMax));
                painter.ClosePath();
                painter.Fill();
            }

            painter.lineWidth = LineWidth;
            painter.strokeColor = lineColor;
            painter.BeginPath();
            for (int i = 0; i < _count; i++)
            {
                var point = new Vector2(SampleX(rect, i, slots), SampleY(rect, i, range));
                if (i == 0)
                    painter.MoveTo(point);
                else
                    painter.LineTo(point);
            }
            painter.Stroke();
        }

        private static float SampleX(Rect rect, int index, int slots) =>
            rect.xMin + (index / (float)slots) * rect.width;

        private float SampleY(Rect rect, int index, float range) =>
            rect.yMax - Mathf.Clamp01(GetValue(index) / range) * rect.height;
    }

    /// <summary>
    /// Shared helpers for the editor debug elements: finding the stylesheet which carries their
    /// theme colors, and working out a usable color when that stylesheet is not there.
    /// </summary>
    internal static class WiseFelineDebugElementStyles
    {
        private const string StyleSheetFileName = "DebugElements.uss";

        private static StyleSheet _styleSheet;
        private static bool _searchFailed;

        /// <summary>
        /// Adds DebugElements.uss to the element if it is not on it already. The stylesheet is
        /// looked up by name through the asset database rather than by a fixed folder path, so the
        /// package folder can be moved or renamed without breaking the elements.
        /// </summary>
        public static void Apply(VisualElement element)
        {
            var sheet = Load();
            if (sheet == null)
                return;
            if (!element.styleSheets.Contains(sheet))
                element.styleSheets.Add(sheet);
        }

        /// <summary>
        /// Returns the given color when it is visible, and a color which contrasts with the given
        /// background when it is not. This is only reached when the stylesheet is missing, and it
        /// exists so that an element created from code always draws something the user can see.
        /// </summary>
        public static Color ReadableColor(Color wanted, Color background)
        {
            if (wanted.a > 0.01f)
                return wanted;
            if (background.a <= 0.01f)
                return Color.gray;
            float luminance = (background.r * 0.299f) + (background.g * 0.587f) + (background.b * 0.114f);
            return luminance > 0.5f ? Color.black : Color.white;
        }

        /// <summary>Returns the same color with its alpha multiplied by the given factor.</summary>
        public static Color WithAlphaFactor(Color color, float factor) =>
            new Color(color.r, color.g, color.b, color.a * factor);

        private static StyleSheet Load()
        {
            if (_styleSheet != null)
                return _styleSheet;
            if (_searchFailed)
                return null;

            foreach (var guid in AssetDatabase.FindAssets("DebugElements t:StyleSheet"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(StyleSheetFileName, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet == null)
                    continue;
                _styleSheet = sheet;
                return _styleSheet;
            }

            // Remembered so that a project without the stylesheet does not search the asset
            // database once per element. The flag is reset by the next domain reload.
            _searchFailed = true;
            return null;
        }
    }
}
