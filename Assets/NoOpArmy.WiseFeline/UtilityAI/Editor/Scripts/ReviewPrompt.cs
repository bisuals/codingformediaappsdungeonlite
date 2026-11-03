using UnityEditor;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// Asks for an Asset Store review once, at most twice, and then never again.
    /// <para>
    /// The trigger is the number of editor sessions the package has been installed for, not time and not
    /// usage, because a session is the one thing which can be counted without watching anything the user
    /// does. Around the tenth session a single dialog offers to open the store page, to be asked one last
    /// time about fifteen sessions later, or to stop asking. Every answer is final in the sense that the
    /// prompt can never appear more than twice on a machine.
    /// </para>
    /// <para>
    /// Nothing here is sent anywhere. The only outbound call in the whole class is
    /// <see cref="Application.OpenURL"/> on the store page, and only when the user asks for it.
    /// </para>
    /// </summary>
    internal static class ReviewPrompt
    {
        // ---------------------------------------------------------------- persisted state (machine wide)

        /// <summary>How many editor sessions this machine has started with the package installed.</summary>
        private const string SessionCountKey = "NoOpArmy.WiseFeline.ReviewPrompt.SessionCount";

        /// <summary>One of the Stage constants below.</summary>
        private const string StageKey = "NoOpArmy.WiseFeline.ReviewPrompt.Stage";

        /// <summary>The session number at which the next (and possibly last) ask is due.</summary>
        private const string NextSessionKey = "NoOpArmy.WiseFeline.ReviewPrompt.NextSession";

        /// <summary>
        /// The key the older RatePackage window stored this edition's answer under. Anyone who clicked
        /// "Rate Now" or "Don't show this again" there has already been asked and answered,
        /// and updating the package must not restart the question. Per package, so it lives with the
        /// store link in <see cref="WiseFelineLinks"/>.
        /// </summary>
        private const string LegacyRatedKey = WiseFelineLinks.LegacyRatedKey;

        // ---------------------------------------------------------------- per session state

        /// <summary>Set once per session so that domain reloads do not each count as a new session.</summary>
        private const string SessionCountedSessionKey = "NoOpArmy.WiseFeline.ReviewPrompt.SessionCounted";

        /// <summary>Set once per session so that at most one attempt to ask is made per session.</summary>
        private const string AskScheduledSessionKey = "NoOpArmy.WiseFeline.ReviewPrompt.AskScheduled";

        // ---------------------------------------------------------------- schedule

        private const int StageNotAsked = 0;
        private const int StageDeferredOnce = 1;
        private const int StageFinished = 2;

        /// <summary>Long enough that the package has clearly been kept, short enough to still matter.</summary>
        private const int FirstAskSession = 10;

        /// <summary>How much later "maybe later" means, in sessions.</summary>
        private const int DeferredAskDelay = 15;

        /// <summary>
        /// Stops the prompt from ever appearing again, whatever stage it was at. Also called by the Getting
        /// Started window when the user opens the store page from there.
        /// </summary>
        internal static void MarkFinished()
        {
            EditorPrefs.SetInt(StageKey, StageFinished);
        }

        [InitializeOnLoadMethod]
        private static void OnEditorStartup()
        {
            // A build machine has nobody to ask and its sessions are not user sessions.
            if (Application.isBatchMode)
                return;

            if (EditorPrefs.GetInt(StageKey, StageNotAsked) >= StageFinished)
                return;

            if (EditorPrefs.GetBool(LegacyRatedKey, false))
            {
                MarkFinished();
                return;
            }

            // Counted even in the sessions which cannot ask, because a session the user spent in the editor
            // is a session either way. Only the asking is suppressed below, never the counting.
            int sessions = CountSessionOnce();

            // The welcome window is this session's one interruption; a review ask stacked on top of it is
            // the definition of nagging. The counter already moved, so a later session asks instead.
            if (GettingStartedWindow.AutoOpensThisSession)
                return;

            if (sessions < EditorPrefs.GetInt(NextSessionKey, FirstAskSession))
                return;

            // One attempt per session. If the attempt below is skipped or lost to a domain reload, the
            // stored state is untouched and the next session tries again.
            if (SessionState.GetBool(AskScheduledSessionKey, false))
                return;
            SessionState.SetBool(AskScheduledSessionKey, true);

            // Modal dialogs cannot be opened from inside [InitializeOnLoadMethod].
            EditorApplication.delayCall += Ask;
        }

        private static int CountSessionOnce()
        {
            int sessions = EditorPrefs.GetInt(SessionCountKey, 0);

            // SessionState survives assembly reloads, including the ones entering and leaving play mode
            // cause, and is cleared when the editor quits. That is exactly one increment per session.
            if (SessionState.GetBool(SessionCountedSessionKey, false))
                return sessions;

            sessions++;
            EditorPrefs.SetInt(SessionCountKey, sessions);
            SessionState.SetBool(SessionCountedSessionKey, true);
            return sessions;
        }

        private static void Ask()
        {
            // Entering play mode reloads the domain, so this can in principle be reached with the game
            // running, where a modal dialog would freeze the play session. Leave every stored value alone
            // and let a later session ask.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            int stage = EditorPrefs.GetInt(StageKey, StageNotAsked);
            if (stage >= StageFinished)
                return;

            if (stage == StageNotAsked)
                AskFirstTime();
            else
                AskLastTime();
        }

        private static void AskFirstTime()
        {
            // The cancel button is also what closing the dialog returns, so the gentlest of the three
            // answers is the one wired to it.
            int choice = EditorUtility.DisplayDialogComplex(
                "Wise Feline",
                "You have been building with Wise Feline for a while now. If it has been useful, a couple of " +
                "lines on the Asset Store are the single most helpful thing you can do for it, because that " +
                "is how other developers find it.\n\n" +
                "No rush, and we will not keep asking.",
                "Leave a review",   // 0
                "Maybe later",      // 1, and what closing the dialog returns
                "Don't ask again"); // 2

            switch (choice)
            {
                case 0:
                    OpenStorePage();
                    break;

                case 1:
                    Defer();
                    break;

                default:
                    MarkFinished();
                    break;
            }
        }

        private static void AskLastTime()
        {
            // "Maybe later" was already spent, so offering it a second time would be a promise this class
            // does not keep. Two honest buttons instead, both of them final.
            bool review = EditorUtility.DisplayDialog(
                "Wise Feline",
                "Last time we ask: if Wise Feline has been useful, a short review on the Asset Store helps " +
                "other developers find it.",
                "Leave a review",
                "No thanks");

            if (review)
                OpenStorePage();
            else
                MarkFinished();
        }

        private static void OpenStorePage()
        {
            Application.OpenURL(WiseFelineLinks.AssetStorePage);
            MarkFinished();
        }

        private static void Defer()
        {
            EditorPrefs.SetInt(StageKey, StageDeferredOnce);
            EditorPrefs.SetInt(
                NextSessionKey,
                EditorPrefs.GetInt(SessionCountKey, FirstAskSession) + DeferredAskDelay);
        }
    }
}
