using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NoOpArmy.WiseFeline
{
    /// <summary>
    /// Attach thiscomponent to any GameObject which needs to attack others
    /// </summary>
    public class Attack : MonoBehaviour
    {
        /// <summary>
        /// Draws lines for spehre checks
        /// </summary>
        [Tooltip("Draws lines for spehre checks")]
        public bool drawDebugLines;
        /// <summary>
        /// We allocate an array of this size and no colliders after this count will be allocated but this is allocated only once
        /// </summary>
        [Tooltip("We allocate an array of this size and no colliders after this count will be allocated but this is allocated only once")]
        public int maxAttackCountInSingleCast = 10;



        /// <summary>
        /// The array of objects which the OverlapSphere function uses.
        /// </summary>
        private Collider[] results;

        private bool shouldChangeColorOnAttackDelay = false;
        private Color attackDelayColor;

        /// <summary>
        /// A delayed attack which has been started and has not dealt its damage yet
        /// </summary>
        private class PendingAttack
        {
            public Coroutine coroutine;
        }

        /// <summary>
        /// Delayed attacks in progress. More than one can be in progress if a new attack is started without checking CanAttack() first.
        /// </summary>
        private readonly List<PendingAttack> pendingAttacks = new List<PendingAttack>();

        private Renderer delayColorRenderer;
        private Color colorBeforeDelay;
        private bool isShowingDelayColor;

        /// <summary>
        /// Sets the fact that we should change color on attack delay and the color used
        /// </summary>
        /// <param name="shouldChangeColor"></param>
        /// <param name="color"></param>
        public void SetAttackDelayColor(bool shouldChangeColor, Color color)
        {
            this.shouldChangeColorOnAttackDelay = shouldChangeColor;
            this.attackDelayColor = color;
        }

        private void Awake()
        {
            results = new Collider[maxAttackCountInSingleCast];
        }

        /// <summary>
        /// Executes a melee attack with delay
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="radius"></param>
        /// <param name="damageAmount"></param>
        /// <param name="castDelay"></param>
        /// <param name="Done"></param>
        /// <param name="applyToAllHits"></param>
        /// <returns></returns>
        public Coroutine MeleeAttackWithDelay(Vector3 offset, float radius, float damageAmount, float castDelay, Action Done = null, bool applyToAllHits = false)
        {
            var pending = new PendingAttack();
            pendingAttacks.Add(pending);
            pending.coroutine = StartCoroutine(MeleeAttack(offset, radius, damageAmount, castDelay, Done, applyToAllHits, pending));
            return pending.coroutine;
        }

        /// <summary>
        /// executes a range attack with delay
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="damageAmount"></param>
        /// <param name="castDelay"></param>
        /// <param name="Done"></param>
        /// <param name="applyToAllHits"></param>
        /// <returns></returns>
        public Coroutine RangeAttackWithDelay(Vector3 center, float radius, float damageAmount, float castDelay, Action Done = null, bool applyToAllHits = false)
        {
            var pending = new PendingAttack();
            pendingAttacks.Add(pending);
            pending.coroutine = StartCoroutine(RangeAttack(center, radius, damageAmount, castDelay, Done, applyToAllHits, pending));
            return pending.coroutine;
        }

        /// <summary>
        /// A melee attack with casting delay
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="radius"></param>
        /// <param name="damageAmount"></param>
        /// <param name="castDelay"></param>
        /// <param name="Done"></param>
        /// <param name="applyToAllHits"></param>
        /// <returns></returns>
        private IEnumerator MeleeAttack(Vector3 offset, float radius, float damageAmount, float castDelay, Action Done, bool applyToAllHits, PendingAttack pending)
        {
            ShowDelayColor();
            yield return new WaitForSeconds(castDelay);
            FinishPendingAttack(pending);
            MeleeAttack(offset, radius, damageAmount, applyToAllHits);
            Done?.Invoke();
        }

        /// <summary>
        /// Executes a range attack in a place with a radius
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="damageAmount"></param>
        /// <param name="castDelay"></param>
        /// <param name="Done"></param>
        /// <param name="Done"></param>
        /// <param name="applyToAllHits"></param>
        /// <returns></returns>
        private IEnumerator RangeAttack(Vector3 center, float radius, float damageAmount, float castDelay, Action Done, bool applyToAllHits, PendingAttack pending)
        {
            ShowDelayColor();
            yield return new WaitForSeconds(castDelay);
            FinishPendingAttack(pending);
            RangeAttack(center, radius, damageAmount, applyToAllHits);
            Done?.Invoke();
        }

        /// <summary>
        /// Returns true if no delayed attack started by MeleeAttackWithDelay or RangeAttackWithDelay is in progress
        /// </summary>
        /// <returns></returns>
        public bool CanAttack()
        {
            return pendingAttacks.Count == 0;
        }

        private void ShowDelayColor()
        {
            if (!shouldChangeColorOnAttackDelay)
                return;
            if (delayColorRenderer == null && !TryGetComponent(out delayColorRenderer))
                return;
            if (!isShowingDelayColor)
                colorBeforeDelay = delayColorRenderer.material.color;
            delayColorRenderer.material.color = attackDelayColor;
            isShowingDelayColor = true;
        }

        private void RestoreColorAfterDelay()
        {
            if (!isShowingDelayColor)
                return;
            isShowingDelayColor = false;
            if (delayColorRenderer != null)
                delayColorRenderer.material.color = colorBeforeDelay;
        }

        /// <summary>
        /// Removes a delayed attack from the pending list when its delay is over. The delay color stays while another delayed attack is still waiting.
        /// </summary>
        private void FinishPendingAttack(PendingAttack pending)
        {
            pendingAttacks.Remove(pending);
            if (pendingAttacks.Count == 0)
                RestoreColorAfterDelay();
        }

        /// <summary>
        /// Cancels every delayed attack in progress, if any, so none of them deals its damage. The color which was shown during the delay is reset and CanAttack() returns true again.
        /// </summary>
        public void StopAttack()
        {
            for (int i = 0; i < pendingAttacks.Count; ++i)
            {
                if (pendingAttacks[i].coroutine != null)
                    StopCoroutine(pendingAttacks[i].coroutine);
            }
            pendingAttacks.Clear();
            RestoreColorAfterDelay();
        }

        /// <summary>
        /// Executes a melee attack
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="radius"></param>
        /// <param name="damageAmount"></param>
        /// <param name="applyToAllHits"></param>
        public void MeleeAttack(Vector3 offset, float radius, float damageAmount, bool applyToAllHits = false)
        {
            Vector3 position = transform.TransformPoint(offset);
            int count = CheckOverlapsWithSphere(radius, position);
            bool damaged = CalculateDamage(damageAmount, count, applyToAllHits);
            if (drawDebugLines)
            {
                DrawDebugLines(radius, position, damaged, 1);
            }
        }

        private void DrawDebugLines(float radius, Vector3 position, bool damaged, float duration = 0.5f)
        {

            Color color = damaged ? Color.blue : Color.green;
            Debug.DrawLine(position, position + Vector3.right * radius, color, duration);
            Debug.DrawLine(position, position + Vector3.left * radius, color, duration);
            Debug.DrawLine(position, position + Vector3.up * radius, color, duration);
            Debug.DrawLine(position, position + Vector3.down * radius, color, duration);
            Debug.DrawLine(position, position + Vector3.forward * radius, color, duration);
            Debug.DrawLine(position, position + Vector3.back * radius, color, duration);
            Debug.DrawLine(position, position + Vector3.back * radius, color, duration);
        }

        /// <summary>
        /// Checks the sphere to see what is inside it to apply damage
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private int CheckOverlapsWithSphere(float radius, Vector3 position)
        {
            int count = Physics.OverlapSphereNonAlloc(position, radius, results);
            return count;
        }

        /// <summary>
        /// Applies damages to colliders in the sphere checked.
        /// </summary>
        /// <param name="damageAmount"></param>
        /// <param name="count"></param>
        /// <param name="applyToAllHits"></param>
        /// <returns>If anything got damaged</returns>
        private bool CalculateDamage(float damageAmount, int count, bool applyToAllHits = false)
        {
            bool processed = false;
            if (count > 0)
            {
                for (int i = 0; i < count; ++i)
                {
                    if (this.gameObject != results[i].gameObject && results[i].TryGetComponent<IDamagable>(out IDamagable damagable))
                    {
                        damagable.ApplyDamage(damageAmount, this.gameObject);
                        processed = true;
                        if (!applyToAllHits)
                            return true;
                    }
                }
            }
            return processed;
        }

        /// <summary>
        /// Executes a range attack
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="damageAmount"></param>
        /// <param name="applyToAllHits"></param>
        public void RangeAttack(Vector3 center, float radius, float damageAmount, bool applyToAllHits = false)
        {
            int count = CheckOverlapsWithSphere(radius, center);
            CalculateDamage(damageAmount, count, applyToAllHits);
        }

        /// <summary>
        /// Shoots a projecitle
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="offset"></param>
        /// <param name="Done"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public IEnumerator ShootProjectile(GameObject projectile, Vector3 offset, Action Done, float delay)
        {
            yield return new WaitForSeconds(delay);
            ShootProjectile(projectile, offset);
            Done?.Invoke();
        }

        /// <summary>
        /// Shoots a projectile
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="offset"></param>
        public void ShootProjectile(GameObject projectile, Vector3 offset)
        {
            Instantiate(projectile, transform.position + offset, transform.rotation);
        }
    }
}