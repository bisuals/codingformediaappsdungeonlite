using NoOpArmy.WiseFeline;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.UtilityAI.Actions
{
    /// <summary>
    /// This action creates gameobjects, can be used for shooting, providing loot and other similar things
    /// </summary>
    public class CreateGameObjectAction : BlackboardActionBase
    {
        private float startTime;

        /// <summary>
        /// The value of objectCountToCreate when the action was initialized.
        /// objectCountToCreate is used as the runtime countdown of the objects which are left to create,
        /// so it is restored from this field every time the action starts to allow the action to run more than once.
        /// </summary>
        private int initialObjectCountToCreate;

        /// <summary>
        /// Delay to create an object
        /// </summary>
        [Tooltip("Delay to create an object")]
        public float creationDelay;

        /// <summary>
        /// The GameObject to create
        /// </summary>
        [Tooltip("The GameObject to create")]
        public GameObject objectToCreate;

        /// <summary>
        /// How many objects to create
        /// </summary>
        [Tooltip("How many objects to create")]
        public int objectCountToCreate = 1;

        /// <summary>
        /// Should we create all objects at the same time or once after each delay
        /// </summary>
        [Tooltip("Should we create all objects at the same time or once after each delay")]
        public bool createAtTheSameTime;

        /// <summary>
        /// Is the position specified from origin or the position of the executing game object.
        /// </summary>
        [Tooltip("Is the position specified from origin or the position of the executing game object.")]
        public bool isPositionRelative;

        /// <summary>
        /// The position offset to create the object
        /// </summary>
        [Tooltip("The position offset to create the object")]
        public Vector3 creationPosition;

        /// <summary>
        /// Should the object adapt the brain's object rotation
        /// </summary>
        [Tooltip("Should the object adapt the brain's object rotation")]
        public bool shouldAdaptBrainRotation = false;

        protected override void UpdateTargets()
        {
            base.UpdateTargets();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            initialObjectCountToCreate = objectCountToCreate;
        }

        protected override void OnStart()
        {
            base.OnStart();
            startTime = Time.time;
            //objectCountToCreate is decremented while the action runs, so the countdown is restored here
            //to make the action behave the same way every time the brain selects it.
            objectCountToCreate = initialObjectCountToCreate;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (Time.time - startTime >= creationDelay && objectCountToCreate != 0)
            {
                if (!createAtTheSameTime)
                {
                    Create();
                    objectCountToCreate--;
                    startTime = Time.time;
                }
                else
                {
                    for (int i = 0; i < objectCountToCreate; i++)
                    {
                        Create();
                    }
                    startTime = 0;
                    objectCountToCreate = 0;
                }
                if (objectCountToCreate == 0)
                    ActionSucceeded();
            }
        }

        private void Create()
        {
            Vector3 pos;
            if (isPositionRelative)
            {
                pos = Brain.transform.transform.TransformPoint(creationPosition);
            }
            else
            {
                pos = creationPosition;
            }
            GameObject.Instantiate(objectToCreate, pos, (!shouldAdaptBrainRotation) ? Quaternion.identity : Brain.transform.rotation);
        }
    }
}