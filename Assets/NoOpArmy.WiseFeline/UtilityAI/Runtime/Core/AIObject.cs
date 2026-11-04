using System;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// This is the base class of Wise Feline AI assets. 
    /// You never need to use this directly.
    /// </summary>
    public class AIObject : ScriptableObject
    {
        /// <summary>
        /// The unique id of the asset.
        /// </summary>
        [HideInInspector]
        public string guid;

        /// <summary>
        /// The name of the asset
        /// </summary>
        public string Name;

        /// <summary>
        /// Sets the Guid of the object.
        /// We do not ensure the IDs are unique and assume the application developer generates IDs with the guarantees he/she needs.
        /// </summary>
        /// <param name="newId">The ID to assign</param>
        /// <param name="canReplace">Can the ID be replaced if it is alredy set, defaults to false</param>
        /// <remarks>This method should onlybe called once per object</remarks>
        public void SetGuid(string newId,bool canReplace = false)
        {
            if (string.IsNullOrEmpty(guid) || canReplace)
            {
                guid = newId;
            }
            else
            {
                throw new InvalidOperationException("Id of an AI Object cannot be set multiple times");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Copies Name into the object's name so the Project window and the Wise Feline window agree.
        /// Instantiate appends "(Clone)" to the object name, which this also removes.
        /// The Wise Feline window calls this after a rename; it is not an OnValidate on purpose so that
        /// actions and considerations written by users keep their own OnValidate without hiding a base one.
        /// </summary>
        public void SyncObjectName()
        {
            if (!string.IsNullOrEmpty(Name) && name != Name)
                name = Name;
        }
#endif
    }
}
