using UnityEngine;
using UnityEngine.UIElements;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// Reorders a row inside its own column by dragging it. The row is inserted where the drop line
    /// shows, it is not swapped with the row under the pointer, and the whole move is one Undo step.
    /// Every handler and the pointer capture live on the row itself, which is also the manipulator's
    /// target, so captured pointer events always reach the code that started the drag.
    /// </summary>
    public class DragAndDropManipulator : PointerManipulator
    {
        // How far the pointer has to move before a press turns into a drag. Without it a plain click
        // that moves by a pixel would reorder the list.
        private const float DragThreshold = 4f;
        private const float AutoScrollMargin = 18f;
        private const float AutoScrollStep = 6f;

        private readonly FieldView _row;

        private ScrollView _scrollView;
        private VisualElement _content;
        private VisualElement _dropLine;
        private IVisualElementScheduledItem _autoScroll;

        private bool _pressed;
        private bool _dragging;
        private int _pointerId = -1;
        private int _dropIndex;
        private float _grabOffset;
        private Vector2 _pressStart;
        private Vector2 _pointerWorld;

        public DragAndDropManipulator(FieldView row)
        {
            _row = row;
            target = row;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            // Only the left button drags. A right button press has to reach the context menu untouched.
            if (evt.button != 0 || _row.IsRenaming)
                return;

            if (_row.BehaviorView == null || !_row.BehaviorView.CanEdit)
                return;

            _scrollView = FindScrollView();
            _content = _scrollView != null ? _scrollView.contentContainer : _row.parent;
            if (_content == null)
                return;

            _pressed = true;
            _pointerId = evt.pointerId;
            _pressStart = evt.position;
            _pointerWorld = evt.position;
            target.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_pressed || evt.pointerId != _pointerId || !target.HasPointerCapture(evt.pointerId))
                return;

            _pointerWorld = evt.position;

            if (!_dragging)
            {
                if (Mathf.Abs(_pointerWorld.y - _pressStart.y) < DragThreshold)
                    return;
                BeginDrag();
            }

            UpdateDrag();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId)
                return;

            bool dropped = _dragging;
            int index = _dropIndex;

            EndDrag();

            if (target.HasPointerCapture(evt.pointerId))
                target.ReleasePointer(evt.pointerId);

            _pressed = false;
            _pointerId = -1;

            // This rebuilds the column, so nothing may touch this row afterwards.
            if (dropped && _row.BehaviorView != null)
                _row.BehaviorView.MoveObject(_row.AIObject, index);
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            // The capture can be lost without a pointer up, for instance when the panel changes.
            // That cancels the drag instead of dropping the row somewhere the user did not aim at.
            EndDrag();
            _pressed = false;
            _pointerId = -1;
        }

        private void BeginDrag()
        {
            _dragging = true;

            float rowY = _row.layout.y;
            Vector2 local = _content.WorldToLocal(_pointerWorld);
            _grabOffset = local.y - rowY;
            _dropIndex = 0;

            _row.AddToClassList("row--dragging");

            _dropLine = new VisualElement { name = "drop-line" };
            _dropLine.AddToClassList("drop-line");
            _dropLine.pickingMode = PickingMode.Ignore;
            _content.Add(_dropLine);

            if (_scrollView != null)
                _autoScroll = _row.schedule.Execute(AutoScroll).Every(16);
        }

        private void UpdateDrag()
        {
            if (!_dragging)
                return;

            Vector2 local = _content.WorldToLocal(_pointerWorld);
            _row.transform.position = new Vector3(0f, local.y - _grabOffset, 0f);

            _dropIndex = ComputeDropIndex(local.y);
            PositionDropLine(_dropIndex);
        }

        private int ComputeDropIndex(float localY)
        {
            int index = 0;
            foreach (VisualElement child in _content.Children())
            {
                if (child == _row || !(child is FieldView))
                    continue;
                if (localY > child.layout.center.y)
                    index++;
                else
                    break;
            }
            return index;
        }

        private void PositionDropLine(int index)
        {
            if (_dropLine == null)
                return;

            float y = 0f;
            int seen = 0;
            VisualElement last = null;

            foreach (VisualElement child in _content.Children())
            {
                if (child == _row || !(child is FieldView))
                    continue;

                if (seen == index)
                {
                    y = child.layout.yMin;
                    _dropLine.transform.position = new Vector3(0f, y - 1f, 0f);
                    return;
                }

                last = child;
                seen++;
            }

            if (last != null)
                y = last.layout.yMax;
            _dropLine.transform.position = new Vector3(0f, y - 1f, 0f);
        }

        private void AutoScroll()
        {
            if (!_dragging || _scrollView == null)
                return;

            Rect viewport = _scrollView.contentViewport.worldBound;
            float delta = 0f;
            if (_pointerWorld.y < viewport.yMin + AutoScrollMargin)
                delta = -AutoScrollStep;
            else if (_pointerWorld.y > viewport.yMax - AutoScrollMargin)
                delta = AutoScrollStep;

            if (Mathf.Approximately(delta, 0f))
                return;

            float maxOffset = Mathf.Max(0f, _content.layout.height - viewport.height);
            Vector2 offset = _scrollView.scrollOffset;
            float wanted = Mathf.Clamp(offset.y + delta, 0f, maxOffset);
            if (Mathf.Approximately(wanted, offset.y))
                return;

            offset.y = wanted;
            _scrollView.scrollOffset = offset;
            UpdateDrag();
        }

        private void EndDrag()
        {
            if (_autoScroll != null)
            {
                _autoScroll.Pause();
                _autoScroll = null;
            }

            if (_dropLine != null)
            {
                _dropLine.RemoveFromHierarchy();
                _dropLine = null;
            }

            if (_dragging)
            {
                _row.RemoveFromClassList("row--dragging");
                _row.transform.position = Vector3.zero;
                _dragging = false;
            }
        }

        private ScrollView FindScrollView()
        {
            VisualElement current = _row.parent;
            while (current != null)
            {
                if (current is ScrollView view)
                    return view;
                current = current.parent;
            }
            return null;
        }
    }
}
