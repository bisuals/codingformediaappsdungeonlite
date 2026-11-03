using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// The About dialog of the Wise Feline editor. It shows the package version, the credits and a
    /// link to the product page. The version comes from <see cref="WiseFelineEditorInfo.Version"/>,
    /// so a release bump is one edit in one file.
    /// </summary>
    public class AboutEditor : EditorWindow
    {
        private const string WindowTitle = "About Wise Feline";

        // The window is a dialog, so the same value is used for the smallest and the largest size.
        private static readonly Vector2 WindowSize = new Vector2(360f, 340f);

        /// <summary>
        /// Opens the About window as a utility window, or focuses it when it is already open.
        /// </summary>
        public static void OpenWindow()
        {
            GetWindow<AboutEditor>(true, WindowTitle, true);
        }

        /// <summary>
        /// Builds the window content. Unity calls this both when the window is opened and when it
        /// is recreated after a domain reload.
        /// </summary>
        public void CreateGUI()
        {
            titleContent = new GUIContent(WindowTitle);
            minSize = WindowSize;
            maxSize = WindowSize;

            var root = rootVisualElement;

            var visualTree = LoadFirst<VisualTreeAsset>("AboutView t:VisualTreeAsset", "AboutView.uxml");
            if (visualTree == null)
            {
                // The layout file is part of the package, so this only happens if it was deleted.
                // A label is more useful here than a null reference exception in the console.
                root.Add(new Label("AboutView.uxml could not be found in the project."));
                return;
            }
            visualTree.CloneTree(root);

            var styleSheet = LoadFirst<StyleSheet>("AboutView t:StyleSheet", "AboutView.uss");
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
                root.styleSheets.Add(styleSheet);

            var version = root.Q<Label>("version");
            if (version != null)
                version.text = $"Wise Feline v{WiseFelineEditorInfo.Version}";

            var url = root.Q<Label>("url");
            if (url != null)
            {
                url.text = WiseFelineEditorInfo.ProductUrl;
                url.RegisterCallback<MouseDownEvent>(OpenProductPage);
            }

            var ok = root.Q<Button>("ok");
            if (ok != null)
                ok.clicked += Close;
        }

        /// <summary>
        /// Finds an asset by name through the asset database instead of a fixed folder path, so the
        /// package folder can be moved or renamed and the window still opens.
        /// </summary>
        /// <typeparam name="T">The type of the asset to load.</typeparam>
        /// <param name="filter">An asset database search filter, for example "AboutView t:StyleSheet".</param>
        /// <param name="fileName">The file name the matched path has to end with.</param>
        /// <returns>The first matching asset, or null when there is none.</returns>
        private static T LoadFirst<T>(string filter, string fileName) where T : UnityEngine.Object
        {
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                    continue;
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    return asset;
            }
            return null;
        }

        private void OpenProductPage(MouseDownEvent evt)
        {
            Application.OpenURL(WiseFelineEditorInfo.ProductUrl);
        }
    }
}
