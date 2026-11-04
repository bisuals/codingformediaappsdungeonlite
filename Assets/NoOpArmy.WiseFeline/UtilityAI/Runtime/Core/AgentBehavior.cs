using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// This class is used to serialize a group of action sets as a scriptable object.
    /// You usually create one of these by going to the Assets>Create>NoOpArmy>WiseFeline>AgentBehavior menu and then select it and open the Window>NoOpArmy>WiseFeline to add action sets,
    /// actions and considerations to it and modify their parameters.
    /// </summary>
    [CreateAssetMenu(menuName = "NoOpArmy/Wise Feline/AgentBehavior")]
    public class AgentBehavior : ScriptableObject
    {
        /// <summary>
        /// Creates an in-memory behavior with no action sets, used as the placeholder for a brain
        /// that was not given a behavior asset. Every call returns a fresh instance; there is no
        /// shared one, so nothing survives into the next Play Mode session when Domain Reload is
        /// disabled.
        /// </summary>
        public static AgentBehavior GetEmpty()
        {
            var behavior = CreateInstance<AgentBehavior>();
            behavior.name = "Empty AgentBehavior";
            return behavior;
        }

        /// <summary>
        /// Fires when an action is done
        /// </summary>
        public System.Action<ActionData> OnThinkDone;

        /// <summary>
        /// The list of action sets in the asset 
        /// </summary>
        [SerializeField, HideInInspector]
        private List<ActionSet> _actionSets;

        /// <summary>
        /// The list of action sets in the asset
        /// </summary>
        public List<ActionSet> ActionSets
        {
            get
            {
                if (_actionSets == null)
                    _actionSets = new List<ActionSet>();
                return _actionSets;
            }
        }

        /// <summary>
        /// The action set selected at the moment in the UI
        /// </summary>
        [HideInInspector]
        public ActionSet SelectedActionSet;

        /// <summary>
        /// The action currently selected
        /// </summary>
        [HideInInspector]
        public ActionBase SelectedAction;

        /// <summary>
        /// Clones the behavior for runtime execution by a Brain component.
        /// </summary>
        /// <returns></returns>
        public AgentBehavior Clone()
        {
            AgentBehavior clone = Instantiate(this);

            for (int i = 0; i < clone.ActionSets.Count; i++)
            {
                clone.ActionSets[i] = clone.ActionSets[i].Clone(clone);
            }

            return clone;
        }

        private void OnEnable()
        {
            if (_actionSets == null)
                _actionSets = new List<ActionSet>();
        }

        internal int AddRuntimeActionSet(ActionSet set, Brain brain)
        {
            if (!Application.isPlaying) return 0;
            if (set == null) return 0;

            for (int i = 0; i < ActionSets.Count; i++)
            {
                if (ActionSets[i].guid == set.guid)
                {
                    Debug.LogWarning(string.Format("ActionSet {0} is already added.", set.Name), brain);
                    return 0;
                }
            }

            var clonedSet = set.Clone(this);
            ActionSets.Add(clonedSet);
            for (int i = 0; i < clonedSet.Actions.Count; i++)
            {
                clonedSet.Actions[i].Initialize(brain);
            }
            return clonedSet.Actions.Count;
        }

        /// <summary>
        /// Removes the runtime copy of <paramref name="set"/> (matched by guid) and returns it, or returns null
        /// and logs a warning when this behavior does not contain the set.
        /// </summary>
        internal ActionSet RemoveRuntimeActionSet(ActionSet set, Brain brain)
        {
            if (!Application.isPlaying) return null;
            if (set == null) return null;

            ActionSet setToRemove = null;
            for (int i = 0; i < ActionSets.Count; i++)
            {
                if (ActionSets[i].guid == set.guid)
                {
                    setToRemove = ActionSets[i];
                    break;
                }
            }
            if (setToRemove == null)
            {
                Debug.LogWarning(string.Format("ActionSet {0} is not in the brain's behavior so it can not be removed.", set.Name), brain);
                return null;
            }
            ActionSets.Remove(setToRemove);
            return setToRemove;
        }

#if UNITY_EDITOR

        public ActionSet AddActionSet(string name)
        {
            ActionSet set = ScriptableObject.CreateInstance<ActionSet>();
            set.Name = name;
            set.name = set.Name;
            set.Behavior = this;
            set.guid = GUID.Generate().ToString();

            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(this, "Agent Behavior (Add Action Set)");

            ActionSets.Add(set);

            if (!Application.isPlaying)
                AssetDatabase.AddObjectToAsset(set, this);

            Undo.RegisterCreatedObjectUndo(set, "Agent Behavior (Add Action Set)");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            AssetDatabase.SaveAssets();
            return set;
        }

        public ActionSet AddCopiedActionSet(ActionSet clone)
        {
            ActionSet set = clone;
            if (ActionSets.Any(_ => _.Name == set.Name))
                set.Name += " 1";
            set.SyncObjectName();
            set.Behavior = this;
            set.guid = GUID.Generate().ToString();
            for (int j = 0; j < set.Actions.Count; j++)
            {
                ActionBase action = set.Actions[j];
                action.guid = GUID.Generate().ToString();
                action.SyncObjectName();
                for (int i = 0; i < action.Considerations.Count; ++i)
                {
                    action.Considerations[i].guid = GUID.Generate().ToString();
                    action.Considerations[i].SyncObjectName();
                }
            }

            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(this, "Agent Behavior (Add Action Set)");

            ActionSets.Add(set);

            if (!Application.isPlaying)
            {
                AssetDatabase.AddObjectToAsset(set, this);
                for (int j = 0; j < set.Actions.Count; j++)
                {
                    // The actions have to become sub-assets too, not only their considerations,
                    // otherwise they only exist in memory and are gone after the next reimport.
                    ActionBase action = set.Actions[j];
                    AssetDatabase.AddObjectToAsset(action, this);
                    for (int i = 0; i < action.Considerations.Count; ++i)
                    {
                        AssetDatabase.AddObjectToAsset(action.Considerations[i], this);
                    }
                }
            }

            Undo.RegisterCreatedObjectUndo(set, "Agent Behavior (Add Action Set)");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            AssetDatabase.SaveAssets();
            return set;
        }

        public ActionBase AddAction(System.Type type, ActionSet set)
        {
            ActionBase action = ScriptableObject.CreateInstance(type) as ActionBase;
            action.Name = type.Name;
            action.name = action.Name;
            action.guid = GUID.Generate().ToString();

            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(set, "Agent Behavior (Add Action)");

            set.Actions.Add(action);

            if (!Application.isPlaying)
                AssetDatabase.AddObjectToAsset(action, this);

            Undo.RegisterCreatedObjectUndo(action, "Agent Behavior (Add Action)");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            AssetDatabase.SaveAssets();
            return action;
        }

        public ActionBase AddCopiedAction(ActionBase clone, ActionSet set)
        {
            ActionBase action = clone;
            action.guid = GUID.Generate().ToString();
            action.SyncObjectName();
            for (int i = 0; i < action.Considerations.Count; ++i)
            {
                action.Considerations[i].guid = GUID.Generate().ToString();
                action.Considerations[i].SyncObjectName();
            }

            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(set, "Agent Behavior (Add Action)");

            set.Actions.Add(action);

            if (!Application.isPlaying)
            {
                AssetDatabase.AddObjectToAsset(action, this);
                for (int i = 0; i < action.Considerations.Count; ++i)
                {
                    AssetDatabase.AddObjectToAsset(action.Considerations[i], this);
                }
            }

            Undo.RegisterCreatedObjectUndo(action, "Agent Behavior (Add Action)");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            AssetDatabase.SaveAssets();
            return action;
        }

        public ConsiderationBase AddConsideration(System.Type type, ActionBase action)
        {
            ConsiderationBase consideration = ScriptableObject.CreateInstance(type) as ConsiderationBase;
            consideration.Name = type.Name;
            consideration.name = consideration.Name;
            consideration.guid = GUID.Generate().ToString();

            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(action, "Agent Behavior (Add Consideration)");

            action.Considerations.Add(consideration);

            if (!Application.isPlaying)
                AssetDatabase.AddObjectToAsset(consideration, this);

            Undo.RegisterCreatedObjectUndo(consideration, "Agent Behavior (Add Consideration)");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            AssetDatabase.SaveAssets();
            return consideration;
        }

        public ConsiderationBase AddCopiedConsideration(ConsiderationBase con, ActionBase action)
        {
            ConsiderationBase consideration = con;
            consideration.guid = GUID.Generate().ToString();
            consideration.SyncObjectName();

            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(action, "Agent Behavior (Add Consideration)");

            action.Considerations.Add(consideration);

            if (!Application.isPlaying)
                AssetDatabase.AddObjectToAsset(consideration, this);

            Undo.RegisterCreatedObjectUndo(consideration, "Agent Behavior (Add Consideration)");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            AssetDatabase.SaveAssets();
            return consideration;
        }

        public void DeleteAsset(ScriptableObject so)
        {
            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(this, "Agent Behavior (Delete Object)");

            if (so is ActionSet)
            {
                var obj = ActionSets.Find(a => a == so);
                foreach (var action in obj.Actions)
                {
                    foreach (var cons in action.Considerations)
                    {
                        Undo.DestroyObjectImmediate(cons);
                    }
                    Undo.DestroyObjectImmediate(action);
                }
                ActionSets.Remove(obj);
                Undo.DestroyObjectImmediate(obj);
            }
            else if (so is ActionBase)
            {
                var obj = (so as ActionBase);
                foreach (var cons in obj.Considerations)
                {
                    Undo.DestroyObjectImmediate(cons);
                }
                var set = GetActionSet(obj);
                set.Actions.Remove(obj);
                Undo.DestroyObjectImmediate(obj);
            }
            else if (so is ConsiderationBase)
            {
                var obj = so as ConsiderationBase;
                var action = GetAction(obj);
                action.Considerations.Remove(obj);
                Undo.DestroyObjectImmediate(obj);
            }
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Get the action for a consideration
        /// </summary>
        /// <param name="consideration"></param>
        /// <returns></returns>
        public ActionBase GetAction(ConsiderationBase consideration)
        {
            foreach (var set in _actionSets)
            {
                foreach (var act in set.Actions)
                {
                    foreach (var cons in act.Considerations)
                    {
                        if (cons == consideration)
                            return (act);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get an action's action set.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public ActionSet GetActionSet(ActionBase action)
        {
            foreach (var set in _actionSets)
            {
                foreach (var act in set.Actions)
                {
                    if (act == action)
                        return set;
                }
            }
            return null;
        }

        /// <summary>
        /// Deletes the null values which are created due to renames/deleted classes/...
        /// </summary>
        public void DeleteNulls()
        {
            _actionSets.RemoveAll(a => a == null);
            foreach (var set in _actionSets)
            {
                set.Actions.RemoveAll(a => a == null);
            }
            foreach (var set in _actionSets)
            {
                foreach (var action in set.Actions)
                {
                    action.Considerations.RemoveAll(c => c == null);
                }
            }
        }
#endif
    }
}
