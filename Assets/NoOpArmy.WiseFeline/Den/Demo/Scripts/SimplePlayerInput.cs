using UnityEngine;

namespace NoOpArmy.WiseFeline.Sample
{
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerInput : MonoBehaviour
    {
        public float speed = 10;
        public float rotationSpeed = 90;
        private CharacterController character;
        private Attack attack;

        private void Awake()
        {
            character = GetComponent<CharacterController>();
            attack = GetComponent<Attack>();
        }

        void Update()
        {
            Vector3 movement = Vector3.zero;
            movement.z = Input.GetAxis("Vertical");
            movement.x = Input.GetAxis("Horizontal");
            movement.Normalize();
            if (movement != Vector3.zero)
            {
                character.SimpleMove(character.transform.TransformDirection(movement) * speed);
            }
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo, 100))
            {
                var pos = hitInfo.point;
                pos.y = character.transform.position.y;
                character.transform.LookAt(pos);

            }
            if (attack == null)
                return;
            if (Input.GetMouseButtonDown(0))
            {
                attack.MeleeAttackWithDelay(Vector3.forward, 1, 10, 0.2f);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                attack.MeleeAttackWithDelay(Vector3.forward, 1f, 50, 1);
            }
        }
    }
}
