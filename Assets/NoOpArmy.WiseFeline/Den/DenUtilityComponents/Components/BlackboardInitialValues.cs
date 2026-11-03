using System.Collections.Generic;
using NoOpArmy.WiseFeline.BlackBoards;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// Writes a list of values which you author in the inspector into the BlackBoard component which is on the same GameObject.
    /// Blackboard keys start at their default values which are 0, false, an empty string or null, so any key which needs a
    /// starting value has to be written before the agent starts thinking and this component does that without any code.
    /// It is also the only code-free way to put a reference to an object which lives in a scene into a blackboard, because
    /// assets like behaviors and their changers cannot reference scene objects while a component which lives in the scene can.
    /// So patrol point holders, camps, the player and other scene objects can simply be dragged into the list below.
    /// The values are applied in Start because the BlackBoard component defines its keys in Awake.
    /// The negative execution order matters: actions like MoveAndPatrol read their GameObject key once,
    /// when the Brain initializes them in its own Start, so the seeding has to be finished before any
    /// Start of a default-ordered component runs. Without it the order of two Starts on the same
    /// GameObject is unspecified and a patrol route would silently come up empty.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(BlackBoard))]
    public class BlackboardInitialValues : MonoBehaviour
    {
        /// <summary>
        /// The values which are written to the blackboard of this GameObject when the scene starts.
        /// Every entry has a key type, a key name and the value to write and the key should already be defined with the same
        /// type in the blackboard definition which the BlackBoard component uses, otherwise applying the value throws.
        /// Every entry is always a set. The add, deltaTime and min and max settings of the entry type are ignored here, which
        /// is why the inspector does not show them for this list.
        /// </summary>
        [Tooltip("The values which are written to the blackboard of this GameObject when the scene starts. Every key should already be defined with the same type in the blackboard definition. Each entry replaces the key's value.")]
        [BlackBoardSetOnly]
        public List<BlackBoardChange> values = new();

        private void Start()
        {
            var blackBoard = GetComponent<BlackBoard>();
            if (values == null)
                return;

            for (int i = 0; i < values.Count; i++)
            {
                values[i]?.SetOnBlackboard(blackBoard);
            }
        }
    }
}
