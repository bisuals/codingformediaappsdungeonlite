using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Camera camera;
    public CharacterController characterController;

    public float walkSpeed = 2f;
    public float sprintSpeed = 4f;
    private bool isRunning = false;
    private float gravity = 10f;
    private Vector3 moveDirection;
    private float actualHorizontalSpeed;
    private float actualSpeed;
    private Vector3 previousPosition;
    public float viewSensitivity = 120f;
    float xRotation;
    private bool canMouseRotate = true;
    private bool canMove = true;
    private bool canSprint = true;

    private float originalHeight = 2;
    private float targetHeight;
    private float crouchSpeed = 5.0f; // Adjust this value to control the crouch speed
    private bool canStandUp;

    private float rotationSpeed = 5; // Rotation Speed
    private float targetZRotation = 0.0f;
    private float currentZRotation = 0.0f;

    // New variables for jumping
    public float jumpForce = 5f;
    private bool isGrounded;

    void Start()
    {
        foreach (Transform findObj in this.transform)
        {
            if (findObj.name == "PlayerCamera")
            {
                camera = findObj.GetComponent<Camera>();
            }
        }

        characterController = transform.GetComponent<CharacterController>();
        Cursor.visible = false;
        Application.targetFrameRate = 60;
        LockCursor(); // Lock cursor after game start
    }

    void Update()
    {
        if (canMove)
        {
            Move();
        }

        if (canMouseRotate && !Input.GetKey(KeyCode.R))
        {
            View();
        }

        if (characterController.height == 1.0f)
        {
            CheckObstaclesAbove();
        }
    }

    private void Move()
    {
        RaycastHit hit;
        actualHorizontalSpeed = ((new Vector3(transform.position.x, 0f, transform.position.z) - new Vector3(previousPosition.x, 0f, previousPosition.z)).magnitude) / Time.deltaTime;
        actualSpeed = ((transform.position - previousPosition).magnitude) / Time.deltaTime;
        previousPosition = transform.position;

        if (characterController.isGrounded)
        {
            isGrounded = true; // Player is on the ground
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);

            if (actualHorizontalSpeed > 0.5f)
            {
                ViewHeadBobbing();
            }

            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if (characterController.height == originalHeight)
                {
                    targetHeight = originalHeight - 1;
                }
                else
                {
                    if (canStandUp)
                    {
                        targetHeight = originalHeight; // Set the target standing height
                    }
                }

                StartCoroutine(ChangeHeightSmoothly());
            }

            if (Input.GetKey(KeyCode.LeftShift) && canSprint)
            {
                isRunning = true;
                moveDirection *= sprintSpeed;
            }
            else
            {
                isRunning = false;
                moveDirection *= walkSpeed;
            }

            // Handle jumping
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                moveDirection.y = jumpForce;
                isGrounded = false; // Prevent multiple jumps
            }

            if (Input.GetKey(KeyCode.D))
            {
                targetZRotation = -1.5f;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                targetZRotation = 1.5f;
            }
            else if (!Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))
            {
                targetZRotation = 0.0f;
            }

            currentZRotation = Mathf.Lerp(currentZRotation, targetZRotation, Time.deltaTime * rotationSpeed);
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, currentZRotation);
        }

        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;
        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void CheckObstaclesAbove()
    {
        float raycastDistance = 1.5f;

        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, raycastDistance))
        {
            canStandUp = false;
        }
        else
        {
            canStandUp = true;
        }
    }

    private IEnumerator ChangeHeightSmoothly()
    {
        float elapsedTime = 0f;
        float startHeight = characterController.height;

        while (elapsedTime < 1.0f)
        {
            elapsedTime += Time.deltaTime * crouchSpeed;
            characterController.height = Mathf.Lerp(startHeight, targetHeight, elapsedTime);
            yield return null;
        }

        characterController.height = targetHeight;
    }

    void View()
    {
        float inputX = Input.GetAxis("Mouse X") * viewSensitivity * Time.deltaTime;
        float inputY = Input.GetAxis("Mouse Y") * viewSensitivity * Time.deltaTime;

        xRotation -= inputY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * inputX);
    }

    private float timer = 0.0f;
    public float bobbingSpeed = 0.24f;
    public float bobbingAmount = 0.06f;
    float bobbingAmountMultiplier = 0.5f;
    float midpoint = 0.7f;

    void ViewHeadBobbing()
    {
        float waveslice = 0.0f;
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (Mathf.Abs(horizontal) == 0 && Mathf.Abs(vertical) == 0)
        {
            timer = 0.0f;
        }
        else
        {
            waveslice = Mathf.Sin(timer);
            timer += bobbingSpeed * (Time.deltaTime * 60f);

            if (timer > Mathf.PI * 2)
            {
                timer -= Mathf.PI * 2;
            }
        }

        Vector3 v3T = camera.transform.localPosition;

        if (waveslice != 0)
        {
            float translateChange = waveslice * (bobbingAmount * (bobbingAmountMultiplier * 0.1f));
            float totalAxes = Mathf.Abs(horizontal) + Mathf.Abs(vertical);
            totalAxes = Mathf.Clamp(totalAxes, 0.0f, 1.0f);
            translateChange = totalAxes * translateChange;
            v3T.y = midpoint + translateChange;
        }
        else
        {
            v3T.y = midpoint;
        }

        camera.transform.localPosition = v3T;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}