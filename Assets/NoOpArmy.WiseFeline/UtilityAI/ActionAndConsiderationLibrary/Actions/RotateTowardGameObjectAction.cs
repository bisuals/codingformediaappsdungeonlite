using UnityEngine;
using NoOpArmy.WiseFeline;

namespace NoOpArmy.UtilityAI.Actions
{

    /// <summary>
    /// Rotates the agent toward the position of a GameObject set on a blackboard key.
    /// </summary>
    public class RotateTowardGameObjectAction : BlackboardActionBase
    {
        /// <summary>
        /// Name of the GameObject key in the blackboard
        /// </summary>
        [Tooltip("Name of the GameObject key in the blackboard")]
        public string gameObjectKeyName;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void OnStart()
        {
            base.OnStart();
            Brain.transform.LookAt(blackBoard.GetGameObject(gameObjectKeyName).transform.position);
            

        }

        protected override void UpdateTargets()
        {

        }
    }
}