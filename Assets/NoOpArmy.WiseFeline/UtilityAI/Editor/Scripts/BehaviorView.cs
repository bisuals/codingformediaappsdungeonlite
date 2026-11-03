using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// The body of the Wise Feline window: the toolbar, the three lists and the Properties panel.
    /// It edits an Agent Behavior asset in edit mode and shows the live scores of a Brain in play mode.
    /// </summary>
    [UxmlElement]
    public partial class BehaviorView : VisualElement
    {
        private const int SearchWindowWidth = 320;
        private const int SearchWindowHeight = 420;

        private const string AgentDestroyedMessage = "The agent was destroyed. Select another GameObject with a Brain component to keep debugging.";

        // The clipboard is static so that Copy in one window pastes in another one, and it stores the
        // asset path plus the guid of the copied object instead of list indices, which go stale as soon
        // as the source list is reordered or edited.
        private static string _clipboardAssetPath;
        private static string _clipboardGuid;
        private static int _clipboardKind = -1;

        /// <summary>
        /// The window that hosts this view.
        /// </summary>
        public UtilityAIEditor window;

        private AgentBehavior _behavior;
        private Brain _brain;
        private bool _playMode;
        private bool _readOnly;

        private Label _title;
        private Label _emptyState;

        private ScrollView _actionSetList;
        private ScrollView _actionList;
        private ScrollView _considerationList;

        private Label _actionSetHint;
        private Label _actionHint;
        private Label _considerationHint;

        private Button _actionSetButton;
        private Button _actionButton;
        private Button _considerationButton;

        private InspectorView _inspector;
        private ToolbarToggle _actionSortToggle;
        private ToolbarToggle _freezeToggle;

        private readonly List<FieldView> _actionSetViews = new List<FieldView>();
        private readonly List<FieldView> _actionViews = new List<FieldView>();
        private readonly List<FieldView> _considerationViews = new List<FieldView>();

        private ActionSet _currentActionSet;
        private ActionBase _currentAction;
        private ConsiderationBase _currentConsideration;
        private AIObject _inspectedObject;
        private AIObject _inspectorTarget;

        // Which action was last selected inside each set, and which consideration inside each action,
        // so that going back to a set restores the row the user was working on. Keyed by guid, because
        // a destroyed Unity object is a poor dictionary key.
        private readonly Dictionary<string, string> _lastActionPerSet = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _lastConsiderationPerAction = new Dictionary<string, string>();

        private VisualTreeAsset _rowTemplate;
        private ActionData _lastActionData;

        // The live widgets under the Properties panel in play mode. They are rebuilt on every selection
        // change together with the inspector, so a null here means the panel shows nothing that has them.
        private ResponseCurvePreview _curvePreview;
        private ScoreSparkline _scoreHistory;
        private Label _liveValueLabel;

        /// <summary>
        /// Raised after every score update the view applied, with the payload of the think that caused
        /// it. It does not fire while the Freeze toggle is on. Debug widgets that need to follow the
        /// scores (a sparkline, a curve marker) can hook this instead of the brain itself.
        /// </summary>
        public event Action<ActionData> ScoresUpdated;

        /// <summary>
        /// The consideration shown in the Properties panel, or null when the panel shows something else.
        /// It is the consideration the debug-extras container of the Properties panel belongs to.
        /// </summary>
        public ConsiderationBase CurrentConsideration
        {
            get { return _inspectedObject as ConsiderationBase; }
        }

        /// <summary>
        /// The Properties panel. Its DebugExtras container is the place to add debug only widgets.
        /// </summary>
        public InspectorView Inspector
        {
            get { return _inspector; }
        }

        /// <summary>
        /// Can the shown behavior be changed at the moment. It is false in play mode, when nothing is
        /// shown and when the asset is read only.
        /// </summary>
        public bool CanEdit
        {
            get { return _behavior != null && !_playMode && !_readOnly; }
        }

        /// <summary>
        /// Is the window showing a running agent instead of an asset.
        /// </summary>
        public bool PlayMode
        {
            get { return _playMode; }
            internal set
            {
                if (_playMode == value)
                    return;

                _playMode = value;
                EnableInClassList("play-mode", value);

                if (_inspector != null)
                    _inspector.SetContentEnabled(!value);

                if (_actionSortToggle != null)
                {
                    _actionSortToggle.EnableInClassList("is-hidden", !value);
                    if (!value)
                        _actionSortToggle.SetValueWithoutNotify(false);
                }

                if (_freezeToggle != null)
                {
                    _freezeToggle.EnableInClassList("is-hidden", !value);
                    if (!value)
                        _freezeToggle.SetValueWithoutNotify(false);
                }
            }
        }

        public BehaviorView()
        {
        }

        public void Init(UtilityAIEditor window)
        {
            this.window = window;

            _title = this.Q<Label>("title");
            _emptyState = this.Q<Label>("empty-state");

            _actionSetList = this.Q<ScrollView>("actionset-container");
            _actionList = this.Q<ScrollView>("actions-container");
            _considerationList = this.Q<ScrollView>("consideration-container");

            _actionSetHint = this.Q<Label>("actionset-hint");
            _actionHint = this.Q<Label>("action-hint");
            _considerationHint = this.Q<Label>("consideration-hint");

            _actionSetButton = this.Q<Button>("add-actionset");
            _actionButton = this.Q<Button>("add-action");
            _considerationButton = this.Q<Button>("add-consideration");

            _inspector = this.Q<InspectorView>("inspector");
            _actionSortToggle = this.Q<ToolbarToggle>("sort-toggle");
            _freezeToggle = this.Q<ToolbarToggle>("freeze-toggle");

            this.Q<ToolbarButton>("about").clicked += ShowAbout;
            this.Q<ToolbarButton>("documentation").clicked += OpenDocumentation;
            this.Q<ToolbarButton>("discord").clicked += OpenDiscord;

            _actionSetButton.clicked += CreateActionSet;
            _actionButton.clicked += ShowActionList;
            _considerationButton.clicked += ShowConsiderationList;

            _actionSortToggle.RegisterValueChangedCallback(OnSortToggleChanged);
            _freezeToggle.RegisterValueChangedCallback(OnFreezeToggleChanged);

            _inspector.SetContentEnabled(true);
            _inspector.ObjectChanged += OnInspectedObjectChanged;

            RegisterColumnMenu(this.Q<VisualElement>("actionset-area"), FieldView.ColumnKind.ActionSet);
            RegisterColumnMenu(this.Q<VisualElement>("action-area"), FieldView.ColumnKind.Action);
            RegisterColumnMenu(this.Q<VisualElement>("consideration-area"), FieldView.ColumnKind.Consideration);

            Undo.undoRedoPerformed += OnUndoRedo;
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            // A destroyed agent stops sending think events, so the view has to notice on its own that
            // what it shows is gone.
            schedule.Execute(CheckAgentAlive).Every(500);

            ClearView();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            DetachModelEvents();
        }

        private void ShowAbout()
        {
            AboutEditor.OpenWindow();
        }

        private void OpenDocumentation()
        {
            Application.OpenURL(WiseFelineEditorInfo.DocumentationUrl);
        }

        private void OpenDiscord()
        {
            Application.OpenURL(WiseFelineEditorInfo.DiscordUrl);
        }

        #region showing a behavior

        /// <summary>
        /// Shows a behavior asset, or the runtime behavior of a brain in play mode.
        /// </summary>
        public void PopulateActionSets(AgentBehavior behavior, Brain brain = null)
        {
            ShowBehavior(behavior, brain, brain != null);
        }

        /// <summary>
        /// Shows a behavior asset, or the runtime behavior of a brain in play mode.
        /// </summary>
        /// <param name="behavior">The behavior to show.</param>
        /// <param name="brain">The brain that runs it, or null in edit mode.</param>
        /// <param name="readOnly">True when the behavior cannot be edited, for instance in play mode.</param>
        public void ShowBehavior(AgentBehavior behavior, Brain brain, bool readOnly)
        {
            if (behavior == null)
            {
                ShowMessage(Application.isPlaying ? UtilityAIEditor.PlayModeHint : UtilityAIEditor.EditModeHint);
                return;
            }

            if (_behavior != behavior)
            {
                _currentActionSet = null;
                _currentAction = null;
                _currentConsideration = null;
                _inspectedObject = null;
                _lastActionPerSet.Clear();
                _lastConsiderationPerAction.Clear();
            }

            DetachModelEvents();

            _behavior = behavior;
            _brain = brain;
            _readOnly = readOnly;

            if (!Application.isPlaying)
                _behavior.DeleteNulls();

            AttachModelEvents();

            _emptyState.AddToClassList("is-hidden");

            UpdateTitle();
            Repopulate();
        }

        /// <summary>
        /// Empties the window and shows one line of text in the middle of it.
        /// </summary>
        public void ShowMessage(string message)
        {
            ClearView();
            _emptyState.text = message;
            _emptyState.RemoveFromClassList("is-hidden");
        }

        /// <summary>
        /// Empties the three lists and the Properties panel.
        /// </summary>
        public void ClearView()
        {
            DetachModelEvents();

            _behavior = null;
            _brain = null;
            _readOnly = false;

            ClearColumn(_actionSetList, _actionSetViews);
            ClearColumn(_actionList, _actionViews);
            ClearColumn(_considerationList, _considerationViews);

            _currentActionSet = null;
            _currentAction = null;
            _currentConsideration = null;
            _inspectedObject = null;
            RefreshInspector(true);

            _title.text = "None";
            _title.tooltip = string.Empty;

            UpdateHints();
            UpdateButtonsState();
        }

        private void AttachModelEvents()
        {
            if (_behavior != null && _playMode)
            {
                _behavior.OnThinkDone -= UpdateScores;
                _behavior.OnThinkDone += UpdateScores;
            }

            if (_brain != null)
            {
                _brain.OnBehaviorListModified -= OnBehaviorModified;
                _brain.OnBehaviorListModified += OnBehaviorModified;
            }
        }

        private void DetachModelEvents()
        {
            if (_behavior != null)
                _behavior.OnThinkDone -= UpdateScores;
            if (_brain != null)
                _brain.OnBehaviorListModified -= OnBehaviorModified;
        }

        /// <summary>
        /// Called when a brain gains or loses action sets at runtime.
        /// </summary>
        public void OnBehaviorModified()
        {
            if (_behavior != null)
                Repopulate();
        }

        private void OnUndoRedo()
        {
            if (_behavior == null || _playMode)
                return;

            _behavior.DeleteNulls();
            Repopulate();
        }

        private void CheckAgentAlive()
        {
            if (!_playMode || _behavior == null)
                return;

            if (_brain == null || _brain.gameObject == null)
                ShowMessage(AgentDestroyedMessage);
        }

        private void UpdateTitle()
        {
            if (_behavior == null)
            {
                _title.text = "None";
                _title.tooltip = string.Empty;
                return;
            }

            string behaviorName = StripClone(_behavior.name);

            if (_playMode && _brain != null)
                _title.text = $"Debugging {StripClone(_brain.gameObject.name)} ({behaviorName})";
            else if (_readOnly)
                _title.text = behaviorName + " (read only)";
            else
                _title.text = behaviorName;

            _title.tooltip = _title.text;
        }

        private static string StripClone(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            const string suffix = "(Clone)";
            string trimmed = name;
            while (trimmed.EndsWith(suffix, StringComparison.Ordinal))
                trimmed = trimmed.Substring(0, trimmed.Length - suffix.Length).TrimEnd();
            return trimmed.Length > 0 ? trimmed : name;
        }

        #endregion

        #region building the three columns

        private void Repopulate()
        {
            if (_behavior == null)
                return;

            string setKey = KeyOf(_currentActionSet);
            string actionKey = KeyOf(_currentAction);
            string considerationKey = KeyOf(_currentConsideration);

            _currentActionSet = ResolveSet(setKey);
            _currentAction = ResolveAction(_currentActionSet, actionKey);
            _currentConsideration = ResolveConsideration(_currentAction, considerationKey);

            RebuildActionSetColumn();
            RebuildActionColumn();
            RebuildConsiderationColumn();

            if (_inspectedObject == null)
                _inspectedObject = PickInspectedObject();

            RefreshInspector(false);
            ApplySelectionClasses();
            UpdateHints();
            UpdateButtonsState();

            if (_playMode)
                UpdateScores(_lastActionData);
        }

        private AIObject PickInspectedObject()
        {
            if (_currentConsideration != null)
                return _currentConsideration;
            if (_currentAction != null)
                return _currentAction;
            return _currentActionSet;
        }

        private ActionSet ResolveSet(string key)
        {
            List<ActionSet> sets = _behavior.ActionSets;
            if (!string.IsNullOrEmpty(key))
            {
                foreach (var set in sets)
                {
                    if (set != null && KeyOf(set) == key)
                        return set;
                }
            }

            foreach (var set in sets)
            {
                if (set != null)
                    return set;
            }
            return null;
        }

        private ActionBase ResolveAction(ActionSet set, string key)
        {
            if (set == null)
                return null;

            if (!string.IsNullOrEmpty(key))
            {
                foreach (var action in set.Actions)
                {
                    if (action != null && KeyOf(action) == key)
                        return action;
                }
            }

            ActionBase remembered = RememberedAction(set);
            if (remembered != null)
                return remembered;

            foreach (var action in set.Actions)
            {
                if (action != null)
                    return action;
            }
            return null;
        }

        private ConsiderationBase ResolveConsideration(ActionBase action, string key)
        {
            if (action == null)
                return null;

            if (!string.IsNullOrEmpty(key))
            {
                foreach (var consideration in action.Considerations)
                {
                    if (consideration != null && KeyOf(consideration) == key)
                        return consideration;
                }
            }

            ConsiderationBase remembered = RememberedConsideration(action);
            if (remembered != null)
                return remembered;

            foreach (var consideration in action.Considerations)
            {
                if (consideration != null)
                    return consideration;
            }
            return null;
        }

        private ActionBase RememberedAction(ActionSet set)
        {
            string setKey = KeyOf(set);
            if (setKey == null || !_lastActionPerSet.TryGetValue(setKey, out string actionKey))
                return null;

            foreach (var action in set.Actions)
            {
                if (action != null && KeyOf(action) == actionKey)
                    return action;
            }
            return null;
        }

        private ConsiderationBase RememberedConsideration(ActionBase action)
        {
            string actionKey = KeyOf(action);
            if (actionKey == null || !_lastConsiderationPerAction.TryGetValue(actionKey, out string considerationKey))
                return null;

            foreach (var consideration in action.Considerations)
            {
                if (consideration != null && KeyOf(consideration) == considerationKey)
                    return consideration;
            }
            return null;
        }

        private void RebuildActionSetColumn()
        {
            ClearColumn(_actionSetList, _actionSetViews);
            foreach (var set in _behavior.ActionSets)
            {
                if (set != null)
                    AddRow(set, _actionSetList, _actionSetViews, FieldView.ColumnKind.ActionSet);
            }
        }

        private void RebuildActionColumn()
        {
            ClearColumn(_actionList, _actionViews);
            if (_currentActionSet == null)
                return;

            foreach (var action in _currentActionSet.Actions)
            {
                if (action != null)
                    AddRow(action, _actionList, _actionViews, FieldView.ColumnKind.Action);
            }
        }

        private void RebuildConsiderationColumn()
        {
            ClearColumn(_considerationList, _considerationViews);
            if (_currentAction == null)
                return;

            foreach (var consideration in _currentAction.Considerations)
            {
                if (consideration != null)
                    AddRow(consideration, _considerationList, _considerationViews, FieldView.ColumnKind.Consideration);
            }
        }

        private static void ClearColumn(ScrollView list, List<FieldView> views)
        {
            if (list != null)
                list.Clear();
            views.Clear();
        }

        private void AddRow(AIObject aiObject, ScrollView list, List<FieldView> views, FieldView.ColumnKind kind)
        {
            FieldView row = CreateFieldView(aiObject, list, kind);
            if (row != null)
                views.Add(row);
        }

        private FieldView CreateFieldView(AIObject aiObject, ScrollView parent, FieldView.ColumnKind kind)
        {
            if (aiObject == null || parent == null)
                return null;

            if (_rowTemplate == null)
                _rowTemplate = UtilityAIEditor.LoadRowTemplate();
            if (_rowTemplate == null)
                return null;

            // The template is cloned into a throwaway container and the row is taken out of it, so that
            // the rows are direct children of the list and nothing sits between them and the ScrollView.
            VisualElement container = _rowTemplate.Instantiate();
            FieldView row = container.Q<FieldView>();
            if (row == null)
                return null;

            row.RemoveFromHierarchy();
            parent.Add(row);
            row.Init(aiObject, this, kind, CanEdit);
            return row;
        }

        private void ApplySelectionClasses()
        {
            foreach (var view in _actionSetViews)
                view.EnableInClassList("row--selected", view.AIObject == (AIObject)_currentActionSet);
            foreach (var view in _actionViews)
                view.EnableInClassList("row--selected", view.AIObject == (AIObject)_currentAction);
            foreach (var view in _considerationViews)
                view.EnableInClassList("row--selected", view.AIObject == (AIObject)_currentConsideration);
        }

        private void UpdateHints()
        {
            bool hasBehavior = _behavior != null;

            SetHint(_actionSetHint, _actionSetViews.Count == 0 && hasBehavior,
                "No action sets yet. Use Add Action Set below.");

            string actionHint = _currentActionSet == null
                ? "Select an action set to see its actions."
                : "No actions in this set yet. Use Add Action below.";
            SetHint(_actionHint, _actionViews.Count == 0 && hasBehavior, actionHint);

            string considerationHint = _currentAction == null
                ? "Select an action to see its considerations."
                : "No considerations on this action yet. Use Add Consideration below.";
            SetHint(_considerationHint, _considerationViews.Count == 0 && hasBehavior, considerationHint);
        }

        private static void SetHint(Label hint, bool visible, string text)
        {
            if (hint == null)
                return;

            hint.text = text;
            hint.EnableInClassList("is-hidden", !visible);
        }

        private void UpdateButtonsState()
        {
            bool canEdit = CanEdit;
            _actionSetButton.SetEnabled(canEdit);
            _actionButton.SetEnabled(canEdit && _currentActionSet != null);
            _considerationButton.SetEnabled(canEdit && _currentAction != null);
        }

        private void RefreshInspector(bool force)
        {
            // ReferenceEquals and not the Unity comparison, so that a destroyed object never counts as
            // "the same as the last one" and leaves a dead editor on screen.
            if (!force && _inspectedObject != null && ReferenceEquals(_inspectorTarget, _inspectedObject))
                return;

            _inspectorTarget = _inspectedObject;
            bool showExtras = _playMode && (_inspectedObject is ConsiderationBase || _inspectedObject is ActionBase);
            _inspector.UpdateSelection(_inspectedObject, showExtras);
            BuildDebugExtras(showExtras);
        }

        /// <summary>
        /// Fills the debug-extras container of the Properties panel: the response curve with the live
        /// marker and the score history for a consideration, the score history alone for an action.
        /// </summary>
        private void BuildDebugExtras(bool show)
        {
            _curvePreview = null;
            _scoreHistory = null;
            _liveValueLabel = null;

            if (!show || _inspector == null)
                return;

            VisualElement extras = _inspector.DebugExtras;

            if (_inspectedObject is ConsiderationBase consideration)
            {
                extras.Add(MakeExtrasHeader("Response curve"));

                _curvePreview = new ResponseCurvePreview();
                _curvePreview.SetCurve(consideration.Curve);
                extras.Add(_curvePreview);

                _liveValueLabel = new Label();
                _liveValueLabel.AddToClassList("debug-extras__value");
                extras.Add(_liveValueLabel);

                extras.Add(MakeExtrasHeader("Score history"));
                _scoreHistory = new ScoreSparkline();
                extras.Add(_scoreHistory);
            }
            else if (_inspectedObject is ActionBase)
            {
                extras.Add(MakeExtrasHeader("Score history"));
                // An action score is the product of its considerations times the weight, so it is not
                // bound to one. The chart follows the largest value seen instead of a fixed top.
                _scoreHistory = new ScoreSparkline { AutoScale = true };
                extras.Add(_scoreHistory);
            }

            UpdateDebugExtras(false);
        }

        private static Label MakeExtrasHeader(string text)
        {
            var label = new Label(text);
            label.AddToClassList("debug-extras__header");
            return label;
        }

        /// <summary>
        /// Pushes the latest values into the live widgets. Called once when they are built, with
        /// addSample false so the history does not start with a duplicate, and after every think.
        /// </summary>
        private void UpdateDebugExtras(bool addSample)
        {
            if (_inspectedObject is ConsiderationBase consideration)
            {
                if (_curvePreview != null)
                {
                    _curvePreview.SetCurve(consideration.Curve);
                    _curvePreview.SetInput(consideration.LastNormalizedInput, consideration.Score);
                }

                if (_liveValueLabel != null)
                {
                    _liveValueLabel.text = string.Format(CultureInfo.InvariantCulture,
                        "Raw value {0:0.###} in the range {1:0.###} to {2:0.###}",
                        consideration.LastRawValue, consideration.MinRange, consideration.MaxRange);
                }

                if (_scoreHistory != null && addSample)
                    _scoreHistory.Add(consideration.Score);
            }
            else if (_inspectedObject is ActionBase action)
            {
                if (_scoreHistory != null && addSample)
                    _scoreHistory.Add(action.Score);
            }
        }

        #endregion

        #region selection

        /// <summary>
        /// Called by a row when the user clicks it.
        /// </summary>
        public void RowClicked(FieldView fieldView)
        {
            if (fieldView == null || fieldView.AIObject == null)
                return;

            switch (fieldView.Kind)
            {
                case FieldView.ColumnKind.ActionSet:
                    SelectActionSet(fieldView.AIObject as ActionSet);
                    break;
                case FieldView.ColumnKind.Action:
                    SelectAction(fieldView.AIObject as ActionBase);
                    break;
                case FieldView.ColumnKind.Consideration:
                    SelectConsideration(fieldView.AIObject as ConsiderationBase);
                    break;
            }
        }

        /// <summary>
        /// Kept for compatibility. Same as RowClicked.
        /// </summary>
        public void FieldSelected(FieldView fieldView)
        {
            RowClicked(fieldView);
        }

        private void SelectActionSet(ActionSet set)
        {
            if (set == null)
                return;

            if (_currentActionSet != set)
            {
                _currentActionSet = set;
                _currentAction = ResolveAction(set, null);
                _currentConsideration = ResolveConsideration(_currentAction, null);
                RebuildActionColumn();
                RebuildConsiderationColumn();
            }

            // The columns follow the click but the Properties panel shows what was clicked, not the
            // first child of it.
            _inspectedObject = set;
            RefreshInspector(false);
            ApplySelectionClasses();
            UpdateHints();
            UpdateButtonsState();
            if (_playMode)
                UpdateScores(_lastActionData);
        }

        private void SelectAction(ActionBase action)
        {
            if (action == null)
                return;

            if (_currentAction != action)
            {
                _currentAction = action;
                _currentConsideration = ResolveConsideration(action, null);
                RebuildConsiderationColumn();
            }

            if (_currentActionSet != null)
                _lastActionPerSet[KeyOf(_currentActionSet)] = KeyOf(action);

            _inspectedObject = action;
            RefreshInspector(false);
            ApplySelectionClasses();
            UpdateHints();
            UpdateButtonsState();
            if (_playMode)
                UpdateScores(_lastActionData);
        }

        private void SelectConsideration(ConsiderationBase consideration)
        {
            if (consideration == null)
                return;

            _currentConsideration = consideration;
            if (_currentAction != null)
                _lastConsiderationPerAction[KeyOf(_currentAction)] = KeyOf(consideration);

            _inspectedObject = consideration;
            RefreshInspector(false);
            ApplySelectionClasses();
            UpdateButtonsState();
        }

        /// <summary>
        /// Moves the selection up or down inside the column that the given row belongs to.
        /// </summary>
        internal void MoveSelection(FieldView fieldView, int delta)
        {
            if (fieldView == null || fieldView.parent == null)
                return;

            // The visual order is used, which is what the user sees when Sort Actions is on.
            List<FieldView> rows = fieldView.parent.Children().OfType<FieldView>().ToList();
            int index = rows.IndexOf(fieldView);
            if (index < 0)
                return;

            int next = Mathf.Clamp(index + delta, 0, rows.Count - 1);
            if (next == index)
                return;

            FieldView target = rows[next];
            RowClicked(target);
            ListFor(fieldView.Kind)?.ScrollTo(target);
            target.Focus();
        }

        private void FocusRowFor(AIObject aiObject)
        {
            FieldView row = FindRow(aiObject);
            if (row == null)
                return;

            row.Focus();
            // A row that was just created has no layout yet, so scrolling to it has to wait one frame.
            ScrollView list = ListFor(row.Kind);
            if (list != null)
                row.schedule.Execute(() => list.ScrollTo(row)).ExecuteLater(0);
        }

        private FieldView FindRow(AIObject aiObject)
        {
            if (aiObject == null)
                return null;

            foreach (var view in _actionSetViews)
                if (view.AIObject == aiObject)
                    return view;
            foreach (var view in _actionViews)
                if (view.AIObject == aiObject)
                    return view;
            foreach (var view in _considerationViews)
                if (view.AIObject == aiObject)
                    return view;
            return null;
        }

        private void RefreshRowFor(AIObject aiObject)
        {
            FieldView row = FindRow(aiObject);
            if (row != null)
            {
                row.RefreshName();
                row.RefreshTag();
            }
        }

        private ScrollView ListFor(FieldView.ColumnKind kind)
        {
            switch (kind)
            {
                case FieldView.ColumnKind.ActionSet:
                    return _actionSetList;
                case FieldView.ColumnKind.Action:
                    return _actionList;
                default:
                    return _considerationList;
            }
        }

        private List<FieldView> ViewsFor(FieldView.ColumnKind kind)
        {
            switch (kind)
            {
                case FieldView.ColumnKind.ActionSet:
                    return _actionSetViews;
                case FieldView.ColumnKind.Action:
                    return _actionViews;
                default:
                    return _considerationViews;
            }
        }

        /// <summary>
        /// The behavior the window shows, or null.
        /// </summary>
        public AgentBehavior GetBehavior()
        {
            return _behavior;
        }

        #endregion

        #region adding, copying and deleting

        private void CreateActionSet()
        {
            if (!CanEdit)
                return;

            ActionSet set = _behavior.AddActionSet("New Action Set");
            _currentActionSet = set;
            _currentAction = null;
            _currentConsideration = null;
            _inspectedObject = set;
            Repopulate();
            FocusRowFor(set);
        }

        private void ShowActionList()
        {
            if (!CanEdit || _currentActionSet == null)
                return;
            CreateSearchableWindow(typeof(ActionBase), CreateAction, _actionButton);
        }

        private void ShowConsiderationList()
        {
            if (!CanEdit || _currentAction == null)
                return;
            CreateSearchableWindow(typeof(ConsiderationBase), CreateConsideration, _considerationButton);
        }

        private void CreateSearchableWindow(Type baseType, Action<Type> callback, VisualElement anchor)
        {
            var provider = ScriptableObject.CreateInstance<AITypesSearchProvider>();
            provider.baseType = baseType;
            provider.OnTypeSelected = callback;

            // The popup is anchored under the button instead of at the mouse position, because
            // Event.current is not a mouse event when the button is activated with the keyboard.
            // GUIToScreenPoint maps panel coordinates to the screen from inside the window's own event
            // handling, which is where a clicked callback runs, whatever event triggered it.
            Vector2 point = GUIUtility.GUIToScreenPoint(new Vector2(anchor.worldBound.xMin, anchor.worldBound.yMax));

            SearchWindow.Open(new SearchWindowContext(point, SearchWindowWidth, SearchWindowHeight), provider);
        }

        private void CreateAction(Type type)
        {
            if (!CanEdit || _currentActionSet == null || type == null)
                return;

            ActionBase action = _behavior.AddAction(type, _currentActionSet);
            _currentAction = action;
            _currentConsideration = null;
            _inspectedObject = action;
            Repopulate();
            FocusRowFor(action);
        }

        private void CreateConsideration(Type type)
        {
            if (!CanEdit || _currentAction == null || type == null)
                return;

            ConsiderationBase consideration = _behavior.AddConsideration(type, _currentAction);
            _currentConsideration = consideration;
            _inspectedObject = consideration;
            Repopulate();
            FocusRowFor(consideration);
        }

        /// <summary>
        /// Duplicates an action set, action or consideration into the behavior the window shows.
        /// The copy becomes the selected row.
        /// </summary>
        public void DuplicateObject(AIObject aiObject)
        {
            if (!CanEdit || aiObject == null)
                return;

            AIObject created = null;

            if (aiObject is ActionSet set)
            {
                ActionSet clone = set.Clone(_behavior);
                created = _behavior.AddCopiedActionSet(clone);
                _currentActionSet = created as ActionSet;
                _currentAction = null;
                _currentConsideration = null;
            }
            else if (aiObject is ActionBase action)
            {
                if (_currentActionSet == null)
                    return;
                ActionBase clone = action.Clone(_currentActionSet);
                created = _behavior.AddCopiedAction(clone, _currentActionSet);
                _currentAction = created as ActionBase;
                _currentConsideration = null;
            }
            else if (aiObject is ConsiderationBase consideration)
            {
                if (_currentAction == null)
                    return;
                ConsiderationBase clone = consideration.Clone();
                created = _behavior.AddCopiedConsideration(clone, _currentAction);
                _currentConsideration = created as ConsiderationBase;
            }

            if (created == null)
                return;

            _inspectedObject = created;
            Repopulate();
            FocusRowFor(created);
        }

        /// <summary>
        /// Kept for compatibility. Same as DuplicateObject.
        /// </summary>
        public void DuplicateField(AIObject aiObject)
        {
            DuplicateObject(aiObject);
        }

        /// <summary>
        /// Deletes the object a row shows and selects its neighbour in the same column.
        /// </summary>
        public void DeleteField(FieldView fieldView)
        {
            if (!CanEdit || fieldView == null || fieldView.AIObject == null)
                return;

            List<FieldView> views = ViewsFor(fieldView.Kind);
            int index = views.IndexOf(fieldView);
            AIObject neighbour = null;
            if (index >= 0)
            {
                if (index + 1 < views.Count)
                    neighbour = views[index + 1].AIObject;
                else if (index > 0)
                    neighbour = views[index - 1].AIObject;
            }

            AIObject deleted = fieldView.AIObject;

            // Which of the three pointers the deleted object is has to be known before it is destroyed.
            // Two destroyed objects compare equal to each other, so asking afterwards gives wrong answers.
            bool wasCurrentSet = ReferenceEquals(deleted, _currentActionSet);
            bool wasCurrentAction = ReferenceEquals(deleted, _currentAction);
            bool wasCurrentConsideration = ReferenceEquals(deleted, _currentConsideration);

            _behavior.DeleteAsset(deleted);

            if (wasCurrentSet)
            {
                _currentActionSet = neighbour as ActionSet;
                _currentAction = null;
                _currentConsideration = null;
            }
            else if (wasCurrentAction)
            {
                _currentAction = neighbour as ActionBase;
                _currentConsideration = null;
            }
            else if (wasCurrentConsideration)
            {
                _currentConsideration = neighbour as ConsiderationBase;
            }

            _inspectedObject = neighbour;
            Repopulate();
            FocusRowFor(neighbour);
        }

        /// <summary>
        /// Renames an object and keeps the name of the sub-asset in the Project window in sync with it.
        /// </summary>
        public void RenameObject(AIObject aiObject, string newName)
        {
            if (!CanEdit || aiObject == null || string.IsNullOrEmpty(newName))
                return;
            if (aiObject.Name == newName)
                return;

            Undo.IncrementCurrentGroup();
            Undo.RecordObject(aiObject, "Wise Feline (Rename)");
            aiObject.Name = newName;
            aiObject.SyncObjectName();
            EditorUtility.SetDirty(aiObject);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            AssetDatabase.SaveAssets();

            RefreshRowFor(aiObject);
            UpdateTitle();
        }

        private void OnInspectedObjectChanged(UnityEngine.Object changed)
        {
            if (!(changed is AIObject aiObject))
                return;

            if (!_playMode)
            {
                string before = aiObject.name;
                aiObject.SyncObjectName();
                if (before != aiObject.name)
                    EditorUtility.SetDirty(aiObject);
            }

            RefreshRowFor(aiObject);
        }

        #endregion

        #region reordering

        /// <summary>
        /// Moves an object to another position inside the list it belongs to. One Undo step.
        /// </summary>
        internal void MoveObject(AIObject aiObject, int newIndex)
        {
            if (!CanEdit || aiObject == null)
                return;

            if (aiObject is ActionSet set)
                Reorder(_behavior, _behavior.ActionSets, set, newIndex, "Wise Feline (Move Action Set)");
            else if (aiObject is ActionBase action)
            {
                ActionSet owner = _behavior.GetActionSet(action);
                if (owner == null)
                    return;
                Reorder(owner, owner.Actions, action, newIndex, "Wise Feline (Move Action)");
            }
            else if (aiObject is ConsiderationBase consideration)
            {
                ActionBase owner = _behavior.GetAction(consideration);
                if (owner == null)
                    return;
                Reorder(owner, owner.Considerations, consideration, newIndex, "Wise Feline (Move Consideration)");
            }
            else
                return;

            Repopulate();
            FocusRowFor(aiObject);
        }

        private static void Reorder<T>(UnityEngine.Object owner, List<T> list, T item, int newIndex, string undoName) where T : class
        {
            int oldIndex = list.IndexOf(item);
            if (oldIndex < 0)
                return;

            newIndex = Mathf.Clamp(newIndex, 0, list.Count - 1);
            if (newIndex == oldIndex)
                return;

            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(owner, undoName);
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
            EditorUtility.SetDirty(owner);
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            AssetDatabase.SaveAssets();
        }

        private void MoveRelative(FieldView fieldView, int delta)
        {
            int index = ModelIndexOf(fieldView);
            if (index < 0)
                return;
            MoveObject(fieldView.AIObject, index + delta);
        }

        private int ModelIndexOf(FieldView fieldView)
        {
            if (fieldView == null || fieldView.AIObject == null || _behavior == null)
                return -1;

            if (fieldView.AIObject is ActionSet set)
                return _behavior.ActionSets.IndexOf(set);
            if (fieldView.AIObject is ActionBase action)
            {
                ActionSet owner = _behavior.GetActionSet(action);
                return owner == null ? -1 : owner.Actions.IndexOf(action);
            }
            if (fieldView.AIObject is ConsiderationBase consideration)
            {
                ActionBase owner = _behavior.GetAction(consideration);
                return owner == null ? -1 : owner.Considerations.IndexOf(consideration);
            }
            return -1;
        }

        private int ModelCountOf(FieldView fieldView)
        {
            if (fieldView == null || _behavior == null)
                return 0;

            if (fieldView.AIObject is ActionSet)
                return _behavior.ActionSets.Count;
            if (fieldView.AIObject is ActionBase action)
            {
                ActionSet owner = _behavior.GetActionSet(action);
                return owner == null ? 0 : owner.Actions.Count;
            }
            if (fieldView.AIObject is ConsiderationBase consideration)
            {
                ActionBase owner = _behavior.GetAction(consideration);
                return owner == null ? 0 : owner.Considerations.Count;
            }
            return 0;
        }

        #endregion

        #region copy and paste

        private static int KindOf(AIObject aiObject)
        {
            if (aiObject is ActionSet)
                return (int)FieldView.ColumnKind.ActionSet;
            if (aiObject is ActionBase)
                return (int)FieldView.ColumnKind.Action;
            if (aiObject is ConsiderationBase)
                return (int)FieldView.ColumnKind.Consideration;
            return -1;
        }

        /// <summary>
        /// Remembers an object so that it can be pasted into this or another behavior. The object is
        /// stored as an asset path plus a guid, so a reorder or an edit of the source cannot make the
        /// clipboard point at the wrong item.
        /// </summary>
        public void CopyToClipboard(AIObject aiObject)
        {
            if (aiObject == null || _behavior == null)
                return;

            string path = AssetDatabase.GetAssetPath(aiObject);
            if (string.IsNullOrEmpty(path))
                path = AssetDatabase.GetAssetPath(_behavior);

            _clipboardAssetPath = path;
            _clipboardGuid = KeyOf(aiObject);
            _clipboardKind = KindOf(aiObject);
        }

        private static AIObject ResolveClipboard(FieldView.ColumnKind kind)
        {
            if (_clipboardKind != (int)kind)
                return null;
            if (string.IsNullOrEmpty(_clipboardAssetPath) || string.IsNullOrEmpty(_clipboardGuid))
                return null;

            var source = AssetDatabase.LoadAssetAtPath<AgentBehavior>(_clipboardAssetPath);
            if (source == null)
                return null;

            foreach (var set in source.ActionSets)
            {
                if (set == null)
                    continue;

                if (_clipboardKind == (int)FieldView.ColumnKind.ActionSet)
                {
                    if (KeyOf(set) == _clipboardGuid)
                        return set;
                    continue;
                }

                foreach (var action in set.Actions)
                {
                    if (action == null)
                        continue;

                    if (_clipboardKind == (int)FieldView.ColumnKind.Action)
                    {
                        if (KeyOf(action) == _clipboardGuid)
                            return action;
                        continue;
                    }

                    foreach (var consideration in action.Considerations)
                    {
                        if (consideration != null && KeyOf(consideration) == _clipboardGuid)
                            return consideration;
                    }
                }
            }

            // The source was changed or deleted since the Copy. Nothing is pasted, nothing throws.
            return null;
        }

        private bool CanPaste(FieldView.ColumnKind kind)
        {
            if (!CanEdit)
                return false;
            if (kind == FieldView.ColumnKind.Action && _currentActionSet == null)
                return false;
            if (kind == FieldView.ColumnKind.Consideration && _currentAction == null)
                return false;
            return ResolveClipboard(kind) != null;
        }

        private void PasteInto(FieldView.ColumnKind kind)
        {
            AIObject source = ResolveClipboard(kind);
            if (source != null)
                DuplicateObject(source);
        }

        private static string PasteLabel(FieldView.ColumnKind kind)
        {
            switch (kind)
            {
                case FieldView.ColumnKind.ActionSet:
                    return "Paste Action Set";
                case FieldView.ColumnKind.Action:
                    return "Paste Action";
                default:
                    return "Paste Consideration";
            }
        }

        #endregion

        #region context menus

        private void RegisterColumnMenu(VisualElement area, FieldView.ColumnKind kind)
        {
            if (area == null)
                return;

            // One manipulator per column. A right click on a row bubbles up to it, and the row adds its
            // own items first through its ContextualMenuPopulateEvent handler.
            area.AddManipulator(new ContextualMenuManipulator(evt => BuildColumnMenu(evt, kind)));
        }

        /// <summary>
        /// Fills the context menu of one row.
        /// </summary>
        internal void BuildRowMenu(DropdownMenu menu, FieldView fieldView)
        {
            if (fieldView == null || fieldView.AIObject == null)
                return;

            // Nothing on this menu can be done while the game runs, so no menu is shown at all.
            if (_playMode)
                return;

            bool canEdit = CanEdit;
            int index = ModelIndexOf(fieldView);
            int count = ModelCountOf(fieldView);

            menu.AppendAction("Rename", _ => fieldView.BeginRename(), StatusOf(canEdit));
            menu.AppendAction("Duplicate", _ => DuplicateObject(fieldView.AIObject), StatusOf(canEdit));
            menu.AppendAction("Copy", _ => CopyToClipboard(fieldView.AIObject), StatusOf(_behavior != null && !_playMode));
            menu.AppendAction(PasteLabel(fieldView.Kind), _ => PasteInto(fieldView.Kind), StatusOf(CanPaste(fieldView.Kind)));
            menu.AppendSeparator();
            menu.AppendAction("Move Up", _ => MoveRelative(fieldView, -1), StatusOf(canEdit && index > 0));
            menu.AppendAction("Move Down", _ => MoveRelative(fieldView, 1), StatusOf(canEdit && index >= 0 && index < count - 1));
            menu.AppendSeparator();
            menu.AppendAction("Delete", _ => DeleteField(fieldView), StatusOf(canEdit));
        }

        private void BuildColumnMenu(ContextualMenuPopulateEvent evt, FieldView.ColumnKind kind)
        {
            if (_playMode)
                return;

            // A right click on a row is the row's menu. The row stops the event before it gets here, so
            // this is only a safety net for the case where it did not.
            var target = evt.target as VisualElement;
            if (target != null && (target is FieldView || target.GetFirstAncestorOfType<FieldView>() != null))
                return;

            bool canEdit = CanEdit;
            switch (kind)
            {
                case FieldView.ColumnKind.ActionSet:
                    evt.menu.AppendAction("Add Action Set", _ => CreateActionSet(), StatusOf(canEdit));
                    break;
                case FieldView.ColumnKind.Action:
                    evt.menu.AppendAction("Add Action", _ => ShowActionList(), StatusOf(canEdit && _currentActionSet != null));
                    break;
                default:
                    evt.menu.AppendAction("Add Consideration", _ => ShowConsiderationList(), StatusOf(canEdit && _currentAction != null));
                    break;
            }

            evt.menu.AppendAction(PasteLabel(kind), _ => PasteInto(kind), StatusOf(CanPaste(kind)));
        }

        private static DropdownMenuAction.Status StatusOf(bool enabled)
        {
            return enabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }

        #endregion

        #region play mode

        private void OnSortToggleChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                SortActionsByScore();
            else
                RestoreAuthoredActionOrder();
        }

        private void OnFreezeToggleChanged(ChangeEvent<bool> evt)
        {
            if (!evt.newValue)
                UpdateScores(_lastActionData);
        }

        /// <summary>
        /// Applies one round of scores to the three lists.
        /// </summary>
        internal void UpdateScores(ActionData actionData)
        {
            if (!_playMode || _behavior == null)
                return;

            if (_freezeToggle != null && _freezeToggle.value)
                return;

            if (_brain == null)
            {
                ShowMessage(AgentDestroyedMessage);
                return;
            }

            _lastActionData = actionData;

            // The running action comes from the brain and not from the event payload, which is empty
            // whenever the brain decided to keep the action it already had.
            ActionBase running = _brain.currentAction;
            ActionSet runningSet = running != null ? _behavior.GetActionSet(running) : null;

            float highestActionScore = 0f;
            foreach (var view in _actionViews)
            {
                if (view.AIObject is ActionBase action)
                    highestActionScore = Mathf.Max(highestActionScore, action.Score);
            }

            float highestSetScore = 0f;
            foreach (var view in _actionSetViews)
                highestSetScore = Mathf.Max(highestSetScore, HighestScoreOf(view.AIObject as ActionSet));

            foreach (var view in _actionSetViews)
            {
                var set = view.AIObject as ActionSet;
                float score = HighestScoreOf(set);
                float fill = highestSetScore > 0f ? score / highestSetScore : 0f;
                view.SetDebugState(score, fill, set != null && set == runningSet);
            }

            foreach (var view in _actionViews)
            {
                var action = view.AIObject as ActionBase;
                float score = action != null ? action.Score : 0f;
                float fill = highestActionScore > 0f ? score / highestActionScore : 0f;
                view.SetDebugState(score, fill, action != null && action == running);
            }

            foreach (var view in _considerationViews)
            {
                var consideration = view.AIObject as ConsiderationBase;
                float score = consideration != null ? consideration.Score : 0f;
                view.SetDebugState(score, Mathf.Clamp01(score), false);
            }

            if (_actionSortToggle != null && _actionSortToggle.value)
                SortActionsByScore();

            UpdateDebugExtras(true);
            ScoresUpdated?.Invoke(actionData);
        }

        private static float HighestScoreOf(ActionSet set)
        {
            if (set == null)
                return 0f;

            float highest = 0f;
            foreach (var action in set.Actions)
            {
                if (action != null)
                    highest = Mathf.Max(highest, action.Score);
            }
            return highest;
        }

        /// <summary>
        /// Puts the action rows in score order without touching the order stored in the asset.
        /// </summary>
        internal void SortActionsByScore()
        {
            List<FieldView> sorted = _actionViews
                .Where(v => v.AIObject is ActionBase)
                .OrderByDescending(v => ((ActionBase)v.AIObject).Score)
                .ThenBy(v => ((ActionBase)v.AIObject).Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            ApplyRowOrder(sorted);
        }

        /// <summary>
        /// Puts the action rows back in the order they have in the asset.
        /// </summary>
        internal void RestoreAuthoredActionOrder()
        {
            if (_currentActionSet == null)
                return;

            var ordered = new List<FieldView>();
            foreach (var action in _currentActionSet.Actions)
            {
                FieldView row = _actionViews.FirstOrDefault(v => v.AIObject == (AIObject)action);
                if (row != null)
                    ordered.Add(row);
            }
            ApplyRowOrder(ordered);
        }

        private static void ApplyRowOrder(List<FieldView> order)
        {
            for (int i = 0; i < order.Count; i++)
            {
                if (i == 0)
                    order[i].SendToBack();
                else
                    order[i].PlaceInFront(order[i - 1]);
            }
        }

        /// <summary>
        /// Kept for compatibility. Sorts the action rows by score.
        /// </summary>
        internal void ReorderActions()
        {
            SortActionsByScore();
        }

        #endregion

        private static string KeyOf(AIObject aiObject)
        {
            if (aiObject == null)
                return null;
            if (!string.IsNullOrEmpty(aiObject.guid))
                return aiObject.guid;
#if UNITY_6000_4_OR_NEWER
            return EntityId.ToULong(aiObject.GetEntityId()).ToString(CultureInfo.InvariantCulture);
#else
            return aiObject.GetInstanceID().ToString(CultureInfo.InvariantCulture);
#endif
        }
    }
}
