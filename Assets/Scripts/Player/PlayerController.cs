using System;
using UnityEngine;
using UnityEngine.InputSystem;
using ReactorBreach.Core;
using ReactorBreach.Data;
using ReactorBreach.Enemies;
using ReactorBreach.Environment;

namespace ReactorBreach.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(ToolManager))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Input")]
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";

        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintSpeed = 7f;
        [SerializeField] private float _jumpHeight = 1.2f;
        [SerializeField] private float _gravity = -9.81f;

        [Header("Ground Check")]
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundCheckRadius = 0.28f;
        [SerializeField] private LayerMask _groundMask;

        [Header("Collision")]
        [SerializeField] private float _maxPushableMass = 30f;
        [SerializeField] private float _pushForceScale = 14f;
        [SerializeField] private float _enemyContactDamage = 5000f;

        [Header("Camera")]
        [SerializeField] private Transform _cameraTarget;
        [SerializeField] private float _mouseSensitivity = 0.15f;
        [SerializeField] private float _maxPitch = 85f;

        private float _pitch;

        // Input values
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _isSprinting;
        private bool _jumpRequested;

        // State
        private Vector3 _velocity;
        private bool _isGrounded;
        private float _stepTimer;
        private Vector3 _lastHorizontalMoveDir;

        // Components
        private CharacterController _controller;
        private ToolManager _toolManager;
        private PlayerHealth _health;
        private InputActionMap _actionMap;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            // Не используем DontDestroyOnLoad: при перезагрузке сцены игрок
            // должен появляться в точке спавна уровня, а не на месте смерти.

            _controller  = GetComponent<CharacterController>();
            _toolManager = GetComponent<ToolManager>();
            _health      = GetComponent<PlayerHealth>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            if (_inputActions == null)
            {
                Debug.LogError("[PlayerController] InputActionAsset is null. Assign PlayerActions in the inspector.");
                return;
            }

            _actionMap = _inputActions.FindActionMap(_actionMapName, throwIfNotFound: false);
            if (_actionMap == null)
            {
                Debug.LogError($"[PlayerController] Action map '{_actionMapName}' not found in {_inputActions.name}.");
                return;
            }

            Subscribe("Move",            OnMove);
            Subscribe("Look",            OnLook);
            Subscribe("Jump",            OnJump);
            Subscribe("Sprint",          OnSprint);
            Subscribe("PrimaryAction",   OnPrimaryAction);
            Subscribe("SecondaryAction", OnSecondaryAction);
            Subscribe("ToolSlot1",       OnToolSlot1);
            Subscribe("ToolSlot2",       OnToolSlot2);
            Subscribe("ToolSlot3",       OnToolSlot3);
            Subscribe("ToolScroll",      OnToolScroll);
            Subscribe("Pause",           OnPause);

            _actionMap.Enable();
            Debug.Log($"[PlayerController] Subscribed to {_actionMap.actions.Count} actions in map '{_actionMap.name}'. Map enabled = {_actionMap.enabled}.");
        }

        private void OnDisable()
        {
            if (_actionMap == null) return;

            Unsubscribe("Move",            OnMove);
            Unsubscribe("Look",            OnLook);
            Unsubscribe("Jump",            OnJump);
            Unsubscribe("Sprint",          OnSprint);
            Unsubscribe("PrimaryAction",   OnPrimaryAction);
            Unsubscribe("SecondaryAction", OnSecondaryAction);
            Unsubscribe("ToolSlot1",       OnToolSlot1);
            Unsubscribe("ToolSlot2",       OnToolSlot2);
            Unsubscribe("ToolSlot3",       OnToolSlot3);
            Unsubscribe("ToolScroll",      OnToolScroll);
            Unsubscribe("Pause",           OnPause);
        }

        private void Subscribe(string actionName, Action<InputAction.CallbackContext> handler)
        {
            var action = _actionMap.FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"[PlayerController] Action '{actionName}' not found in map.");
                return;
            }
            action.started   += handler;
            action.performed += handler;
            action.canceled  += handler;
        }

        private void Unsubscribe(string actionName, Action<InputAction.CallbackContext> handler)
        {
            var action = _actionMap.FindAction(actionName);
            if (action == null) return;
            action.started   -= handler;
            action.performed -= handler;
            action.canceled  -= handler;
        }

        private void Update()
        {
            bool wasGrounded = _isGrounded;
            float preLandVelocityY = _velocity.y;
            GroundCheck();
            CheckFallDamage(wasGrounded, preLandVelocityY);
            Move();
            LookUpdate();
            ApplyGravity();
            CheckPitUnderStationInLevel1();
            EmitStepVibrations();
            _toolManager.CurrentTool?.Tick(Time.deltaTime);
        }

        private void LookUpdate()
        {
            float yaw   = _lookInput.x * _mouseSensitivity;
            float pitch = _lookInput.y * _mouseSensitivity;

            if (Mathf.Abs(yaw) > 0f)
                transform.Rotate(Vector3.up, yaw);

            _pitch = Mathf.Clamp(_pitch - pitch, -_maxPitch, _maxPitch);
            if (_cameraTarget != null)
                _cameraTarget.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void GroundCheck()
        {
            _isGrounded = Physics.CheckSphere(
                _groundCheck != null ? _groundCheck.position : transform.position + Vector3.down * 0.9f,
                _groundCheckRadius,
                _groundMask);

            if (_isGrounded && _velocity.y < 0f)
                _velocity.y = -2f;
        }

        private void Move()
        {
            float speed = _isSprinting ? _sprintSpeed : _walkSpeed;

            // Camera-relative input direction
            Vector3 forward = Camera.main != null
                ? Camera.main.transform.forward
                : transform.forward;
            Vector3 right = Camera.main != null
                ? Camera.main.transform.right
                : transform.right;
            forward.y = 0f;
            right.y   = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 dir = forward * _moveInput.y + right * _moveInput.x;
            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
                _lastHorizontalMoveDir = dir;
            }
            _controller.Move(dir * (speed * Time.deltaTime));

            if (_jumpRequested && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                _jumpRequested = false;
            }
        }

        private void CheckFallDamage(bool wasGrounded, float velocityY)
        {
            if (!wasGrounded && _isGrounded && velocityY < 0f)
            {
                float fallSpeed = Mathf.Abs(velocityY);
                float fallDamage = Mathf.Max(0f, fallSpeed - 10f) * 5f;
                if (fallDamage > 0f) _health.TakeDamage(fallDamage);
            }
        }

        private void CheckPitUnderStationInLevel1()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;
            if (_health.IsDead) return;

            float z = transform.position.z;
            float x = transform.position.x;
            if (z <= GameConstants.Level1_ChasmZMin || z >= GameConstants.Level1_ChasmZMax) return;
            if (Mathf.Abs(x) >= GameConstants.Level1_SectionHalfWidthX) return;

            float bottomY = transform.position.y + _controller.center.y - 0.5f * _controller.height;
            if (bottomY >= GameConstants.StationFloorPlaneY) return;

            _health.TakeDamage(9999f);
        }

        private void ApplyGravity()
        {
            _velocity.y += _gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        private void EmitStepVibrations()
        {
            bool isMoving = _moveInput.sqrMagnitude > 0.01f && _isGrounded;
            if (!isMoving) return;

            float interval = _isSprinting
                ? GameConstants.StepVibrationIntervalSprint
                : GameConstants.StepVibrationIntervalWalk;
            float radius = _isSprinting
                ? GameConstants.VibrationRadiusSprint
                : GameConstants.VibrationRadiusWalk;

            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                _stepTimer = interval;
                Enemies.VibrationSystem.Emit(transform.position, radius);
            }
        }

        public void OnMove(InputAction.CallbackContext ctx)
            => _moveInput = ctx.ReadValue<Vector2>();

        public void OnLook(InputAction.CallbackContext ctx)
            => _lookInput = ctx.ReadValue<Vector2>();

        public void OnJump(InputAction.CallbackContext ctx)
        {
            if (ctx.started && _isGrounded) _jumpRequested = true;
        }

        public void OnSprint(InputAction.CallbackContext ctx)
            => _isSprinting = ctx.ReadValueAsButton();

        public void OnPrimaryAction(InputAction.CallbackContext ctx)
            => _toolManager.OnPrimaryAction(ctx);

        public void OnSecondaryAction(InputAction.CallbackContext ctx)
            => _toolManager.OnSecondaryAction(ctx);

        public void OnToolSlot1(InputAction.CallbackContext ctx) { if (ctx.started) _toolManager.SwitchTool(0); }
        public void OnToolSlot2(InputAction.CallbackContext ctx) { if (ctx.started) _toolManager.SwitchTool(1); }
        public void OnToolSlot3(InputAction.CallbackContext ctx) { if (ctx.started) _toolManager.SwitchTool(2); }

        public void OnToolScroll(InputAction.CallbackContext ctx)
        {
            if (!ctx.started) return;
            float scroll = ctx.ReadValue<Vector2>().y;
            if (scroll > 0f) _toolManager.SwitchTool((_toolManager.CurrentIndex + 1) % 3);
            else if (scroll < 0f) _toolManager.SwitchTool((_toolManager.CurrentIndex + 2) % 3);
        }

        public void OnPause(InputAction.CallbackContext ctx)
        {
            if (!ctx.started) return;
            if (Core.GameManager.Instance?.CurrentState == Core.GameState.Playing)
                Core.GameManager.Instance.PauseGame();
            else
                Core.GameManager.Instance?.ResumeGame();
        }

        public Vector2 GetLookInput() => _lookInput;
        public bool IsGrounded => _isGrounded;
        public bool IsSprinting => _isSprinting;
        public float VerticalVelocity => _velocity.y;

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;
            if (hit.collider == null) return;

            if (hit.collider.gameObject.layer == GameConstants.LayerEnemy
                && hit.collider.GetComponentInParent<EnemyBase>() != null)
            {
                if (!_health.IsDead) _health.TakeDamage(_enemyContactDamage);
                return;
            }

            Rigidbody body = hit.rigidbody != null ? hit.rigidbody : hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return;
            if (hit.gameObject.layer != GameConstants.LayerInteractable) return;
            if (!body.TryGetComponent<GravityAffectableObject>(out var grav)) return;
            bool can = grav.CanBePushedByPlayer
                || (body.mass < 0.4f && body.mass < grav.OriginalMass);
            if (!can) return;
            if (body.mass > _maxPushableMass) return;

            if (hit.moveDirection.y < -0.25f) return;

            float strength = 1f - (body.mass / _maxPushableMass);
            if (strength < 0.01f) return;

            var push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            if (push.sqrMagnitude < 0.0001f && _lastHorizontalMoveDir.sqrMagnitude > 0.0001f)
                push = new Vector3(_lastHorizontalMoveDir.x, 0f, _lastHorizontalMoveDir.z);
            if (push.sqrMagnitude < 0.0001f) return;
            push = push.normalized * (_pushForceScale * strength);

            body.WakeUp();
            body.AddForceAtPosition(push, hit.point, ForceMode.Force);
        }
    }
}
