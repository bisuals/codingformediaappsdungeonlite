using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.WiseFeline.BlackBoards
{
    /// <summary>
    /// This class defines the keys which can exist in a blackboard and their data types.
    /// Create instances of this using the unity editor from the create menu and assign them to the blackboard
    /// component to use.
    /// The blackboard component then will only accept these keys with these data types.
    /// </summary>
    [CreateAssetMenu(menuName = "NoOpArmy/Wise Feline/BlackBoard Definition")]
    public class BlackBoardDefinition : ScriptableObject
    {
        /// <summary>
        /// List of key definitions
        /// </summary>
        public List<KeyDefinition> definitions;
    }

    /// <summary>
    /// This class represents blackboard key definitions.
    /// </summary>
    [System.Serializable]
    public class KeyDefinition
    {
        /// <summary>
        /// The possible key types for the blackboard
        /// </summary>
        public enum KeyType : byte
        {
            /// <summary>
            /// A UnityEngine.Vector3
            /// </summary>
            Vector3, 
            /// <summary>
            /// A System.Object for any type you need and we did not support
            /// </summary>
            Object, 
            /// <summary>
            /// A floating point number
            /// </summary>
            Float, 
            /// <summary>
            /// A 32 bit integer
            /// </summary>
            Int, 
            /// <summary>
            /// A boolean value
            /// </summary>
            Bool, 
            /// <summary>
            /// A UnityEngine.Object for any unity object which we did not support and you need
            /// </summary>
            UnityObject, 
            /// <summary>
            /// A game object 
            /// </summary>
            GameObject,
            /// <summary>
            /// A string
            /// </summary>
            String
        }

        /// <summary>
        /// Unique name of the key in the blackboard
        /// </summary>
        public string keyName;

        /// <summary>
        /// Type of the key
        /// </summary>
        public KeyType keyType;
    }
}
