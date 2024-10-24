using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 5f;
    public float crouchHeight = 0.5f;
    public float normalHeight = 2f;

    public Camera playerCamera;
    public float mouseSensitivity = 2f;

    private Rigidbody rb;
    private bool isCrouching = false;
    private bool isSprinting = false;
    private float originalHeight;
    private float cameraVerticalAngle = 0f;

    private PlayerInteraction playerInteraction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalHeight = transform.localScale.y;
        Cursor.lockState = CursorLockMode.Locked;

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        InitializePlayerInteraction();
    }

    void Update()
    {
        HandleMovementInput();
        HandleJumpInput();
        HandleCrouchInput();
        HandleSprintInput();
        RotateView();
    }

    void FixedUpdate()
    {
        Move();
    }

    private void InitializePlayerInteraction()
    {
        playerInteraction = GetComponent<PlayerInteraction>();
        if (playerInteraction != null)
        {
            playerInteraction.playerCamera = playerCamera;
            playerInteraction.uiManager = FindObjectOfType<UIManager>();
        }
        else
        {
            Debug.LogError("PlayerInteraction component is not attached to the player.");
        }
    }

    private void HandleMovementInput()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = transform.right * moveHorizontal + transform.forward * moveVertical;
        float speed = isSprinting ? sprintSpeed : (isCrouching ? crouchSpeed : walkSpeed);

        rb.MovePosition(transform.position + movement * speed * Time.deltaTime);
    }

    private void HandleJumpInput()
    {
        if (Input.GetButtonDown("Jump") && Mathf.Abs(rb.linearVelocity.y) < 0.001f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void HandleCrouchInput()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (!isCrouching)
            {
                isCrouching = true;
                transform.localScale = new Vector3(transform.localScale.x, crouchHeight / originalHeight, transform.localScale.z);
            }
        }
        else
        {
            if (isCrouching)
            {
                isCrouching = false;
                transform.localScale = new Vector3(transform.localScale.x, 1, transform.localScale.z);
            }
        }
    }

    private void HandleSprintInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isSprinting = true;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isSprinting = false;
        }
    }

    private void RotateView()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 水平旋转
        transform.Rotate(Vector3.up * mouseX);

        // 垂直旋转
        cameraVerticalAngle -= mouseY;
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, -90f, 90f);
        playerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngle, 0f, 0f);
    }

    private void Move()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = transform.right * moveHorizontal + transform.forward * moveVertical;
        float speed = isSprinting ? sprintSpeed : (isCrouching ? crouchSpeed : walkSpeed);

        rb.MovePosition(transform.position + movement * speed * Time.deltaTime);
    }
}
