using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// One row of one of the three lists of the Wise Feline window.
    /// The row element itself is the clickable, focusable and draggable element. There is no button
    /// inside it, so the selection color covers the whole row and the keyboard reaches it.
    /// </summary>
    [UxmlElement]
    public partial class FieldView : VisualElement
    {
        /// <summary>
        /// Which of the three columns a row belongs to.
        /// The numbers are also the kind stored by Copy, so do not renumber them.
        /// </summary>
        public enum ColumnKind
        {
            /// <summary>The first column, which lists the action sets of the behavior.</summary>
            ActionSet = 0,
            /// <summary>The second column, which lists the actions of the selected action set.</summary>
            Action = 1,
            /// <summary>The third column, which lists the considerations of the selected action.</summary>
            Consideration = 2
        }

        /// <summary>
        /// The action set, action or consideration this row shows.
        /// </summary>
        public AIObject AIObject { get; private set; }

        /// <summary>
        /// The concrete type of <see cref="AIObject"/>.
        /// </summary>
        public Type ObjectType { get; private set; }

        /// <summary>
        /// The view that owns this row.
        /// </summary>
        public BehaviorView BehaviorView { get; private set; }

        /// <summary>
        /// The column this row belongs to.
        /// </summary>
        public ColumnKind Kind { get; private set; }

        /// <summary>
        /// Is the row being renamed in place at the moment.
        /// </summary>
        public bool IsRenaming { get { return _renaming; } }

        private VisualElement _bar;
        private Label _nameLabel;
        private Label _tagLabel;
        private Label _scoreLabel;
        private TextField _renameField;

        private SerializedObject _serializedObject;
        private bool _renaming;
        private string _nameBeforeRename;

        public FieldView()
        {
            focusable = true;
        }

        /// <summary>
        /// Fills the row with an object. Called once per row, right after it is cloned from FieldView.uxml.
        /// </summary>
        /// <param name="aiObject">The object the row shows.</param>
        /// <param name="behaviorView">The view that owns the row.</param>
        /// <param name="kind">The column the row belongs to.</param>
        /// <param name="interactive">False for a read only or a play mode row, which cannot be dragged.</param>
        public void Init(AIObject aiObject, BehaviorView behaviorView, ColumnKind kind, bool interactive)
        {
            if (aiObject == null)
                return;

            AIObject = aiObject;
            ObjectType = aiObject.GetType();
            BehaviorView = behaviorView;
            Kind = kind;

            _bar = this.Q<VisualElement>("score-bar");
            _nameLabel = this.Q<Label>("label-name");
            _tagLabel = this.Q<Label>("label-tag");
            _scoreLabel = this.Q<Label>("label-score");
            _renameField = this.Q<TextField>("field-name");

            AddToClassList(ClassForKind(kind));

            _serializedObject = new SerializedObject(aiObject);
            SerializedProperty nameProperty = _serializedObject.FindProperty("Name");
            if (nameProperty != null && _nameLabel != null)
                _nameLabel.BindProperty(nameProperty);

            RefreshName();
            RefreshTag();
            SetDebugState(0f, 0f, false);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            // The manipulator has to sit on the row itself: it sends the populate event to its own
            // target, so a manipulator on the column would never let the row fill the menu. It also
            // stops the trigger event, which is what keeps the column menu from opening on top of this
            // one when the click was on a row.
            this.AddManipulator(new ContextualMenuManipulator(OnContextualMenuPopulate));

            if (_renameField != null)
            {
                _renameField.RegisterCallback<KeyDownEvent>(OnRenameKeyDown, TrickleDown.TrickleDown);
                _renameField.RegisterCallback<FocusOutEvent>(OnRenameFocusOut);
            }

            if (interactive)
                this.AddManipulator(new DragAndDropManipulator(this));
        }

        private static string ClassForKind(ColumnKind kind)
        {
            switch (kind)
            {
                case ColumnKind.ActionSet:
                    return "row--actionset";
                case ColumnKind.Action:
                    return "row--action";
                default:
                    return "row--consideration";
            }
        }

        /// <summary>
        /// Re-reads the name from the object. The label itself is bound to the serialized Name field,
        /// this keeps the tooltip that shows the full name of an elided row in sync with it.
        /// </summary>
        public void RefreshName()
        {
            if (AIObject == null)
                return;

            if (_nameLabel != null)
                _nameLabel.text = AIObject.Name;
            tooltip = AIObject.Name;
        }

        /// <summary>
        /// Rebuilds the small tag between the name and the score: the priority and the weight of an
        /// action when they are not the defaults, the chosen target while the game runs, and a "T" on a
        /// consideration that scores the targets of its action instead of the agent.
        /// </summary>
        public void RefreshTag()
        {
            if (_tagLabel == null || AIObject == null)
                return;

            string text = string.Empty;
            string tip = string.Empty;

            if (AIObject is ActionBase action)
            {
                var parts = new List<string>();
                var described = new List<string>();

                if (action._priority != 0)
                {
                    parts.Add("P" + action._priority.ToString(CultureInfo.InvariantCulture));
                    described.Add("priority " + action._priority.ToString(CultureInfo.InvariantCulture));
                }

                if (Mathf.Abs(action.Weight - 1f) > 0.0001f)
                {
                    string weight = action.Weight.ToString("0.##", CultureInfo.InvariantCulture);
                    parts.Add("W" + weight);
                    described.Add("weight " + weight);
                }

                if (BehaviorView != null && BehaviorView.PlayMode && action.ChosenTarget != null)
                {
                    parts.Add("(" + action.ChosenTarget.name + ")");
                    described.Add("chosen target " + action.ChosenTarget.name);
                }

                text = string.Join(" ", parts);
                tip = described.Count > 0 ? string.Join(", ", described) : string.Empty;
            }
            else if (AIObject is ConsiderationBase consideration && consideration.IsTargeted)
            {
                text = "T";
                tip = "This consideration scores the targets of the action instead of the agent itself.";
            }

            _tagLabel.text = text;
            _tagLabel.tooltip = tip;
            _tagLabel.EnableInClassList("is-hidden", string.IsNullOrEmpty(text));
        }

        /// <summary>
        /// Applies one score update to the row. The score label and the bar behind it are only visible
        /// in play mode, which is handled by the play-mode class on the view and not from here.
        /// </summary>
        /// <param name="score">The score to print.</param>
        /// <param name="fill">How much of the row the bar covers, 0 to 1.</param>
        /// <param name="running">Is this the running action, or the set that contains it.</param>
        internal void SetDebugState(float score, float fill, bool running)
        {
            if (_scoreLabel != null)
                _scoreLabel.text = score.ToString("0.000", CultureInfo.InvariantCulture);

            // The width of the bar is 100% in USS and the transform scales it, so that the only style
            // value the code writes stays the drag translation.
            if (_bar != null)
                _bar.transform.scale = new Vector3(Mathf.Clamp01(fill), 1f, 1f);

            EnableInClassList("row--running", running);
            RefreshTag();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_renaming || BehaviorView == null)
                return;

            // The right button selects the row as well, so that the context menu always acts on the row
            // the user can see is selected. It never starts a rename and never starts a drag.
            if (evt.button != 0 && evt.button != 1)
                return;

            Focus();
            BehaviorView.RowClicked(this);

            if (evt.button == 0 && evt.clickCount == 2 && BehaviorView.CanEdit)
                BeginRename();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (_renaming || BehaviorView == null)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.F2:
                    if (BehaviorView.CanEdit)
                        BeginRename();
                    evt.StopPropagation();
                    break;
                case KeyCode.Delete:
                    if (BehaviorView.CanEdit)
                        BehaviorView.DeleteField(this);
                    evt.StopPropagation();
                    break;
                case KeyCode.UpArrow:
                    BehaviorView.MoveSelection(this, -1);
                    evt.StopPropagation();
                    break;
                case KeyCode.DownArrow:
                    BehaviorView.MoveSelection(this, 1);
                    evt.StopPropagation();
                    break;
                case KeyCode.D:
                    if (evt.actionKey && BehaviorView.CanEdit)
                    {
                        BehaviorView.DuplicateObject(AIObject);
                        evt.StopPropagation();
                    }
                    break;
            }
        }

        private void OnContextualMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            if (BehaviorView != null)
                BehaviorView.BuildRowMenu(evt.menu, this);
        }

        /// <summary>
        /// Starts renaming the row in place. Enter commits and Escape cancels.
        /// </summary>
        public void BeginRename()
        {
            if (_renaming || _renameField == null || AIObject == null)
                return;

            _renaming = true;
            _nameBeforeRename = AIObject.Name;
            _renameField.SetValueWithoutNotify(AIObject.Name);
            AddToClassList("row--renaming");

            // The field is hidden by USS until the class above is applied, so focusing it has to wait
            // for the next layout pass.
            _renameField.schedule.Execute(() =>
            {
                _renameField.Focus();
                _renameField.SelectAll();
            }).ExecuteLater(0);
        }

        private void OnRenameKeyDown(KeyDownEvent evt)
        {
            if (!_renaming)
                return;

            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                CommitRename();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                CancelRename();
                evt.StopPropagation();
            }
        }

        private void OnRenameFocusOut(FocusOutEvent evt)
        {
            CommitRename();
        }

        /// <summary>
        /// Commits an inline rename if one is running. Does nothing otherwise.
        /// </summary>
        public void FinishRename()
        {
            CommitRename();
        }

        private void CommitRename()
        {
            if (!_renaming)
                return;

            _renaming = false;
            RemoveFromClassList("row--renaming");

            string newName = _renameField.value == null ? string.Empty : _renameField.value.Trim();
            if (!string.IsNullOrEmpty(newName) && newName != _nameBeforeRename && BehaviorView != null)
                BehaviorView.RenameObject(AIObject, newName);

            RefreshName();
            Focus();
        }

        private void CancelRename()
        {
            if (!_renaming)
                return;

            _renaming = false;
            RemoveFromClassList("row--renaming");
            _renameField.SetValueWithoutNotify(_nameBeforeRename);
            Focus();
        }
    }
}
