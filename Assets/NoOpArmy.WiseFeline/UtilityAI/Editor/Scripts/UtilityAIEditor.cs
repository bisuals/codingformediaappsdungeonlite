using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// The Wise Feline window. In edit mode it edits the selected Agent Behavior asset.
    /// In play mode it shows the live scores of the Brain component on the selected GameObject.
    /// </summary>
    public class UtilityAIEditor : EditorWindow
    {
        // The UI files are looked up by GUID so that moving the package folder does not break the
        // window. A hardcoded path would also come back after a domain reload, when Unity recreates
        // the window from the saved layout by calling CreateGUI directly.
        private const string WindowUxmlGuid = "b1b1947d8f002f44fb8465d79829fd23";
        private const string WindowUssGuid = "c1a025f767b68b74eae345bfaa096fcc";
        private const string RowUxmlGuid = "425f6677aea79014a8fc78d3b62798ae";
        private const string RowUssGuid = "44f0bae86d1b4ad4aac2e346a3aadaf8";
        private const string IconGuid = "d1448a1cca2460f4e9d7eb9f009523fc";

        internal const string EditModeHint = "Select an Agent Behavior asset, or a GameObject with a Brain component, to edit it.";
        internal const string PlayModeHint = "Select a GameObject with a Brain component in the scene to debug it.";

        private static string _assetPath;

        /// <summary>
        /// The folder that holds the UXML and USS files of the window, with a trailing slash.
        /// It is resolved from the asset database instead of being hardcoded, and it is re-resolved
        /// whenever the cached value no longer points at a real file, so a moved package folder and a
        /// domain reload are both fine. The About window reads it too.
        /// </summary>
        public static string AssetPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_assetPath) &&
                    AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_assetPath + "UtilityAIEditor.uxml") != null)
                {
                    return _assetPath;
                }

                _assetPath = ResolveAssetPath();
                return _assetPath;
            }
        }

        private BehaviorView _behaviorView;
        private Brain _pendingBrain;

        [MenuItem("Window/NoOpArmy/Wise Feline")]
        public static void OpenWindow()
        {
            UtilityAIEditor wnd = GetWindow<UtilityAIEditor>();
            wnd.ApplyWindowChrome();
            wnd.Show();
        }

        private void ApplyWindowChrome()
        {
            autoRepaintOnSceneChange = true;
            titleContent = new GUIContent("Wise Feline", LoadIcon());
            // The minimum follows from the minimum widths of the four columns in UtilityAIEditor.uss.
            minSize = new Vector2(620, 300);
        }

        [OnOpenAsset]
#if UNITY_6000_4_OR_NEWER
        public static bool OnOpenAsset(EntityId instancId, int line)
#else
        public static bool OnOpenAsset(int instancId, int line)
