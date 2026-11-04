using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// Any component which wants to receive damage can implement this interface
    /// </summary>
    public interface IDamagable
    {
        /// <summary>
        /// Applies damage to the GameObject
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="damager"></param>
        void ApplyDamage(float amount, GameObject damager);

        /// <summary>
        /// Heals the GameObject
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="healer"></param>
        void IncreaseHealth(float amount, GameObject healer);
    }
}
