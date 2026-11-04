using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace NoOpArmy.WiseFeline
{


    /// <summary>
    /// This component allows you to move the character to a destination. This component implements the IAIMovement interface so generic actions can use it.
    /// The component uses the CharacterController component to move and does not use path finding and a NavmeshAgent
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControllerBasedAIMovement : MonoBehaviour, IAIMovement
    {
        /// <summary>
        /// Movement speed of the character used by the CharacterController.SimpleMove() function
        /// </summary>
        [Tooltip("Movement speed of the character used by the CharacterController.SimpleMove() function")]
        public float speed = 5;

        /// <summary>
        /// The distance to destination which means we arrived
        /// </summary>
        [Tooltip("The distance to destination which means we arrived")]
        public float arrivalDistance = 0.5f;

        public bool CalculateOnXZPlane = false;
        public Vector3 Destination { get; private set; }

        public string animatorSpeedParameter;

        private CharacterController characterController;
        private Action reachCallback;

        private bool startMovement = false;

        private Animator animator;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// Starts moving toward the position. If the character is already within arrivalDistance of it, the move still succeeds
        /// and the callback is called on the next Update, the same way NavMeshBasedAIMovement reports arrival.
        /// </summary>
        /// <param name="position">The destination</param>
        /// <param name="callback">Called once when the character reaches the destination</param>
        /// <returns>Always true</returns>
        public bool MoveToPosition(Vector3 position, Action callback)
        {
            Destination = position;
            reachCallback = callback;
            startMovement = true;
            return true;
        }

        /// <summary>
        /// Starts moving toward the target's current position. If the character is already within arrivalDistance of it, the move still succeeds
        /// and the callback is called on the next Update.
        /// </summary>
        /// <param name="target">The object to move to</param>
        /// <param name="callback">Called once when the character reaches the destination</param>
        /// <returns>False if target is null, true otherwise</returns>
        public bool MoveToPosition(GameObject target, Action callback)
        {
            if (target == null)
                return false;
            return MoveToPosition(target.transform.position, callback);
        }

        public void StopMoving()
        {
            startMovement = false;
        }

        private void Update()
        {
            if (startMovement)
            {
                Vector3 direction = (Destination - transform.position).normalized;
                characterController.SimpleMove(direction * speed);

                if (animator != null && !string.IsNullOrEmpty(animatorSpeedParameter))
                {
                    animator.SetFloat(animatorSpeedParameter, characterController.velocity.magnitude);
                }

                if (IsReached())
                {
                    if (reachCallback != null)
                    {
                        reachCallback.Invoke();
                        reachCallback = null;
                    }
                    StopMoving();
                }
            }
        }

        private bool IsReached()
        {
            if (!CalculateOnXZPlane)
                return Vector3.Distance(transform.position, Destination) <= arrivalDistance;
            else
            {
                var destPost = Destination;
                destPost.y = transform.position.y;
                return Vector3.Distance(transform.position, destPost) <= arrivalDistance;
            }
        }
    }
}