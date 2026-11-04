using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The per-package identity below is selected by the edition sentinel comments when
/// NoOpArmy &gt; Publishing prepares a package; the working tree carries the Ultimate identity.
/// Rating key is what we use to store our stuff in prefs.
/// It has some garbage at the end intentionally.
/// </summary>
//[InitializeOnLoad]
public class RatePackage : EditorWindow
{
    #region DEN
    #endregion

    #region BB
    // The historical Blackboards releases shipped with Den's rating key by mistake; it stays so that
    // customers who already answered are not asked again after an update.
    #endregion

    #region Remembrance
    #endregion

    #region IMAP
    #endregion

    #region Lite
    private const string RATING_KEY = "RateUAILite.RatingStatus21";
    private const string ASSET_URL = "https://assetstore.unity.com/packages/slug/249840";
    private const string ASSET_NAME = "Wise Feline Utility AI (Lite Version)";
    #endregion

    #region Ultimate
    #endregion

    #region SmartObjects
    #endregion

    // A flag to indicate if the user has rated the package or not
    private static bool HasRated => EditorPrefs.GetBool(RATING_KEY, false);

    // A method to show the rating window
    public static void ShowWindow()
    {
        // Check if the user has already rated the package
        if (HasRated) return;

        // Get the existing window or create a new one
        var window = GetWindow<RatePackage>("Rate Package");

        // Set the window size and position
        window.minSize = new Vector2(500, 350);
        window.maxSize = new Vector2(500, 350);
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 500, 350);

        // Show the window
        window.Show();
    }

    // A method to draw the window content
    private void OnGUI()
    {
        // Draw a label with the message
        EditorGUILayout.LabelField("Thank you for using NoOpArmy's products. We hope you find them useful and fun to use.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        // Draw a label with the rating request
        EditorGUILayout.LabelField($"If you like {ASSET_NAME}, please consider leaving a rating and a review on the Asset Store. It would help us a lot and motivates us to keep improving it even more.", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        if (GUILayout.Button("Rate Now"))
        {
            // Open the Asset Store link in the browser
            Application.OpenURL(ASSET_URL);

            // Set the rating status to true and close the window
            EditorPrefs.SetBool(RATING_KEY, true);
            choiceMade = true;
            Close();
        }
        if (GUILayout.Button("Ultimate version, view and wish list or buy"))
        {
            // Open the Asset Store link in the browser
            Application.OpenURL("https://assetstore.unity.com/packages/slug/235906");
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Maybe Later"))
        {
            choiceMade = true;
            RecordDismissed();
            Close();
        }
        if (GUILayout.Button("Don't show this again"))
        {
            EditorPrefs.SetBool(RATING_KEY, true);
            choiceMade = true;
            Close();
        }
    }

    private void OnDestroy()
    {
        // Closing the window with its X is an answer too, and it means the same thing "Maybe Later"
        // does. Without this the window would come back on the very next session.
        if (!choiceMade && !HasRated)
            RecordDismissed();
    }

    // ------------------------------------------------------------------ session counted auto-show
    //
    // The window used to open itself from every domain reload until one of the suppress buttons was
    // clicked. It now opens itself the way the utility AI packages' ReviewPrompt does: around the
    // tenth editor session, at most twice ever, with "Maybe Later" (or closing the window) deferring
    // the second and last ask by another fifteen sessions. Every package ships this file with its own
    // region uncommented above, so the counters below are keyed off RATING_KEY and never collide
    // between packages.
    //
    // In the packages which also ship the utility AI editor assembly (Ultimate and Lite), that
    // assembly's ReviewPrompt owns the asking and honors RATING_KEY, so this window stays quiet
    // there - detected through reflection because this file cannot reference that assembly without
    // breaking the packages which do not include it.

    private bool choiceMade;

    private const int FirstAskSession = 10;
    private const int DeferredAskDelay = 15;

    private const int StageNotAsked = 0;
    private const int StageDeferredOnce = 1;
    private const int StageFinished = 2;

    private static string SessionCountKey => RATING_KEY + ".Sessions";
    private static string NextSessionKey => RATING_KEY + ".NextAskSession";
    private static string StageKey => RATING_KEY + ".AutoAskStage";
    private static string SessionCountedKey => RATING_KEY + ".SessionCounted";
    private static string AskScheduledKey => RATING_KEY + ".AskScheduled";

    private static bool SessionPromptShipsInProject =>
        Type.GetType("NoOpArmy.WiseFeline.ReviewPrompt, NoOpArmy.WiseFeline.Editor") != null;

    [InitializeOnLoadMethod]
    private static void OnEditorStartup()
    {
        if (Application.isBatchMode)
            return;

        if (HasRated)
            return;

        if (SessionPromptShipsInProject)
            return;

        int sessions = CountSessionOnce();

        if (EditorPrefs.GetInt(StageKey, StageNotAsked) >= StageFinished)
            return;

        if (sessions < EditorPrefs.GetInt(NextSessionKey, FirstAskSession))
            return;

        // One attempt per session, and SessionState survives domain reloads, so a reload can never
        // schedule a second window.
        if (SessionState.GetBool(AskScheduledKey, false))
            return;
        SessionState.SetBool(AskScheduledKey, true);

        // A window cannot be created from inside [InitializeOnLoadMethod], and a modal-ish popup in
        // the middle of a play session would be rude either way.
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !HasRated)
                ShowWindow();
        };
    }

    private static int CountSessionOnce()
    {
        int sessions = EditorPrefs.GetInt(SessionCountKey, 0);

        // SessionState survives assembly reloads and is cleared when the editor quits, which makes
        // this exactly one increment per editor session instead of one per domain reload.
        if (SessionState.GetBool(SessionCountedKey, false))
            return sessions;

        sessions++;
        EditorPrefs.SetInt(SessionCountKey, sessions);
        SessionState.SetBool(SessionCountedKey, true);
        return sessions;
    }

    private static void RecordDismissed()
    {
        if (EditorPrefs.GetInt(StageKey, StageNotAsked) == StageNotAsked)
        {
            EditorPrefs.SetInt(StageKey, StageDeferredOnce);
            EditorPrefs.SetInt(
                NextSessionKey,
                EditorPrefs.GetInt(SessionCountKey, FirstAskSession) + DeferredAskDelay);
        }
        else
        {
            EditorPrefs.SetInt(StageKey, StageFinished);
        }
    }
}