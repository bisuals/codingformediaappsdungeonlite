using UnityEngine;

namespace NoOpArmy.WiseFeline.CombatQuickstart
{
    /// <summary>
    /// DEMO CODE, NOT AI CODE. Drives the player: move with WASD or the arrow keys, turn to face
    /// the mouse cursor, hold the left mouse button to shoot. The soldier's behaviour asset knows nothing
    /// about this class - it reacts to the player because the behavior reads a player reference or an AI tag, and because
    /// the player's projectiles move a number on the soldier's blackboard.
    /// <para>
    /// Input goes through the old <c>Input</c> class because that is what this project is set to
    /// (<c>activeInputHandler</c> is Input Manager) and what every other demo in the package uses.
    /// </para>
    /// <para>
    /// Movement is a plain transform translation and not a rigidbody force, on purpose. The capsule
    /// collider has to stay on this same GameObject: <c>LineOfSightConsideration</c> compares the
    /// transform its ray hits against the transform of the <c>AITags</c> it was given, and the two only
    /// match when the collider and the tags are on one object. Pushing the object around with physics
    /// would eventually separate the visible position from that collider, so nothing here touches
    /// velocities.
    /// </para>
    /// </summary>
    public class QuickstartPlayerController : MonoBehaviour
    {
        /// <summary>
        /// Metres per second. A little faster than the soldier's 3.5 so that outrunning it is possible.
        /// </summary>
        [Tooltip("Metres per second. A little faster than the soldier so outrunning it is possible.")]
        public float moveSpeed = 6f;

        /// <summary>
        /// Half the width of the arena floor, used to keep the player on the ground plane. The demo's
        /// ground is a 40 by 40 plane centred on the origin, so this is 20 minus a small margin.
        /// </summary>
        [Tooltip("Half the arena width. The player is clamped to this box so it cannot walk off the floor.")]
        public float arenaHalfSize = 19f;

        /// <summary>
        /// The tracer to spawn. Must be the player's variant, whose QuickstartProjectile is on the
        /// player's team, otherwise the player would shoot itself the moment it fires.
        /// </summary>
        [Tooltip("The player's projectile prefab. Its team must be Player.")]
        public GameObject projectilePrefab;

        /// <summary>
        /// Where shots come from. Its forward is the shot direction, so it has to sit in front of the
        /// player's own collider. Falls back to this transform when it is not set.
        /// </summary>
        [Tooltip("Where shots come from. Its forward is the shot direction.")]
        public Transform firePoint;

        /// <summary>
        /// Seconds between shots while the mouse button is held.
        /// </summary>
        [Tooltip("Seconds between shots while the fire button is held.")]
        public float fireCooldown = 0.25f;

        /// <summary>
        /// Prints the controls in the corner, so that opening the scene and pressing Play is enough to
        /// know that the soldier can be fought back against.
        /// </summary>
        [Tooltip("Print the controls in the corner of the screen.")]
        public bool showControlsHint = true;

        /// <summary>
        /// Optional. Receives the movement in the player's own frame as the <c>MoveX</c> and <c>MoveZ</c>
        /// floats, so the legs can step sideways and backwards while the body keeps facing the cursor,
        /// and the <c>Shoot</c> trigger once per shot. Leave it empty and nothing is animated.
        /// </summary>
        [Tooltip("Optional. Receives MoveX and MoveZ in the player's own frame, and the Shoot trigger on every shot.")]
        public Animator animator;

        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
        private static readonly int ShootHash = Animator.StringToHash("Shoot");

        private Camera aimCamera;
        private float nextFireTime;
        private GUIStyle hintStyle;

        private void Awake()
        {
            aimCamera = Camera.main;
        }

        private void Update()
        {
            Move();
            AimAtMouse();
            Fire();
        }

        /// <summary>
        /// World relative movement, which is the right choice here because the demo camera is fixed and
        /// looking down at the arena: up on the keyboard is always up on the screen.
        /// </summary>
        private void Move()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            var position = transform.position + moveSpeed * Time.deltaTime * input;
            position.x = Mathf.Clamp(position.x, -arenaHalfSize, arenaHalfSize);
            position.z = Mathf.Clamp(position.z, -arenaHalfSize, arenaHalfSize);
            transform.position = position;

            if (animator != null)
            {
                // Damped, so tapping a key eases the legs in and out instead of snapping between poses.
                var local = transform.InverseTransformDirection(input);
                animator.SetFloat(MoveXHash, local.x, 0.08f, Time.deltaTime);
                animator.SetFloat(MoveZHash, local.z, 0.08f, Time.deltaTime);
            }
        }

        /// <summary>
        /// Turns towards where the cursor crosses the horizontal plane the player stands on. A maths plane
        /// rather than a raycast against the floor, so aiming still works over a wall, over the camp patch
        /// and past the edge of the ground.
        /// </summary>
        private void AimAtMouse()
        {
            if (aimCamera == null)
            {
                aimCamera = Camera.main;
                if (aimCamera == null)
                    return;
            }

            var ground = new Plane(Vector3.up, transform.position);
            var ray = aimCamera.ScreenPointToRay(Input.mousePosition);

            // False when the cursor is above the horizon, in which case there is nothing to aim at.
            if (!ground.Raycast(ray, out float distance))
                return;

            var toCursor = ray.GetPoint(distance) - transform.position;
            toCursor.y = 0f;
            if (toCursor.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toCursor);
        }

        private void Fire()
        {
            if (projectilePrefab == null || Time.time < nextFireTime || !Input.GetMouseButton(0))
                return;

            nextFireTime = Time.time + fireCooldown;
            var muzzle = firePoint != null ? firePoint : transform;
            Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);

            if (animator != null)
                animator.SetTrigger(ShootHash);
        }

        private void OnGUI()
        {
            if (!showControlsHint)
                return;

            hintStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };

            // Below the health readout that QuickstartHealth draws in the same corner.
            var box = new Rect(12f, 42f, 260f, 24f);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 8f, box.y + 3f, box.width - 16f, box.height),
                "WASD move, mouse aims, hold left click to shoot", hintStyle);
        }
    }
}
