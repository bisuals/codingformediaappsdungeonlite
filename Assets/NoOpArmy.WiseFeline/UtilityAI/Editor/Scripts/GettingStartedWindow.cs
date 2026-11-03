using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// The outbound links the onboarding UI uses, in one place so that the Getting Started window and the
    /// review prompt cannot drift apart. Every one of them already existed somewhere in the package or the
    /// manual; nothing here opens anything by itself and nothing in this file talks to the network.
    /// </summary>
    internal static class WiseFelineLinks
    {
        /// <summary>Root of the online manual, the same URL the Wise Feline window's toolbar opens.</summary>
        internal const string Documentation = "https://www.nooparmygames.com/WF-UtilityAI-Unity/";

        /// <summary>The manual's first article, which walks through the demo scene the window can open.</summary>
        internal const string CombatQuickstart = Documentation + "articles/combat-quickstart.html";

        /// <summary>Support server, the same invite the Wise Feline window's toolbar and the manual use.</summary>
        internal const string Discord = "https://discord.gg/FA8R7APZWR";

        /// <summary>The video tutorial channel linked from the manual's landing page.</summary>
        internal const string YouTube = "https://www.youtube.com/nooparmy";

        // The two constants below are per package, exactly like the regions in RatePackageWindow.cs;
        // the edition sentinel comments pick the right ones when NoOpArmy > Publishing prepares a
        // package, so the review link opens the right listing and the right historical "already rated"
        // answer is honored. Only Ultimate and Lite ship this file.

        #region Ultimate
        #endregion

        #region Lite
        internal const string AssetStorePage = "https://assetstore.unity.com/packages/slug/249840";
        internal const string LegacyRatedKey = "RateUAILite.RatingStatus21";
        #endregion
    }

    /// <summary>
    /// The window a new user meets first: a welcome line and the four things worth doing in the first ten
    /// minutes: run the combat quickstart scene, read the article that explains it, find the docs and the
    /// support channels, and leave a review.
    /// <para>
    /// It opens by itself exactly once per machine (see <see cref="ShownKey"/>) and is always reachable from
    /// <c>Window &gt; NoOpArmy &gt; Getting Started</c> afterwards.
    /// </para>
    /// </summary>
    public class GettingStartedWindow : EditorWindow
    {
        /// <summary>Set the first time the window opens itself; never cleared. Machine wide, like all EditorPrefs.</summary>
        private const string ShownKey = "NoOpArmy.WiseFeline.GettingStarted.Shown";

        /// <summary>
        /// Caches this session's answer to "does the welcome window open by itself", because
        /// <see cref="ReviewPrompt"/> asks the same question and the two must agree regardless of which
        /// <see cref="InitializeOnLoadMethodAttribute"/> the editor happens to run first. -1 means undecided.
        /// </summary>
        private const string AutoOpenDecisionSessionKey = "NoOpArmy.WiseFeline.GettingStarted.AutoOpenDecision";

        private const string DemoSceneTitle = "LifeSim Light demo";
        private const string DemoScenePath =
           "Assets/NoOpArmy.WiseFeline/Demos/LifeSimLight/Scene/LightDemo.unity";

        /// <summary>
        /// Whether the welcome window opens by itself during this editor session. Decided once per session on
        /// the first read and then frozen, so that reading it after <see cref="ShownKey"/> has been written
        /// still returns what actually happened this session.
        /// </summary>
        internal static bool AutoOpensThisSession
        {
            get
            {
                const int undecided = -1;

                int cached = SessionState.GetInt(AutoOpenDecisionSessionKey, undecided);
                if (cached != undecided)
                    return cached == 1;

                // A build machine gets no windows and must not burn the one automatic opening a user gets.
                bool decision = !Application.isBatchMode && !EditorPrefs.GetBool(ShownKey, false);
                SessionState.SetInt(AutoOpenDecisionSessionKey, decision ? 1 : 0);
                return decision;
            }
        }

        [MenuItem("Window/NoOpArmy/Getting Started")]
        public static void OpenWindow()
        {
            GettingStartedWindow wnd = GetWindow<GettingStartedWindow>();
            wnd.titleContent = new GUIContent("Getting Started");
            wnd.minSize = new Vector2(430, 440);
            wnd.Show();
        }

        [InitializeOnLoadMethod]
        private static void ScheduleFirstRun()
        {
            // Asked before the flag below is written, so that the session wide answer is cached from the
            // state this session started with. ReviewPrompt reads the same property.
            if (!AutoOpensThisSession)
                return;

            // AutoOpensThisSession keeps its cached answer for the rest of the session, so this raw read is
            // what stops a later domain reload from opening the window a second time. If a reload happens
            // before the delayed call below runs, the flag is still unset and the reload reschedules it.
            if (EditorPrefs.GetBool(ShownKey, false))
                return;

            // A window cannot be created from inside [InitializeOnLoadMethod]: the editor has no layout to
            // dock it into yet. delayCall runs on the first editor update, once it has one.
            EditorApplication.delayCall += AutoOpen;
        }

        private static void AutoOpen()
        {
            if (EditorPrefs.GetBool(ShownKey, false))
                return;

            EditorPrefs.SetBool(ShownKey, true);
            OpenWindow();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            // Everything lives in a scroll view so that a short window never clips the review section.
            var content = new ScrollView(ScrollViewMode.Vertical);
            content.style.flexGrow = 1f;
            root.Add(content);

            content.Add(Title("Welcome to Wise Feline"));
            content.Add(Paragraph(
               "Wise Feline agents score everything they could do right now and run the winner. There is no " +
               "tree to wire and no state to transition out of.\n\n" +
               "The quickest way to see what that buys you is the LifeSim Light demo: agents whose hunger, " +
               "energy, bladder and boredom pull them between the bed, the fridge, the WC and the TV, with " +
               "every decision made by considerations scoring their needs.\n\n" +
               "The 10 minute quickstart article below is written against the combat demo of the Ultimate " +
               "version, but the concepts it teaches are exactly the ones this demo runs on."));

            content.Add(SectionHeader("Start here"));
            content.Add(ActionButton("Open the " + DemoSceneTitle + " scene", OpenDemoScene));
            content.Add(ActionButton("Read the 10 minute quickstart", () => Application.OpenURL(WiseFelineLinks.CombatQuickstart)));

            content.Add(SectionHeader("Learn more"));
            content.Add(ActionButton("Full documentation", () => Application.OpenURL(WiseFelineLinks.Documentation)));
            content.Add(ActionButton("Ask a question on Discord", () => Application.OpenURL(WiseFelineLinks.Discord)));
            content.Add(ActionButton("Video tutorials on YouTube", () => Application.OpenURL(WiseFelineLinks.YouTube)));

            content.Add(SectionHeader("Enjoying Wise Feline?"));
            content.Add(Paragraph(
                "Reviews are how other developers find the asset. If it has been useful to you, a couple of " +
                "lines on the Asset Store go a long way."));
            content.Add(ActionButton("Leave a review on the Asset Store", LeaveAReview));

            var footer = Paragraph("Window > NoOpArmy > Getting Started reopens this window at any time.");
            footer.style.marginTop = 14;
            footer.style.fontSize = 11;
            footer.style.opacity = 0.7f;
            content.Add(footer);
        }

        private static void LeaveAReview()
        {
            Application.OpenURL(WiseFelineLinks.AssetStorePage);

            // Somebody who came here on their own should never be asked again by the session counter.
            ReviewPrompt.MarkFinished();
        }

        private static void OpenDemoScene()
        {
            // Loading a scene while the game is running throws, so say why instead.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Wise Feline",
                    "Exit play mode first, then open the " + DemoSceneTitle + " scene.",
                    "OK");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null)
            {
                EditorUtility.DisplayDialog(
                    "Wise Feline",
                    "The " + DemoSceneTitle + " scene was not found at\n\n" + DemoScenePath + "\n\n" +
                    "It ships with the package under Demos. Reimport the package, or read " +
                    "the quickstart online instead.",
                    "OK");
                return;
            }

            // Returns false when the user cancels out of the save prompt, which means "do not open anything".
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        }

        // ---------------------------------------------------------------- small UI builders

        private static Label Title(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 15;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 6;
            return label;
        }

        private static Label Paragraph(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 4;
            return label;
        }

        private static VisualElement SectionHeader(string text)
        {
            var header = new VisualElement();
            header.style.marginTop = 14;
            header.style.marginBottom = 4;

            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(label);

            var rule = new VisualElement();
            rule.style.height = 1;
            rule.style.marginTop = 3;
            rule.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
            header.Add(rule);

            return header;
        }

        private static Button ActionButton(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.style.height = 24;
            button.style.marginTop = 3;
            button.style.marginLeft = 0;
            button.style.marginRight = 0;
            button.style.paddingLeft = 8;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            return button;
        }
    }
}
