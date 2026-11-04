using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.WiseFeline.Sample
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        public Transform player;
        public Vector3 offset;
        public bool shouldLookAtTarget;

        private void LateUpdate()
        {
            if(player != null)
            {
                transform.position = player.position + offset;
                if (shouldLookAtTarget)
                {
                    transform.transform.LookAt(player);
                }
            }
        }
    }
}