#endif
        {
            if (Selection.activeObject is AgentBehavior)
            {
                OpenWindow();
                return true;
            }
            return false;
        }

        public void CreateGUI()
        {
            // Also done here and not only in OpenWindow, because Unity recreates a window that was in
            // the saved layout by calling CreateGUI directly.
            ApplyWindowChrome();

            var root = rootVisualElement;

            var visualTree = LoadUxml(WindowUxmlGuid, "UtilityAIEditor.uxml");
            if (visualTree == null)
            {
                root.Add(new Label("The Wise Feline window layout file could not be found. Reimport the package."));
                return;
            }
            visualTree.CloneTree(root);

            AddStyleSheet(root, LoadUss(WindowUssGuid, "UtilityAIEditor.uss"));
            AddStyleSheet(root, LoadUss(RowUssGuid, "FieldView.uss"));

            _behaviorView = root.Q<BehaviorView>();
            if (_behaviorView == null)
                return;
            _behaviorView.Init(this);

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;

            OnSelectionChange();
        }

        private static void AddStyleSheet(VisualElement root, StyleSheet sheet)
        {
            // The UXML links both sheets as well. Adding them again would apply them twice, so this
            // only adds what is missing, which is what happens when the UXML link cannot be resolved.
            if (sheet != null && !root.styleSheets.Contains(sheet))
                root.styleSheets.Add(sheet);
        }

        private void OnPlayModeChanged(PlayModeStateChange playMode)
        {
            if (_behaviorView == null)
                return;

            switch (playMode)
            {
                case PlayModeStateChange.EnteredEditMode:
                    _behaviorView.PlayMode = false;
                    _behaviorView.ClearView();
                    OnSelectionChange();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    _behaviorView.ClearView();
                    OnSelectionChange();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    // Application.isPlaying is still true here, so asking for the selection again would
                    // subscribe to a brain that is about to be destroyed. EnteredEditMode refreshes.
                    _behaviorView.PlayMode = false;
                    _behaviorView.ClearView();
                    break;
            }
        }

        private void OnSelectionChange()
        {
            if (_behaviorView == null)
                return;

            StopWaitingForBrain();

            if (Application.isPlaying)
                ShowPlayModeSelection();
            else
                ShowEditModeSelection();
        }

        private void ShowPlayModeSelection()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
            {
                _behaviorView.ShowMessage(PlayModeHint);
                return;
            }

            if (EditorUtility.IsPersistent(go))
            {
                _behaviorView.ShowMessage($"{go.name} is a prefab asset. Select the agent in the scene to debug it.");
                return;
            }

            if (!go.TryGetComponent(out Brain brain))
            {
                _behaviorView.ShowMessage(PlayModeHint);
                return;
            }

            if (!brain.IsInitialized)
            {
                // The brain initializes in Start, so an agent that was just spawned, or one whose
                // GameObject is inactive, has nothing to show yet. Waiting for it costs one event.
                WaitForBrain(brain);
                _behaviorView.ShowMessage($"The Brain on {go.name} is not initialized yet. A Brain initializes in Start, so an inactive GameObject never gets there.");
                return;
            }

            AgentBehavior behavior = brain.Behavior;
            if (behavior == null)
            {
                _behaviorView.ShowMessage($"The Brain on {go.name} has no behavior to show.");
                return;
            }

            _behaviorView.PlayMode = true;
            _behaviorView.ShowBehavior(behavior, brain, true);
        }

        private void ShowEditModeSelection()
        {
            AgentBehavior behavior = Selection.activeObject as AgentBehavior;
            if (behavior == null && Selection.activeGameObject != null)
            {
                if (Selection.activeGameObject.TryGetComponent(out Brain brain))
                    behavior = brain.Behavior;
            }

            if (behavior == null)
            {
                _behaviorView.ShowMessage(EditModeHint);
                return;
            }

            // A behavior that lives only in memory, or one that version control keeps locked, is shown
            // read only instead of being edited by mistake.
            bool readOnly = !EditorUtility.IsPersistent(behavior) ||
                            !AssetDatabase.CanOpenForEdit(behavior, StatusQueryOptions.UseCachedIfPossible);

            _behaviorView.PlayMode = false;
            _behaviorView.ShowBehavior(behavior, null, readOnly);
        }

        private void WaitForBrain(Brain brain)
        {
            StopWaitingForBrain();
            _pendingBrain = brain;
            brain.OnInitialized += OnPendingBrainInitialized;
        }

        private void StopWaitingForBrain()
        {
            if (_pendingBrain != null)
                _pendingBrain.OnInitialized -= OnPendingBrainInitialized;
            _pendingBrain = null;
        }

        private void OnPendingBrainInitialized()
        {
            Brain brain = _pendingBrain;
            StopWaitingForBrain();
            if (brain != null && Selection.activeGameObject == brain.gameObject)
                OnSelectionChange();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            StopWaitingForBrain();
        }

        /// <summary>
        /// Loads the template used for one row of the three lists.
        /// </summary>
        internal static VisualTreeAsset LoadRowTemplate()
        {
            return LoadUxml(RowUxmlGuid, "FieldView.uxml");
        }

        internal static VisualTreeAsset LoadUxml(string guid, string fileName)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            VisualTreeAsset asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            if (asset == null)
                asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetPath + fileName);
            return asset;
        }

        internal static StyleSheet LoadUss(string guid, string fileName)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StyleSheet sheet = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet == null)
                sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetPath + fileName);
            return sheet;
        }

        internal static Texture2D LoadIcon()
        {
            Texture2D icon = Resources.Load<Texture2D>("wficon");
            if (icon == null)
            {
                string path = AssetDatabase.GUIDToAssetPath(IconGuid);
                if (!string.IsNullOrEmpty(path))
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            return icon;
        }

        private static string ResolveAssetPath()
        {
            string path = AssetDatabase.GUIDToAssetPath(WindowUxmlGuid);
            if (string.IsNullOrEmpty(path))
            {
                string[] uxmlGuids = AssetDatabase.FindAssets("UtilityAIEditor t:VisualTreeAsset");
                if (uxmlGuids.Length > 0)
                    path = AssetDatabase.GUIDToAssetPath(uxmlGuids[0]);
            }
            if (!string.IsNullOrEmpty(path))
                return FolderOf(path);

            string[] scriptGuids = AssetDatabase.FindAssets("UtilityAIEditor t:script");
            if (scriptGuids.Length > 0)
            {
                // .../Editor/Scripts/UtilityAIEditor.cs, so two steps up is the Editor folder.
                string editorFolder = ParentOf(ParentOf(AssetDatabase.GUIDToAssetPath(scriptGuids[0])));
                if (!string.IsNullOrEmpty(editorFolder))
                    return editorFolder + "/UI/";
            }

            return "Assets/NoOpArmy.WiseFeline/UtilityAI/Editor/UI/";
        }

        private static string FolderOf(string assetPath)
        {
            int slash = assetPath.LastIndexOf('/');
            return slash < 0 ? string.Empty : assetPath.Substring(0, slash + 1);
        }

        private static string ParentOf(string assetPath)
        {
            int slash = assetPath.LastIndexOf('/');
            return slash < 0 ? string.Empty : assetPath.Substring(0, slash);
        }
    }
}
