using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// The Properties panel of the Wise Feline window. It draws the selected action set, action or
    /// consideration with an InspectorElement, which owns the Editor instance and destroys it when the
    /// element leaves the panel, so nothing leaks when the window closes.
    /// </summary>
    [UxmlElement]
    public partial class InspectorView : VisualElement
    {
        private readonly VisualElement _content;
        private readonly VisualElement _debugExtras;
        private UnityEngine.Object _target;
        private SerializedObject _serializedObject;

        /// <summary>
        /// Extension point for the debugger. This container is called "debug-extras", sits below the
        /// inspector and is only visible in play mode while a consideration is selected. Add debug only
        /// widgets (a response curve marker, a score sparkline) to it; it is cleared on every selection
        /// change and it stays enabled while the rest of the panel is disabled in play mode.
        /// </summary>
        public VisualElement DebugExtras
        {
            get { return _debugExtras; }
        }

        /// <summary>
        /// The object the panel currently shows, or null.
        /// </summary>
        public UnityEngine.Object Target
        {
            get { return _target; }
        }

        /// <summary>
        /// Raised when any serialized value of the shown object changes, no matter whether the change
        /// came from this panel, from an inline rename or from an Undo. The window uses it to keep the
        /// object name and the row tags in sync with the fields.
        /// </summary>
        public event Action<UnityEngine.Object> ObjectChanged;

        public InspectorView()
        {
            _content = new VisualElement { name = "inspector-content" };
            _content.AddToClassList("inspector-content");
            Add(_content);

            _debugExtras = new VisualElement { name = "debug-extras" };
            _debugExtras.AddToClassList("debug-extras");
            _debugExtras.AddToClassList("is-hidden");
            Add(_debugExtras);
        }

        /// <summary>
        /// Shows an object in the panel. Pass null to empty it.
        /// </summary>
        /// <param name="uObject">The object to show.</param>
        /// <param name="showDebugExtras">Should the debug-extras container below the inspector be visible.</param>
        public void UpdateSelection(UnityEngine.Object uObject, bool showDebugExtras = false)
        {
            _content.Clear();
            _debugExtras.Clear();
            _target = uObject;
            _serializedObject = null;

            if (uObject == null)
            {
                _debugExtras.EnableInClassList("is-hidden", true);
                return;
            }

            _serializedObject = new SerializedObject(uObject);
            var inspector = new InspectorElement(_serializedObject);
            _content.Add(inspector);

            // The tracker lives on the InspectorElement, so it goes away with it and does not pile up
            // one more tracker per selection change.
            SerializedObject tracked = _serializedObject;
            inspector.TrackSerializedObjectValue(tracked, _ => RaiseObjectChanged());

            _debugExtras.EnableInClassList("is-hidden", !showDebugExtras);
        }

        /// <summary>
        /// Enables or disables the inspector itself without touching the debug-extras container, which
        /// has to stay readable while the fields are disabled in play mode.
        /// </summary>
        public void SetContentEnabled(bool enabled)
        {
            _content.SetEnabled(enabled);
        }

        private void RaiseObjectChanged()
        {
            if (_target != null)
                ObjectChanged?.Invoke(_target);
        }
    }
}
