using UnityEngine;
using NoOpArmy.WiseFeline;
using NoOpArmy.WiseFeline.BlackBoards;
using System.Collections.Generic;
using System;
using System.Collections;

namespace NoOpArmy.UtilityAI.Actions
{


    /// <summary>
    /// Patrols the agent on the way points provided as children of a GameObject which is set on a blackboard key
    /// </summary>
    public class MoveAndPatrol : BlackboardActionBase
    {
        private IAIMovement agentMovement;

        //List of the Children of GameObject that is set on the black board
        private Transform[] childTransforms;
        private int currentTargetIndex = 0;


        /// <summary>
        /// Name of the key in the blackboard of the GameObject which contains all patrol points as children
        /// </summary>
        [Tooltip("Name of the key in the blackboard of the GameObject which contains all patrol points as children")]
        public string gameObjectKeyName;

        /// <summary>
        /// Should the action finishes after going through the points once?
        /// </summary>
        [Tooltip("Should the action finishes after going through the points once")]
        public bool FinishAfterOneCycle = false;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            agentMovement = Brain.GetComponent<IAIMovement>();
            blackBoard = Brain.GetComponent<BlackBoard>();
            ReadPatrolPoints();
        }

        /// <summary>
        /// Reads the patrol points from the children of the GameObject stored in gameObjectKeyName.
        /// Leaves childTransforms null if the key has no GameObject yet.
        /// </summary>
        private void ReadPatrolPoints()
        {
            if (blackBoard == null || !blackBoard.HasGameObject(gameObjectKeyName))
                return;

            GameObject parentGameObject = blackBoard.GetGameObject(gameObjectKeyName);
            //Add Game Object Child Transforms to the list that we already created
            if (parentGameObject != null)
            {
                childTransforms = new Transform[parentGameObject.transform.childCount];
                for (int i = 0; i < parentGameObject.transform.childCount; i++)
                {
                    childTransforms[i] = parentGameObject.transform.GetChild(i);
                }
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            //The key might have been set after the action was initialized, so try reading it again
            if (childTransforms == null)
                ReadPatrolPoints();

            if (childTransforms == null)
            {
                Debug.LogWarning($"{Name}: the blackboard key {gameObjectKeyName} does not contain a GameObject with patrol points, so the action fails.");
                ActionFailed();
                return;
            }

            currentTargetIndex = 0;

            if (childTransforms != null && childTransforms.Length > 0)
            {
                Brain.StartCoroutine(GoToNextDestination());
            }
            else
            {
                ActionFailed();
            }
        }

        private IEnumerator GoToNextDestination()
        {
            // Wait for the next frame. WaitForEndOfFrame never resumes in batchmode, which would freeze patrols on a dedicated server.
            yield return null;

            var res = agentMovement.MoveToPosition(childTransforms[currentTargetIndex].position, ReachedToDestination);
            if (!res)
                ActionFailed();
        }

        private void ReachedToDestination()
        {
            if (childTransforms.Length == 0)
            {
                ActionSucceeded();
                return;
            }

            if (++currentTargetIndex >= childTransforms.Length)
            {
                if (FinishAfterOneCycle)
                    ActionSucceeded();
                else
                    currentTargetIndex = 0;
            }

            Brain.StartCoroutine(GoToNextDestination());
        }

        protected override void UpdateTargets()
        {

        }
    }
}