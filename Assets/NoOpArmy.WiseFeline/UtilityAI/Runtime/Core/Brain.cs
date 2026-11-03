using NoOpArmy.WiseFeline.DataRecorder;
using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// You should attach this component to any GameObject which wishes to be an AI agent controlled by a set of actions 
    /// </summary>
    public class Brain : MonoBehaviour
    {
        /// <summary>
        /// Assign any component which your AI always needs to this to avoid calling GetComponent in all actions/considerations
        /// </summary>
        public Component context;

        /// <summary>
        /// This event is called when the brain gets fully initialized for you to be able to add your behaviors.
        /// </summary>
        public event Action OnInitialized;

        /// <summary>
        /// Is the brain initialized yet
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Different action selection modes
        /// </summary>
        public enum ActionSelectionAlgorithm : byte
        {
            /// <summary>
            /// Choose the action with the highest score
            /// </summary>
            HighestScore,
            /// <summary>
            /// Randomly choose from the top N, considering only actions with the same priority as the top one
            /// </summary>
            RandomConsideringPriority,
            /// <summary>
            /// Randomly choose from the top N regardless of their priorities
            /// </summary>
            RandomWithoutConsideringPriority,
            /// <summary>
            /// Randomly choose from the top N, weighted by score, considering only actions with the same priority as the top one
            /// </summary>
            WeightedRandomConsideringPriority,
            /// <summary>
            /// Randomly choose from the top N, weighted by score, regardless of their priorities
            /// </summary>
            WeightedRandomWithoutConsideringPriority
        }

        // The enum and the three fields below stay in every edition so that behaviors authored against
        // one edition keep their serialized data in another; Lite only loses the code that reads them
        // (see Think() and GetMaxScoreIndex below) and labels them as what they are: an upsell.
        [Header("Selection Mode, Priority and score delta are Ultimate features")]
        /// <summary>
        /// The algorithm the brain uses for selecting the best action
        /// </summary>
        [Tooltip("The algorithm the brain uses for selecting the best action")]
        public ActionSelectionAlgorithm actionSelectionAlgorithm;

        /// <summary>
        /// The top N items are considered for random action selection and this value is what N is.
        /// </summary>
        [Tooltip("The top N items are considered for random action selection and this value is what N is.")]
        public int itemCountForRandomActionSelection = 5;

        /// <summary>
        /// If the score difference between the chosen action and the currently executing one is higher than this, then the actions changes to
        /// the chosen one
        /// </summary>
        [Tooltip("If the score difference between the chosen action and the currently executing one is higher than this, then the actions changes to the chosen one")]
        public float scoreDeltaForMidExecutionActionChange = 0;

        /// <summary>
        /// Fires when the list of behaviors available to this brain is modified by calling AddBehavior() or RemoveBehavior()
        /// </summary>
        public event Action OnBehaviorListModified;

        /// <summary>
        /// Fires when the executing action changes.
        /// The first parameter is the previous action and the second one is the action we are changing too
        /// If the first parameter is null then either the previous action is fully finished successfully or with a failure
        /// or this is the first action to execute
        /// </summary>
        public event Action<ActionBase, ActionBase> OnCurrentActionChanged;

        /// <summary>
        /// This event gets fired when an action succeeds
        /// </summary>
        public event Action<ActionBase> OnActionSucceeded;

        /// <summary>
        /// This event gets fired when an action fails
        /// </summary>
        public event Action<ActionBase> OnActionFailed;

        /// <summary>
        /// The sets of behavior for an agent
        /// </summary>
        [SerializeField]
        private AgentBehavior _behavior;

        /// <summary>
        /// The wait period between calculations of action scores
        /// </summary>
        public float _thinkDuration = 2f;

        /// <summary>
        /// The wait period between calls to update target lists.
        /// Since updating this list is usually heavy, you should do this less often than score calculations and deal with null targets in your actions.
        /// </summary>
        public float _updateTargetsDuration = 5f;

        /// <summary>
        /// Gets the currently executing action class
        /// </summary>
        public ActionBase currentAction { get { return _currentAction; } }

        public AgentBehavior Behavior { get { if (_clonedBehavior == null) return _behavior; else return _clonedBehavior; } }
        private AgentBehavior _clonedBehavior;
        private List<ActionData> _actionDataList;
        private readonly List<PendingActionSetChange> _pendingActionSetChanges = new();

        /// <summary>
        /// Should this brain component automatically think when time passes by based on ThinkDuration and UpdateTargetsDuration
        /// </summary>
        [SerializeField]
        private bool _pauseAutomaticThinking = false;
        private bool _pauseAction = false;
        private float _thinkTimer = 0;
        private float _updateTimer = 0;
        private ActionBase _currentAction;

        // Think(), ActionSucceeded() and ActionFailed() call into user code (action callbacks and the brain events).
        // While one of them runs, this is above 0 and changes which would re-enter the brain (finishing the running
        // action, thinking again) are recorded below and applied once the outermost call returns.
        private int _brainCallbackDepth;
        // The running action whose set was removed while the brain was inside one of those calls.
        private ActionBase _removedRunningAction;
        // Think() was requested while the brain was inside one of those calls.
        private bool _thinkRequested;
        // Incremented whenever a set is actually removed, so Think() can tell whether its chosen action is still in the brain.
        private int _actionSetRemovalCount;

        private DataRecorder.DataRecorder dataRecorder;
        public int dataRecorderRecordIndex { get; private set; }


        //profiler markers
        private static readonly ProfilerMarker thinkMarker = new(ProfilerCategory.Ai, "WiseFelineThink");
        private static readonly ProfilerMarker getTargetsMarker = new(ProfilerCategory.Ai, "WiseFelineGetTargets");

        private void Awake()
        {
            dataRecorder = GetComponent<DataRecorder.DataRecorder>();
            // The brain always runs off its own copy so that the shared asset is never mutated.
            // Without an assigned asset there is nothing to copy, so an empty one is built directly
            // instead of creating a placeholder just to clone it.
            _clonedBehavior = _behavior == null ? AgentBehavior.GetEmpty() : _behavior.Clone();
        }

        private void Start()
        {
            int count = 0;
            for (int i = 0; i < Behavior.ActionSets.Count; i++)
            {
                count += Behavior.ActionSets[i].Actions.Count;
                for (int j = 0; j < Behavior.ActionSets[i].Actions.Count; j++)
                {
                    ActionBase action = Behavior.ActionSets[i].Actions[j];
                    if (action != null)
                    {
                        action.Initialize(this);
                    }
                }
            }
            // Sets added or removed before this point were queued; AddRuntimeActionSet initializes the actions it adds.
            ApplyPendingActionSetChanges();
            _actionDataList = new List<ActionData>(count);
            IsInitialized = true;
            OnInitialized?.Invoke();
            RebuildMainList();
            UpdateActionsTargets();
            Think();
        }

        private void RebuildMainList()
        {
            _actionDataList.Clear();
            int k = 0;
            for (int i = 0; i < Behavior.ActionSets.Count; i++)
            {
                ActionSet set = Behavior.ActionSets[i];
                for (int j = 0; j < set.Actions.Count; j++, k++)
                {
                    ActionBase action = set.Actions[j];
                    _actionDataList.Add(new ActionData(set, action, action.GetScore(dataRecorder, dataRecorderRecordIndex), action._priority, k));
                }
            }
        }

        private List<ActionScoringData> UpdateActionScores()
        {
            List<ActionScoringData> pad = new(_actionDataList.Count);
            _actionDataList.Clear();
            int k = 0;
            for (int i = 0; i < Behavior.ActionSets.Count; i++)
            {
                ActionSet set = Behavior.ActionSets[i];
                for (int j = 0; j < set.Actions.Count; j++, k++)
                {
                    ActionBase action = set.Actions[j];
                    _actionDataList.Add(new ActionData(set, action, action.GetScore(dataRecorder, dataRecorderRecordIndex), action._priority, k));
                    pad.Add(new ActionScoringData(action.Score, action._priority, k));
                }
            }
            return pad;
        }

        private void Update()
        {
            if (Behavior == null || _actionDataList.Count == 0)
                return;
            if (!_pauseAutomaticThinking)
            {
                _updateTimer += Time.deltaTime;
                if (_updateTimer >= _updateTargetsDuration)
                {
                    UpdateActionsTargets();
                }

                _thinkTimer += Time.deltaTime;
                if (_thinkTimer >= _thinkDuration || _currentAction == null)
                {
                    _thinkTimer = 0;
                    Think();
                }
            }

            if (!_pauseAction)
            {
                if (_currentAction) _currentAction.Update();
            }
        }

        private void LateUpdate()
        {
            if (!_pauseAction)
            {
                if (_currentAction) _currentAction.LateUpdate();
            }
        }

        private void FixedUpdate()
        {
            if (!_pauseAction)
            {
                if (_currentAction) _currentAction.FixedUpdate();
            }
        }

        /// <summary>
        /// Puases/Resumes thinking which changes action scores
        /// </summary>
        /// <param name="pause">Pauses the thinking if true and resumes it if false.</param>
        public void PauseThinking(bool pause)
        {
            _pauseAutomaticThinking = pause;
        }

        /// <summary>
        /// Pauses/Resumes executing the current action.
        /// </summary>
        /// <param name="pause">pauses ecution if true and resumes it if false</param>
        public void PauseExecutingActions(bool pause)
        {
            _pauseAction = pause;
        }

        /// <summary>
        /// Updates the target list of all actions so the updated targets can be used while thinking
        /// </summary>
        public void UpdateActionsTargets()
        {
            getTargetsMarker.Begin();

            _updateTimer = 0;

            for (int i = 0; i < Behavior.ActionSets.Count; i++)
            {
                ActionSet set = Behavior.ActionSets[i];
                for (int j = 0; j < set.Actions.Count; j++)
                {
                    ActionBase action = set.Actions[j];
                    action.UpdateTargetsList();
                }
            }

            getTargetsMarker.End();
        }

        /// <summary>
        /// Calculates all action scores and selects the next action to execute. This might or might not change the currently executing action
        /// </summary>
        /// <remarks>
        /// When this is called from a brain event handler or an action callback which the brain itself is running
        /// (for example OnCurrentActionChanged or an action's OnStart), the brain thinks right after that call returns instead.
        /// </remarks>
        public void Think()
        {
            if (_brainCallbackDepth > 0)
            {
                _thinkRequested = true;
                return;
            }
            _thinkRequested = false;

            _brainCallbackDepth++;
            thinkMarker.Begin();
            try
            {
                ThinkInternal();
            }
            finally
            {
                thinkMarker.End();
                _brainCallbackDepth--;
            }
            ApplyDeferredChanges();
        }

        private void ThinkInternal()
        {
            //if there are no actions
            if (_actionDataList.Count == 0)
            {
                return;
            }

            //If the current action is not interruptable
#if UNITY_EDITOR
            if (dataRecorder)
            {
                dataRecorderRecordIndex = dataRecorder.GetCurrentFrameRecord();
            }
            if (Debug.isDebugBuild)
            {
                dataRecorder?.AddSubRecord(new SubRecord
                {
                    text = "Actions",
                    indentationLevel = 0
                }
                , dataRecorderRecordIndex);
            }
#endif
            
            //Update all action scores by evaluating all actions and their considerations
            List<ActionScoringData> scratchPad = UpdateActionScores();
            int maxIndex = GetMaxScoreIndexLite(scratchPad);
            if (maxIndex >= 0)
            {
                // The callbacks below can add or remove sets, which rebuilds _actionDataList, so keep the chosen entry
                // instead of indexing the list again afterwards.
                ActionData chosen = _actionDataList[maxIndex];
                bool chosenStillInBrain = true;
                if (_currentAction != chosen.Action)//a new action is chosen
                {
                    var prev = _currentAction;
                    int removalCount = _actionSetRemovalCount;
                    if (_currentAction) _currentAction.Finish();
                    // prev is being replaced anyway, so its set being removed from its own OnFinish needs no extra handling.
                    if (_removedRunningAction == prev) _removedRunningAction = null;
                    if (removalCount != _actionSetRemovalCount && !BehaviorContains(chosen.Action))
                    {
                        // prev's OnFinish removed the set of the chosen action, so it is not started.
                        chosenStillInBrain = false;
                        _currentAction = null;
                        if (prev) OnCurrentActionChanged?.Invoke(prev, null);
                        _thinkRequested = true;
                    }
                    else
                    {
                        _currentAction = chosen.Action;
                        OnCurrentActionChanged?.Invoke(prev, _currentAction);
                        if (_currentAction == chosen.Action && _removedRunningAction == chosen.Action)
                        {
                            // A handler removed the set of the chosen action before it started, so it is not started
                            // (and therefore not finished either) and the brain thinks again once this call returns.
                            chosenStillInBrain = false;
                            _removedRunningAction = null;
                            _currentAction = null;
                            OnCurrentActionChanged?.Invoke(chosen.Action, null);
                            _thinkRequested = true;
                        }
                        else if (_currentAction == chosen.Action)
                        {
                            _currentAction.Start();
                        }
                    }
                }
                Behavior.OnThinkDone?.Invoke(chosenStillInBrain ? GetCurrentActionData(chosen, maxIndex) : default);
#if UNITY_EDITOR
                dataRecorder?.AddShape(new Shape
                {
                    color = Color.red,
                    size = 1,
                    Type = ShapeType.WiredSphere,
                    position = transform.position + Vector3.up
                }, dataRecorderRecordIndex);
#endif
            }
            else
            {
                Behavior.OnThinkDone?.Invoke(default);
#if UNITY_EDITOR
                dataRecorder?.AddShape(new Shape
                {
                    color = Color.white,
                    size = 1,
                    Type = ShapeType.WiredSphere,
                    position = transform.position + Vector3.up
                }, dataRecorderRecordIndex);
#endif
            }
        }

        /// <summary>
        /// Returns the current entry for the action Think() chose. If the callbacks rebuilt the list, the action is looked up
        /// again so the entry's index matches the new list; an action which is no longer in the brain gives default.
        /// </summary>
        private ActionData GetCurrentActionData(ActionData chosen, int index)
        {
            if (index < _actionDataList.Count && _actionDataList[index].Action == chosen.Action)
                return _actionDataList[index];
            for (int i = 0; i < _actionDataList.Count; i++)
            {
                if (_actionDataList[i].Action == chosen.Action)
                    return _actionDataList[i];
            }
            return default;
        }

        private bool BehaviorContains(ActionBase action)
        {
            if (action == null) return false;
            for (int i = 0; i < Behavior.ActionSets.Count; i++)
            {
                if (Behavior.ActionSets[i].Actions.Contains(action))
                    return true;
            }
            return false;
        }


        private int GetMaxScoreIndexLite(List<ActionScoringData> pad)
        {
            pad.Sort((x, y) =>
            {
                if (x.Score > y.Score)
                    return -1;
                if (y.Score > x.Score)
                    return 1;
                return 0;
            });

            // Like the Ultimate selection, an action whose score is 0 is never started or switched to.
            if (pad.Count == 0 || pad[0].Score <= 0)
                return -1;
            return pad[0].Index;
        }


        internal void ActionSucceeded(ActionBase action)
        {
            CompleteAction(action, OnActionSucceeded);
        }

        internal void ActionFailed(ActionBase action)
        {
            CompleteAction(action, OnActionFailed);
        }

        private void CompleteAction(ActionBase action, Action<ActionBase> completionEvent)
        {
            _brainCallbackDepth++;
            try
            {
                completionEvent?.Invoke(action);
                // The action is finishing here, so a handler above which removed its set only needs the brain to think again.
                if (_removedRunningAction == action && _currentAction == action)
                {
                    _removedRunningAction = null;
                    _thinkRequested = true;
                }
                if (action) action.Finish();
                if (_currentAction == action)
                {
                    _currentAction = null;
                    OnCurrentActionChanged?.Invoke(action, null);
                }
            }
            finally
            {
                _brainCallbackDepth--;
            }
            ApplyDeferredChanges();
        }

        /// <summary>
        /// Applies what was recorded while the brain was running Think(), ActionSucceeded() or ActionFailed(): finishes a
        /// running action whose set was removed and thinks again. Does nothing while one of those calls is still running.
        /// </summary>
        private void ApplyDeferredChanges()
        {
            if (_brainCallbackDepth > 0) return;
            ActionBase removed = _removedRunningAction;
            _removedRunningAction = null;
            bool think = _thinkRequested || removed != null;
            if (removed != null && removed == _currentAction)
                EndRunningActionAfterRemoval();
            if (think)
                Think();
        }

        /// <summary>
        /// Ends the running action the same way Think() ends an action which gets interrupted by another one.
        /// </summary>
        private void EndRunningActionAfterRemoval()
        {
            var prev = _currentAction;
            _currentAction = null;
            _brainCallbackDepth++;
            try
            {
                prev.Finish();
                OnCurrentActionChanged?.Invoke(prev, null);
            }
            finally
            {
                _brainCallbackDepth--;
            }
        }

        /// <summary>
        /// Adds actions inside a behavior agent asset to the actions of this brain
        /// </summary>
        /// <param name="agentBehavior">The agent behavior asset to add</param>
        /// <remarks>
        /// An action set that is already in the brain (same set asset) is skipped with a warning.
        /// Different sets which contain the same kind of action each keep their own instance of it, so design your behaviors
        /// so they don't have duplicate actions. You usually do not need multiple instances of an action in the same brain.
        /// Calling this before the brain has initialized (before its Start runs) is allowed: the sets are added when the brain initializes.
        /// </remarks>
        public void AddBehavior(AgentBehavior agentBehavior)
        {
            if (!Application.isPlaying) return;
            if (agentBehavior == null) return;
            if (agentBehavior.ActionSets.Count == 0) return;
            for (int i = 0; i < agentBehavior.ActionSets.Count; i++)
            {
                AddActionSetInternal(agentBehavior.ActionSets[i]);
            }
            if (IsInitialized)
            {
                RebuildMainList();
                OnBehaviorListModified?.Invoke();
            }
        }

        /// <summary>
        /// Adds an action set to the set of behaviors this brain has available to score and execute
        /// </summary>
        /// <param name="set">The action set to add</param>
        /// <remarks>
        /// An action set that is already in the brain is skipped with a warning.
        /// You usually do not need multiple instances of an action in the same brain.
        /// Calling this before the brain has initialized (before its Start runs) is allowed: the set is added when the brain initializes.
        /// </remarks>
        public void AddActionSet(ActionSet set)
        {
            if (!Application.isPlaying) return;
            if (set == null) return;

            AddActionSetInternal(set);
            if (IsInitialized)
            {
                RebuildMainList();
                OnBehaviorListModified?.Invoke();
            }
        }

        /// <summary>
        /// Removes actions of a behavior from this brain
        /// </summary>
        /// <param name="agentBehavior">The behavior whose action sets should be removed</param>
        /// <remarks>
        /// Sets the brain does not have are skipped with a warning. If the running action belongs to a removed set,
        /// it is finished as if it was interrupted and the brain thinks again right away.
        /// When this is called from a brain event handler or an action callback which the brain itself is running (OnCurrentActionChanged,
        /// OnActionSucceeded, OnActionFailed, OnStart and so on), the action is ended and the brain thinks once that call returns,
        /// so no callback runs twice.
        /// Calling this before the brain has initialized (before its Start runs) is allowed: the sets are removed when the brain initializes.
        /// </remarks>
        public void RemoveBehavior(AgentBehavior agentBehavior)
        {
            if (!Application.isPlaying) return;
            if (agentBehavior == null) return;
            if (agentBehavior.ActionSets.Count == 0) return;
            bool removedCurrentAction = false;
            foreach (var set in agentBehavior.ActionSets)
            {
                removedCurrentAction |= RemoveActionSetInternal(set);
            }
            if (IsInitialized)
                AfterActionSetsRemoved(removedCurrentAction);
        }

        /// <summary>
        /// Removes a specific action set from the list of behaviors of this brain
        /// </summary>
        /// <param name="set">The action set to remove</param>
        /// <remarks>
        /// A set the brain does not have is skipped with a warning. If the running action belongs to the removed set,
        /// it is finished as if it was interrupted and the brain thinks again right away.
        /// When this is called from a brain event handler or an action callback which the brain itself is running (OnCurrentActionChanged,
        /// OnActionSucceeded, OnActionFailed, OnStart and so on), the action is ended and the brain thinks once that call returns,
        /// so no callback runs twice.
        /// Calling this before the brain has initialized (before its Start runs) is allowed: the set is removed when the brain initializes.
        /// </remarks>
        public void RemoveActionSet(ActionSet set)
        {
            if (!Application.isPlaying) return;
            if (set == null) return;

            bool removedCurrentAction = RemoveActionSetInternal(set);
            if (IsInitialized)
                AfterActionSetsRemoved(removedCurrentAction);
        }

        /// <summary>
        /// Adds the set to the runtime behavior, or queues it until Start when the brain is not initialized yet.
        /// </summary>
        private void AddActionSetInternal(ActionSet set)
        {
            if (set == null) return;
            if (!IsInitialized)
            {
                _pendingActionSetChanges.Add(new PendingActionSetChange(set, true));
                return;
            }
            _clonedBehavior.AddRuntimeActionSet(set, this);
        }

        /// <summary>
        /// Removes the set from the runtime behavior, or queues the removal until Start when the brain is not initialized yet.
        /// Returns true when the removed set contained the running action.
        /// </summary>
        private bool RemoveActionSetInternal(ActionSet set)
        {
            if (set == null) return false;
            if (!IsInitialized)
            {
                _pendingActionSetChanges.Add(new PendingActionSetChange(set, false));
                return false;
            }
            ActionSet removed = _clonedBehavior.RemoveRuntimeActionSet(set, this);
            if (removed != null) _actionSetRemovalCount++;
            return removed != null && _currentAction != null && removed.Actions.Contains(_currentAction);
        }

        private void AfterActionSetsRemoved(bool removedCurrentAction)
        {
            RebuildMainList();
            // Inside Think(), ActionSucceeded() or ActionFailed() (for example from OnCurrentActionChanged or OnActionSucceeded),
            // finishing the action and thinking here would run those callbacks twice, so the outer call does it when it returns.
            bool deferred = _brainCallbackDepth > 0;
            if (removedCurrentAction)
            {
                if (deferred)
                    _removedRunningAction = _currentAction;
                else
                    EndRunningActionAfterRemoval();
            }
            OnBehaviorListModified?.Invoke();
            if (removedCurrentAction && !deferred)
                Think();
        }

        /// <summary>
        /// Applies the AddBehavior/AddActionSet/RemoveBehavior/RemoveActionSet calls made before the brain initialized, in order.
        /// </summary>
        private void ApplyPendingActionSetChanges()
        {
            for (int i = 0; i < _pendingActionSetChanges.Count; i++)
            {
                PendingActionSetChange change = _pendingActionSetChanges[i];
                if (change.Set == null) continue;
                if (change.Add)
                    _clonedBehavior.AddRuntimeActionSet(change.Set, this);
                else
                    _clonedBehavior.RemoveRuntimeActionSet(change.Set, this);
            }
            _pendingActionSetChanges.Clear();
        }

        private readonly struct PendingActionSetChange
        {
            public readonly ActionSet Set;
            public readonly bool Add;

            public PendingActionSetChange(ActionSet set, bool add)
            {
                Set = set;
                Add = add;
            }
        }

        private void OnDestroy()
        {
            Destroy(Behavior);
        }
    }

    /// <summary>
    /// This struct represents the runtime info for a an action and its final score
    /// </summary>
    public struct ActionData
    {
        /// <summary>
        /// The action set this instance refers to.
        /// </summary>
        public ActionSet ActionSet;

        /// <summary>
        /// Which action this instance refers to.
        /// </summary>
        public ActionBase Action;

        /// <summary>
        /// The score of the action in the last think operation.
        /// </summary>
        public float Score;

        /// <summary>
        /// Priority of the action
        /// </summary>
        public int Priority;

        /// <summary>
        /// index of the action in the main list
        /// </summary>
        public int Index;

        public ActionData(ActionSet set, ActionBase action, float score, int priority, int index)
        {
            ActionSet = set;
            Action = action;
            Score = score;
            Priority = priority;
            Index = index;
        }
    }

    public struct ActionScoringData
    {
        /// <summary>
        /// The score of the action in the last think operation.
        /// </summary>
        public float Score;

        /// <summary>
        /// Priority of the action
        /// </summary>
        public int Priority;

        /// <summary>
        /// index of the action in the main list
        /// </summary>
        public int Index;

        public ActionScoringData(float score, int priority, int index)
        {
            Score = score;
            Priority = priority;
            Index = index;
        }
    }
}
