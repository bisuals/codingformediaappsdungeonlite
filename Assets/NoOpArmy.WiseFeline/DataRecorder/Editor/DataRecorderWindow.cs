using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using NoOpArmy.WiseFeline.DataRecorder;
using System;
using System.Linq;
using UnityEngine.UI;


public class DataRecorderWindow : EditorWindow
{
    private ListView objectsList;
    private ListView framesList;
    private TreeView detailsTreeView;

    [MenuItem("Window/NoOpArmy/Data Recorder")]
    public static void ShowExample()
    {
        DataRecorderWindow wnd = GetWindow<DataRecorderWindow>();
        wnd.autoRepaintOnSceneChange = true;
        wnd.titleContent = new GUIContent("Data Recorder");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/NoOpArmy.WiseFeline/DataRecorder/Editor/DataRecorderWindow.uxml");
        VisualElement controls = visualTree.Instantiate();
        root.Add(controls);

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/NoOpArmy.WiseFeline/DataRecorder/Editor/DataRecorderWindow.uss");
        //root.styleSheets.Add(styleSheet);
        objectsList = root.Q<ListView>("Objects");
        framesList = root.Q<ListView>("Frames");
        detailsTreeView = root.Q<TreeView>("Details");
        var recordToggle = root.Q<ToolbarToggle>();
        if (Application.isPlaying)
        {
            RecordManager.Instance.ShouldRecord = recordToggle.value;
        }
        recordToggle.RegisterValueChangedCallback(evt =>
        {
            if (Application.isPlaying)
                RecordManager.Instance.ShouldRecord = evt.newValue;
        });
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

        SetItemSources(objectsList, framesList, detailsTreeView);

        objectsList.makeItem = () => new Label();
        objectsList.bindItem = (VisualElement element, int index) => ((Label)element).text = RecordManager.Instance.dataRecorders[index].gameObject.name;
        objectsList.selectionChanged += OnObjectSelectionChanged;

        framesList.makeItem = () => new Label();
        framesList.bindItem = (VisualElement element, int index) => ((Label)element).text = RecordManager.Instance.currentRecorder.records[index].frameCount.ToString();
        framesList.selectionChanged += OnFrameSelectionChanged;

        detailsTreeView.makeItem = () => new Label();

        detailsTreeView.bindItem = (VisualElement element, int index) =>
            (element as Label).text = detailsTreeView.GetItemDataForIndex<string>(index);

    }

    private void OnObjectSelectionChanged(IEnumerable<object> enumerable)
    {
        if (objectsList.selectedIndex >= 0)
        {
            RecordManager.Instance.SetCurrentRecorder(objectsList.selectedIndex);
        }
        SetItemSources(objectsList, framesList, detailsTreeView);
        objectsList.Rebuild();
        framesList.Rebuild();
        detailsTreeView.Rebuild();
    }

    private void OnFrameSelectionChanged(IEnumerable<object> enumerable)
    {
        if (framesList.selectedIndex >= 0)
        {
            RecordManager.Instance.SetCurrentRecord(framesList.selectedIndex);
        }
        SetItemSources(objectsList, framesList, detailsTreeView);
        objectsList.Rebuild();
        framesList.Rebuild();
        detailsTreeView.Rebuild();
    }

    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        SetItemSources(objectsList, framesList, detailsTreeView);
        lastUpdate = 0;
        EditorApplication.update -= OnUpdate;
        EditorApplication.update += OnUpdate;
    }

    private void OnEnable()
    {
        Selection.selectionChanged += OnSceneSelectionChanged;
        EditorApplication.update += OnUpdate;
    }

    private float lastUpdate;
    private void OnUpdate()
    {
        if (objectsList == null)
            return;
        ResetPanesIfRecorderIsGone();
        var delta = Time.realtimeSinceStartup - lastUpdate;
        if (delta > 1 || delta < 0)
        {
            objectsList.Rebuild();
            framesList.Rebuild();
            detailsTreeView.Rebuild();
            lastUpdate = Time.realtimeSinceStartup;
        }
    }

    /// <summary>
    /// The recorder whose frames the window shows, so the panes can be reset when it is destroyed
    /// </summary>
    private DataRecorder shownRecorder;

    /// <summary>
    /// If the recorder shown in the frames and details panes was destroyed, clears both panes and the selections
    /// so they do not keep showing its old data or point at another recorder's row.
    /// </summary>
    private void ResetPanesIfRecorderIsGone()
    {
        if (ReferenceEquals(shownRecorder, null))
            return;
        var manager = EditorApplication.isPlaying ? RecordManager.Instance : null;
        if (shownRecorder != null && manager != null && manager.currentRecorder == shownRecorder)
        {
            // Another recorder may have been destroyed and shifted the rows, so keep the selection on the shown one.
            int index = manager.dataRecorders.IndexOf(shownRecorder);
            if (index >= 0 && index != objectsList.selectedIndex)
                objectsList.SetSelectionWithoutNotify(new[] { index });
            return;
        }

        shownRecorder = null;
        objectsList.SetSelectionWithoutNotify(Array.Empty<int>());
        framesList.SetSelectionWithoutNotify(Array.Empty<int>());
        SetItemSources(objectsList, framesList, detailsTreeView);
        objectsList.Rebuild();
        framesList.Rebuild();
        detailsTreeView.Rebuild();
    }

    private void OnSceneSelectionChanged()
    {
        if (!Application.isPlaying)
            return;
        if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<DataRecorder>() && Selection.activeGameObject != RecordManager.Instance?.currentRecorder?.gameObject)
        {
            var index = RecordManager.Instance.SetCurrentRecorder(Selection.activeGameObject.GetComponent<DataRecorder>());
            if (index >= 0)
            {
                objectsList.selectedIndex = index;
                SetItemSources(objectsList, framesList, detailsTreeView);
                objectsList.Rebuild();
                framesList.Rebuild();
                detailsTreeView.Rebuild();
            }
        }
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Selection.selectionChanged -= OnSceneSelectionChanged;
        EditorApplication.update -= OnUpdate;
    }

    private void SetItemSources(ListView objectsList, ListView framesList, TreeView detailsTreeView)
    {
        if (EditorApplication.isPlaying && RecordManager.Instance != null)
        {
            objectsList.itemsSource = RecordManager.Instance.dataRecorders;
        }
        else
        {
            objectsList.itemsSource = new List<DataRecorder>();
        }

        if (EditorApplication.isPlaying && RecordManager.Instance != null && RecordManager.Instance.currentRecorder != null)
        {
            framesList.itemsSource = RecordManager.Instance.currentRecorder.records;
            shownRecorder = RecordManager.Instance.currentRecorder;
        }
        else
        {
            framesList.itemsSource = new List<Record>();
            shownRecorder = null;
        }

        if (EditorApplication.isPlaying && RecordManager.Instance != null && RecordManager.Instance.currentRecord != null)
        {
            List<TreeViewItemData<string>> roots = RecordManager.Instance.GetRoots(RecordManager.Instance.currentRecord);
            detailsTreeView.SetRootItems(roots);
        }
        else
        {
            detailsTreeView.SetRootItems<string>(new List<TreeViewItemData<string>>());
        }

    }
}