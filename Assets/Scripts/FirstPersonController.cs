using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace InputAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonController : PortalTraveller
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;
        [Tooltip("The height the player can step up")]
        public float StepOffset = 0.5f;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        public bool allowMovement = true;
        public bool allowCameraRotation = true;

        // cinemachine
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = -53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private InputManager _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        // Additional variables
        private float _yaw;
        private float _pitch;
        private Vector3 _playerVelocity;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<InputManager>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies.");
#endif

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            _controller.slopeLimit = 45f; // Adjust the slope limit
            _controller.stepOffset = StepOffset; // Adjust the height of steps

            // Initialize yaw and pitch based on the current rotation
            _yaw = transform.eulerAngles.y;
            _pitch = CinemachineCameraTarget.transform.localEulerAngles.x;
        }

        private void Update()
        {
            JumpAndGravity();
            GroundedCheck();

            if (allowMovement)
            {
                Move();
            }
        }

        private void LateUpdate()
        {
            if (allowCameraRotation)
            {
                CameraRotation();
            }
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // 如果球体检测到地面，再进行射线检测以确保玩家确实在地面上
            if (Grounded)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, Vector3.down, out hit, GroundedRadius + 0.1f, GroundLayers))
                {
                    // 确保射线检测到的地面距离玩家不超过一定范围
                    if (hit.distance <= GroundedRadius + 0.1f)
                    {
                        Grounded = true;
                    }
                    else
                    {
                        Grounded = false;
                    }
                }
                else
                {
                    Grounded = false;
                }
            }
        }
        private void CameraRotation()
        {
            // if there is an input
            if (_input.look.sqrMagnitude >= _threshold)
            {
                //Don't multiply mouse input by Time.deltaTime
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                // Use _yaw and _pitch for rotation
                _yaw += _input.look.x * RotationSpeed * deltaTimeMultiplier;
                _pitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;

                // Clamp pitch
                _pitch = ClampAngle(_pitch, BottomClamp, TopClamp);

                // Update Cinemachine camera target pitch
                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_pitch, 0.0f, 0.0f);

                // Rotate the player object on the Y axis
                transform.rotation = Quaternion.Euler(0.0f, _yaw, 0.0f);
            }
        }
        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_playerVelocity.x, 0.0f, _playerVelocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // normalize input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // if there is a move input, rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                // move
                inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
            }

            // move the player
            _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update the player's velocity for the next frame
            _playerVelocity = _controller.velocity;
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // reset jump timeout
                    _jumpTimeoutDelta = JumpTimeout;
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal velocity
            if (_verticalVelocity > _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            // Ensure the player doesn't move upwards when colliding with objects
            if (_controller.collisionFlags == CollisionFlags.Above && _verticalVelocity > 0)
            {
                _verticalVelocity = 0;
            }
        }

        public void Bounce(float bounceHeight)
        {
            _verticalVelocity = bounceHeight; // 设置垂直速度为弹跳高度
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }

        // Updated Teleport method to handle scaling
        public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            // Calculate scale factor
            float scaleFactor = scale.x / transform.localScale.x;

            // Update position
            transform.position = pos;

            // Update rotation
            Quaternion deltaRotation = rot * Quaternion.Inverse(transform.rotation);
            Vector3 eulerRot = deltaRotation.eulerAngles;
            _yaw += eulerRot.y;
            _pitch += eulerRot.x;

            // Clamp pitch
            _pitch = ClampAngle(_pitch, BottomClamp, TopClamp);

            // Apply rotation
            transform.rotation = Quaternion.Euler(0.0f, _yaw, 0.0f);
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_pitch, 0.0f, 0.0f);

            // Adjust scale
            transform.localScale = scale;

            // Adjust movement speed and gravity based on scale
            MoveSpeed *= scaleFactor;
            SprintSpeed *= scaleFactor;
            JumpHeight *= scaleFactor;
            Gravity *= scaleFactor;

            // Adjust CharacterController parameters
            _controller.height *= scaleFactor;
            _controller.radius *= scaleFactor;
            _controller.stepOffset *= scaleFactor;
            _controller.center *= scaleFactor;

            // Adjust vertical velocity
            _verticalVelocity *= scaleFactor;

            // Transform the player's velocity
            Vector3 originalVelocity = _playerVelocity;
            _playerVelocity = toPortal.TransformVector(fromPortal.InverseTransformVector(originalVelocity)) * scaleFactor;
        }

        public void ResetYawAndPitch()
        {
            _yaw = transform.eulerAngles.y;
            _pitch = CinemachineCameraTarget.transform.localEulerAngles.x;
        }

        public void LevelPitch()
        {
            _yaw = 0.0f;
            _pitch = 0.0f;
        }

        public void SetRotation(Quaternion rotation)
        {
            // Decompose rotation into yaw and pitch
            Vector3 euler = rotation.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x;

            // Clamp pitch
            _pitch = ClampAngle(_pitch, BottomClamp, TopClamp);

            // Update the player's rotation
            transform.rotation = Quaternion.Euler(0.0f, _yaw, 0.0f);
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_pitch, 0.0f, 0.0f);
        }

        public float GetYaw()
        {
            return _yaw;
        }

        public float GetPitch()
        {
            return _pitch;
        }

        public void SetYawAndPitch(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = ClampAngle(pitch, BottomClamp, TopClamp);

            // Apply rotation
            transform.rotation = Quaternion.Euler(0.0f, _yaw, 0.0f);
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_pitch, 0.0f, 0.0f);
        }
    }
}