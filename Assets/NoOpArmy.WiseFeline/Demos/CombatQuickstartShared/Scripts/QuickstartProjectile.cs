using UnityEngine;

namespace NoOpArmy.WiseFeline.CombatQuickstart
{
    /// <summary>
    /// DEMO CODE, NOT AI CODE. The tracer both sides shoot. It flies straight forward, dies after a few
    /// seconds and hands damage to the first <see cref="QuickstartHealth"/> of the other side that it
    /// touches.
    /// <para>
    /// The soldier's copy is spawned by the shipped <c>CreateGameObjectAction</c>, which instantiates the
    /// prefab at an offset from the agent and with the agent's rotation, so nothing here has to know
    /// anything about who fired it beyond <see cref="team"/>. The player's copy is spawned by
    /// <see cref="QuickstartPlayerController"/> in exactly the same way.
    /// </para>
    /// <para>
    /// <b>Team filtering is what makes the muzzle spawn safe.</b> Both shooters spawn their projectile
    /// less than a metre in front of their own collider, so the first thing a projectile can overlap is
    /// the object that fired it. A projectile that meets its own side is ignored completely - no damage
    /// and no despawn - so a shooter can never hurt itself and never eats its own shot.
    /// </para>
    /// <para>
    /// <b>Layer.</b> Keep this prefab off the <c>Obstacles</c> and <c>PlayerLayer</c> layers. Those two are
    /// the mask of the soldier's <c>LineOfSightConsideration</c>, and a tracer crossing the sight line
    /// would be raycast against and would make the soldier lose sight of the player for a frame at a time
    /// while it is being shot at. <c>Default</c> or the project's <c>Projectile</c> layer are both fine.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class QuickstartProjectile : MonoBehaviour
    {
        /// <summary>
        /// Who fired this. Only a <see cref="QuickstartHealth"/> on the other side is damaged.
        /// </summary>
        [Tooltip("Who fired this. Only health on the other side is damaged.")]
        public QuickstartTeam team = QuickstartTeam.Soldier;

        /// <summary>
        /// Metres per second along the object's own forward axis.
        /// </summary>
        [Tooltip("Metres per second along the object's own forward axis.")]
        public float speed = 18f;

        /// <summary>
        /// Health removed from the first valid thing this hits.
        /// </summary>
        [Tooltip("Health removed from the first valid thing this hits.")]
        public float damage = 10f;

        /// <summary>
        /// Seconds before the tracer despawns on its own, so shots that hit nothing cannot pile up.
        /// </summary>
        [Tooltip("Seconds before the tracer despawns on its own.")]
        public float lifeTime = 3f;

        /// <summary>
        /// Optional effect spawned where the tracer hits something, facing out of the surface it hit. It
        /// has to clean up after itself: the demo's impact prefabs are particle systems whose stop action
        /// is Destroy.
        /// </summary>
        [Tooltip("Optional effect spawned where the tracer hits something. It must destroy itself when it is done.")]
        public GameObject impactEffect;

        private Vector3 previousPosition;

        private void Start()
        {
            previousPosition = transform.position;
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            // A kinematic body moved by its transform: no forces, no gravity, no drift, and the trigger
            // overlaps are still evaluated by the physics step.
            previousPosition = transform.position;
            transform.position += speed * Time.deltaTime * transform.forward;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Two tracers crossing is not an event.
            if (other.GetComponentInParent<QuickstartProjectile>() != null)
                return;

            var health = other.GetComponentInParent<QuickstartHealth>();
            if (health != null)
            {
                // Own side, including the shooter itself at the muzzle: pass straight through.
                if (health.team == team)
                    return;

                health.TakeDamage(damage);
            }

            // Anything else with a collider is scenery, and stopping there is what makes cover mean
            // something for the player as well as for the soldier's line of sight.
            SpawnImpact(other);
            Destroy(gameObject);
        }

        /// <summary>
        /// The trigger only says that the two volumes overlap, and by then the tracer can be most of a
        /// frame's travel inside what it hit. A ray cast against that one collider, starting from where
        /// the tracer was on the previous frame, finds the surface it actually crossed.
        /// </summary>
        private void SpawnImpact(Collider other)
        {
            if (impactEffect == null)
                return;

            var direction = transform.forward;
            var from = previousPosition - 0.5f * direction;
            float reach = Vector3.Distance(from, transform.position) + 1f;

            var point = transform.position;
            var normal = -direction;
            if (other.Raycast(new Ray(from, direction), out var hit, reach))
            {
                point = hit.point;
                normal = hit.normal;
            }

            Instantiate(impactEffect, point, Quaternion.LookRotation(normal));
        }
    }
}
