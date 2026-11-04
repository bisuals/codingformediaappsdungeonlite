using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.WiseFeline.Sample
{
    public class SimpleAgentCombat : MonoBehaviour
    {
        public float delayBetweenAttacks = 3;
        private float lastAttack;
        private Attack attack;

        private void Awake()
        {
            attack = GetComponent<Attack>();
        }

        void Update()
        {
            if (Time.time - lastAttack > delayBetweenAttacks)
            {
                if (Random.value > 0.95f)
                {
                    lastAttack = Time.time;
                    attack.SetAttackDelayColor(true, Color.gray);
                    attack.MeleeAttackWithDelay(Vector3.forward, 1, 10, 1, null, true);
                }
            }
        }
    }
}
