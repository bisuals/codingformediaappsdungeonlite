using NoOpArmy.WiseFeline.BlackBoards;
using UnityEngine;

namespace NoOpArmy.WiseFeline.CombatQuickstart
{
    /// <summary>
    /// Which side something is on. Damage only ever crosses sides, which is what keeps a shooter from
    /// killing itself with a projectile that spawns at its own muzzle.
    /// </summary>
    public enum QuickstartTeam
    {
        /// <summary>The AI agent.</summary>
        Soldier,

        /// <summary>The human player.</summary>
        Player,
    }

    /// <summary>
    /// DEMO CODE, NOT AI CODE. The Combat Quickstart's behaviour contains no custom C# at all; this
    /// component and the two next to it only exist so that the demo can be played rather than only
    /// watched, and nothing in the AI knows they are there.
    /// <para>
    /// It is the whole "wire your damage system to the blackboard" pattern in one class, and it has two
    /// modes which it picks automatically:
    /// </para>
    /// <para>
    /// <b>Write through</b> - when there is a <see cref="BlackBoard"/> on the same GameObject, which is the
    /// case on the soldier. Damage is subtracted from the blackboard's <c>Health</c> key and written
    /// straight back, and this component stores no health of its own. That single
    /// <see cref="BlackBoard.SetFloat"/> is the entire integration: the retreat and rest curves already
    /// read that key, so being shot at makes the soldier disengage without one line of AI code changing.
    /// Replace this component with your own damage system and keep the <c>SetFloat</c>, and the same
    /// behaviour asset keeps working.
    /// </para>
    /// <para>
    /// <b>Standalone</b> - when there is no blackboard, which is the case on the player. Health is a plain
    /// float and reaching zero respawns at the position the object started in, because a quickstart demo
    /// should never end with the player having to press Play again.
    /// </para>
    /// </summary>
    public class QuickstartHealth : MonoBehaviour
    {
        /// <summary>
        /// The side this object is on. QuickstartProjectile only damages the other side.
        /// </summary>
        [Tooltip("The side this object is on. Projectiles only damage the other side.")]
        public QuickstartTeam team = QuickstartTeam.Soldier;

        /// <summary>
        /// Full health, and the upper clamp for both modes. The soldier's blackboard seed and the curves
        /// that read it are both written against 100.
        /// </summary>
        [Tooltip("Full health, and the upper clamp. The soldier's blackboard seed and curves assume 100.")]
        public float maxHealth = 100f;

        /// <summary>
        /// The float key to write through to when there is a blackboard on this GameObject.
        /// </summary>
        [Tooltip("The blackboard float key to write through to, when this GameObject has a BlackBoard.")]
        public string blackboardHealthKey = "Health";

        /// <summary>
        /// Draws the current health in the corner of the screen. Meant for the player, so that the effect
        /// of being shot is visible without opening a window.
        /// </summary>
        [Tooltip("Draw the current health in the top left corner of the screen. Meant for the player.")]
        public bool showHealthOnScreen;

        private BlackBoard blackBoard;
        private float standaloneHealth;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private GUIStyle readoutStyle;

        /// <summary>
        /// True when damage is written through to a blackboard instead of being tracked here.
        /// </summary>
        public bool WritesToBlackboard => blackBoard != null;

        /// <summary>
        /// Current health, read from whichever of the two modes is active.
        /// </summary>
        public float CurrentHealth => blackBoard != null ? blackBoard.GetFloat(blackboardHealthKey) : standaloneHealth;

        private void Awake()
        {
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            standaloneHealth = maxHealth;
        }

        // The blackboard is resolved in Start and not Awake because BlackBoard defines its keys in its
        // own Awake and the order of two Awakes on one GameObject is unspecified - a HasFloat check that
        // runs first would report the key missing and silently lock this component into standalone mode.
        // By Start every Awake has run, and no projectile can land before the first frame.
        private void Start()
        {
            blackBoard = GetComponent<BlackBoard>();

            // GetFloat throws on an unknown key, so a blackboard whose definition has no such key is
            // treated as no blackboard at all rather than as an exception on the first hit.
            if (blackBoard != null && !blackBoard.HasFloat(blackboardHealthKey))
            {
                Debug.LogWarning($"{name}: the blackboard has no float key '{blackboardHealthKey}', " +
                                 "so health is tracked on the component instead of being written through.", this);
                blackBoard = null;
            }
        }

        /// <summary>
        /// Applies damage, clamped into 0..maxHealth. In write through mode this is the only thing the
        /// AI ever sees of the player's weapon.
        /// </summary>
        /// <param name="amount">How much health to remove. Zero and negative amounts are ignored.</param>
        public void TakeDamage(float amount)
        {
            if (amount <= 0f)
                return;

            if (blackBoard != null)
            {
                float health = Mathf.Clamp(blackBoard.GetFloat(blackboardHealthKey) - amount, 0f, maxHealth);
                blackBoard.SetFloat(blackboardHealthKey, health);
                return;
            }

            standaloneHealth = Mathf.Clamp(standaloneHealth - amount, 0f, maxHealth);
            if (standaloneHealth <= 0f)
                Respawn();
        }

        /// <summary>
        /// Back to the starting spot at full health. Only used in standalone mode: the soldier has no
        /// death, it retreats and heals, which is the loop the demo is about.
        /// </summary>
        private void Respawn()
        {
            standaloneHealth = maxHealth;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        }

        private void OnGUI()
        {
            if (!showHealthOnScreen)
                return;

            readoutStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            var box = new Rect(12f, 12f, 190f, 26f);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 8f, box.y + 2f, box.width - 16f, box.height),
                $"{team} health {CurrentHealth:0} / {maxHealth:0}", readoutStyle);
        }
    }
}
