using UnityEngine;

namespace NoOpArmy.UtilityAI.Sample
{

    /// <summary>
    /// This script handles the fly camera logic
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FlyCamera : MonoBehaviour
    {
        public float acceleration = 50;
        public float accSprintMultiplier = 4;
        public float lookSensitivity = 1;
        public float dampingCoefficient = 5;
        public bool focusOnEnable = true;

        Vector3 velocity;


        void OnEnable()
        {
        }


        void Update()
        {
            if (Input.GetMouseButton(1))
                UpdateInput();


            // Physics: Damping to bring the camera to a halt
            velocity = Vector3.Lerp(velocity, Vector3.zero, dampingCoefficient * Time.fixedDeltaTime);
            transform.position += velocity * Time.fixedDeltaTime;
        }

        void UpdateInput()
        {
            // Handle position and rotation based on user input

            // Position: Acceleration based on user input
            velocity += GetAccelerationVector() * Time.fixedDeltaTime;

            // Rotation: Mouse input controls camera rotation
            Vector2 mouseDelta = lookSensitivity * new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            Quaternion rotation = transform.rotation;
            Quaternion horiz = Quaternion.AngleAxis(mouseDelta.x, Vector3.up);
            Quaternion vert = Quaternion.AngleAxis(mouseDelta.y, Vector3.right);
            transform.rotation = horiz * rotation * vert;


        }

        Vector3 GetAccelerationVector()
        {
            // Calculate the acceleration vector based on user input

            Vector3 moveInput = default;

            void AddMovement(KeyCode key, Vector3 dir)
            {
                // Add movement based on key input
                if (Input.GetKey(key))
                    moveInput += dir;
            }

            // Define movement keys and directions
            AddMovement(KeyCode.W, Vector3.forward);
            AddMovement(KeyCode.S, Vector3.back);
            AddMovement(KeyCode.D, Vector3.right);
            AddMovement(KeyCode.A, Vector3.left);
            AddMovement(KeyCode.Space, Vector3.up);
            AddMovement(KeyCode.LeftControl, Vector3.down);
            Vector3 direction = transform.TransformVector(moveInput.normalized);

            // Apply acceleration, considering sprinting if LeftShift is held
            if (Input.GetKey(KeyCode.LeftShift))
                return direction * (acceleration * accSprintMultiplier); // "sprinting"
            return direction * acceleration; // "walking"
        }
    }
}