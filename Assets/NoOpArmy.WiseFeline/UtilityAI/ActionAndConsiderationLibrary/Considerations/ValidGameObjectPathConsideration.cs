using NoOpArmy.WiseFeline;
using NoOpArmy.WiseFeline.BlackBoards;
using UnityEngine;
using UnityEngine.AI;

namespace NoOpArmy.UtilityAI.Considerations
{

    /// <summary>
    /// This consideration returns 1 if a valid path from the agent's NavmeshAgent to the position of the GameObject stored in the key is present, otherwise returns 0
    /// The NeedTarget causes the GameObject to be read from the target but always a path from the owning agent is calculated
    /// It also returns 0 if the key holds no GameObject or the agent has no NavMeshAgent.
    /// </summary>
    public class ValidGameObjectPathConsideration : ConsiderationBase
    {
        /// <summary>
        /// The GameObject key to read
        /// </summary>
        [Tooltip("The GameObject key to read")]
        public string vector3KeyName;

        /// <summary>
        /// The path calculated in the last evaluation
        /// </summary>
        public NavMeshPath navMeshPath;

        private NavMeshAgent agent;
        private bool warnedAboutMissingAgent;

        protected override void OnInitialized()
        {
            agent = Brain.GetComponent<NavMeshAgent>();
            navMeshPath = new NavMeshPath();
        }

        private bool HasAgent()
        {
            if (agent != null)
                return true;
            if (!warnedAboutMissingAgent)
            {
                warnedAboutMissingAgent = true;
                Debug.LogWarning($"{Name}: the agent {Brain.gameObject.name} has no NavMeshAgent component, so this consideration always returns 0.");
            }
            return false;
        }

        protected override float GetValue(Component target)
        {
            var blackboard = target.GetComponent<BlackBoard>();
            if (blackboard != null)
            {
                if (!blackboard.HasGameObject(vector3KeyName))
                    return 0;

                var value = blackboard.GetGameObject(vector3KeyName);
                if (value == null || !HasAgent())
                    return 0;
                agent.CalculatePath(value.transform.position, navMeshPath);
                if (navMeshPath.status == NavMeshPathStatus.PathComplete)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
    }
}