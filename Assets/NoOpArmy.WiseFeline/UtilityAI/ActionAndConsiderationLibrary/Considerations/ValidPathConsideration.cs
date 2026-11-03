using NoOpArmy.WiseFeline;
using NoOpArmy.WiseFeline.BlackBoards;
using UnityEngine;
using UnityEngine.AI;

namespace NoOpArmy.UtilityAI.Considerations
{

    /// <summary>
    /// This consideration returns 1 if a valid path from the agent's NavmeshAgent to the position stored in the Vector3 key is present, otherwise returns 0
    /// The NeedTarget causes the Vector3 to be read from the target but always a path from the owning agent is calculated
    /// It also returns 0 if the agent has no NavMeshAgent.
    /// </summary>
    public class ValidPathConsideration : ConsiderationBase
    {
        /// <summary>
        /// The Vector3 key to read
        /// </summary>
        [Tooltip("The Vector3 key to read")]
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
                if (!blackboard.HasVector3(vector3KeyName))
                    return 0;
                var value = blackboard.GetVector3(vector3KeyName);
                if (!HasAgent())
                    return 0;
                agent.CalculatePath(value, navMeshPath);
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