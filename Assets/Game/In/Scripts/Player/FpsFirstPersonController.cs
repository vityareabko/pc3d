using UnityEngine;
using UnityEngine.InputSystem; // <-- новый Input System

[RequireComponent(typeof(CharacterController))]
public class FpsFirstPersonControllerInputSystem : MonoBehaviour
{
    [Header("References")]
    public Transform cameraPivot;

    [Header("Movement")]
    public float walkSpeed = 4.0f;
    public float sprintSpeed = 7.0f;
    public float accelTime = 0.08f;
    public float decelTime = 0.12f;

    [Header("Jump/Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;
    public float groundedGravity = -2.0f;

    [Header("Mouse Look")]
    [Tooltip("Чувствительность: градусов на пиксель мыши (и на юнит стика).")]
    public float mouseSensitivity = 0.08f;
    public float lookSmooth = 12f;
    public float maxPitch = 85f;

    [Header("Misc")]
    public float eyeHeight = 1.7f;
    public bool lockCursor = true;

    CharacterController _cc;

    // накопленные углы
    float _yaw;
    float _pitch;

    Vector3 _velocity;
    Vector3 _planarVelCurrent;
    Vector3 _planarVelRef;

    // Input System actions
    InputAction _move;
    InputAction _look;
    InputAction _jump;
    InputAction _sprint;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (cameraPivot == null)
        {
            Debug.LogError("Assign 'cameraPivot' (child Transform with Camera inside).");
        }
        else
        {
            var lp = cameraPivot.localPosition;
            lp.y = eyeHeight;
            cameraPivot.localPosition = lp;
        }

        var e = transform.rotation.eulerAngles;
        _yaw = e.y;
        _pitch = cameraPivot ? NormalizePitch(cameraPivot.localRotation.eulerAngles.x) : 0f;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        SetupInputActions();
    }

    void OnEnable()
    {
        _move?.Enable();
        _look?.Enable();
        _jump?.Enable();
        _sprint?.Enable();
    }

    void OnDisable()
    {
        _move?.Disable();
        _look?.Disable();
        _jump?.Disable();
        _sprint?.Disable();
    }

    void Update()
    {
        // ===== 1) ВВОД =====
        Vector2 mv = _move.ReadValue<Vector2>();   // -1..1
        Vector2 lk = _look.ReadValue<Vector2>();   // мышь: пиксели/кадр, стик: -1..1

        bool sprintHeld = _sprint.IsPressed();
        bool jumpPressed = _jump.WasPressedThisFrame();

        // ===== 2) ПОВОРОТ (накапливаем углы) =====
        // ВАЖНО: дельту мыши НЕ умножаем на deltaTime — это уже "за кадр".
        _yaw   += lk.x * mouseSensitivity;
        _pitch -= lk.y * mouseSensitivity;
        _pitch  = Mathf.Clamp(_pitch, -maxPitch, maxPitch);

        // ===== 3) ПЛАНАРНОЕ ДВИЖЕНИЕ в системе yaw =====
        float targetSpeed = sprintHeld ? sprintSpeed : walkSpeed;
        Vector3 inputDir = new Vector3(mv.x, 0f, mv.y);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        Quaternion yawRot = Quaternion.Euler(0f, _yaw, 0f);
        Vector3 targetPlanarVel = yawRot * inputDir * targetSpeed;

        float smoothTime = (targetPlanarVel.sqrMagnitude > _planarVelCurrent.sqrMagnitude) ? accelTime : decelTime;
        _planarVelCurrent = Vector3.SmoothDamp(_planarVelCurrent, targetPlanarVel, ref _planarVelRef, smoothTime);

        // ===== 4) ГРАВИТАЦИЯ/ПРЫЖОК =====
        if (_cc.isGrounded)
        {
            _velocity.y = groundedGravity;
            if (jumpPressed)
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            _velocity.y += gravity * Time.deltaTime;
        }

        // ===== 5) ПРИМЕНЯЕМ СДВИГ =====
        Vector3 motion = _planarVelCurrent + new Vector3(0f, _velocity.y, 0f);
        _cc.Move(motion * Time.deltaTime);
    }

    void LateUpdate()
    {
        // Поворот тела (yaw) — после движения
        Quaternion targetYaw = Quaternion.Euler(0f, _yaw, 0f);
        if (lookSmooth > 0f)
        {
            float t = 1f - Mathf.Exp(-lookSmooth * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetYaw, t);
        }
        else transform.rotation = targetYaw;

        if (cameraPivot)
        {
            Quaternion targetPitch = Quaternion.Euler(_pitch, 0f, 0f);
            if (lookSmooth > 0f)
            {
                float t = 1f - Mathf.Exp(-lookSmooth * Time.deltaTime);
                cameraPivot.localRotation = Quaternion.Slerp(cameraPivot.localRotation, targetPitch, t);
            }
            else cameraPivot.localRotation = targetPitch;
        }
    }

    void SetupInputActions()
    {
        // Движение (WASD + левый стик)
        _move = new InputAction("Move", InputActionType.Value);
        _move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        _move.AddBinding("<Gamepad>/leftStick");

        // Взгляд (мышиная дельта + правый стик)
        _look = new InputAction("Look", InputActionType.PassThrough);
        _look.AddBinding("<Mouse>/delta");                // пиксели за кадр
        _look.AddBinding("<Gamepad>/rightStick");         // -1..1

        // Прыжок (Space / A)
        _jump = new InputAction("Jump", InputActionType.Button);
        _jump.AddBinding("<Keyboard>/space");
        _jump.AddBinding("<Gamepad>/buttonSouth");

        // Спринт (Shift / L3)
        _sprint = new InputAction("Sprint", InputActionType.Button);
        _sprint.AddBinding("<Keyboard>/leftShift");
        _sprint.AddBinding("<Keyboard>/rightShift");
        _sprint.AddBinding("<Gamepad>/leftStickPress");
    }
    
    public Vector3 GetVelocity() => _velocity;
    
    static float NormalizePitch(float x)
    {
        x = Mathf.Repeat(x + 180f, 360f) - 180f;
        return x;
    }
}
