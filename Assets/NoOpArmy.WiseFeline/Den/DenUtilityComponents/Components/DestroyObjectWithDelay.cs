using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    public class DestroyObjectWithDelay : MonoBehaviour
    {
        public float delayInSeconds = 3;

        void Start()
        {
            Destroy(this.gameObject, delayInSeconds);
        }
    }
}
