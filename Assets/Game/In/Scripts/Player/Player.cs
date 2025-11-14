using System;
using System.Collections;
using KinematicCharacterController.Examples;
using UnityEngine;

public interface IInteract
{
    void Activate();
}

public class Player : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private Transform _holdPoint;
    [SerializeField] private float _rayDistance = 5f;

    [Header("Throw")]
    [SerializeField] private float _moveToHoldSpeed = 20f;   
    [SerializeField] private float _rotateToHoldSpeed = 20f; 
    [SerializeField] private float _throwForce = 12f;        

    public MainCamera camera;
    public ExampleCharacterController controller;

    private Rigidbody _heldRb;
    private Transform _heldTf;
    private Coroutine _moveRoutine;

    private float _origDrag, _origAngDrag;
    private bool _origUseGravity;
    private RigidbodyConstraints _origConstraints;
    private CollisionDetectionMode _origCollision;
    private RigidbodyInterpolation _origInterpolation;

    private void Awake()
    {
        G.main.player = this;
    }

    private void Update()
    {
        RaycastHandle();

        if (_heldRb != null && Input.GetMouseButtonDown(0))
            Throw();
    }

    private void RaycastHandle()
    {
        Ray ray = camera.cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        G.main.ui.hud.SetCursor(CursorType.Default);
        
        if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance))
        {
            var interact = hit.collider.gameObject.layer == LayerMask.NameToLayer(L.Interact);
            var pickUp   = hit.collider.gameObject.layer == LayerMask.NameToLayer(L.PickUpable);

            if (interact)
            {
                G.main.ui.hud.SetCursor(CursorType.Interact);
                
                if (Input.GetKeyDown(K.Interact))
                {
                    if (hit.collider.TryGetComponent(out IInteract i))
                        i.Activate();
                }
            }

            if (pickUp)
            {
                G.main.ui.hud.SetCursor(CursorType.Interact);
                if (Input.GetKeyDown(K.Interact))
                {
                    if (_heldRb == null) TryPickUp(hit);
                    else Drop();

                    if (hit.collider.TryGetComponent(out IIngredient i))
                    {
                        if (i.state != IngredientState.World)
                        {
                            EventAggregator.Post(this, new PickUpIngredient(){ingredient = i});
                        }
                    }
                }
            }
        }
        else
        {
            // G.main.ui.SetCursor(C.Default);
        }
    }

    private void TryPickUp(RaycastHit hit)
    {
        
        Rigidbody rb = hit.rigidbody != null ? hit.rigidbody : hit.collider.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        _origDrag         = rb.linearDamping;
        _origAngDrag      = rb.angularDamping;
        _origUseGravity   = rb.useGravity;
        _origConstraints  = rb.constraints;
        _origCollision    = rb.collisionDetectionMode;
        _origInterpolation= rb.interpolation;

        _heldRb = rb;
        _heldTf = rb.transform;

     
        rb.useGravity = false;
        rb.linearDamping = 10f;
        rb.angularDamping = 10f;
        rb.constraints = RigidbodyConstraints.FreezeRotation; 
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;


        if (_moveRoutine != null) StopCoroutine(_moveRoutine);
        _moveRoutine = StartCoroutine(MoveHeldToPoint());
    }

    private IEnumerator MoveHeldToPoint()
    {
        var wait = new WaitForFixedUpdate();

        while (_heldRb != null)
        {
            Vector3 targetPos = _holdPoint.position;
            Quaternion targetRot = _holdPoint.rotation;
   
            Vector3 newPos = Vector3.Lerp(_heldTf.position, targetPos, Time.fixedDeltaTime * _moveToHoldSpeed);
            Quaternion newRot = Quaternion.Slerp(_heldTf.rotation, targetRot, Time.fixedDeltaTime * _rotateToHoldSpeed);

            _heldRb.MovePosition(newPos);
            _heldRb.MoveRotation(newRot);

            yield return wait;
        }
    }

    private void Drop()
    {
        if (_heldRb == null) return;

        // вернём исходные настройки
        _heldRb.useGravity = _origUseGravity;
        _heldRb.linearDamping = _origDrag;
        _heldRb.angularDamping = _origAngDrag;
        _heldRb.constraints = _origConstraints;
        _heldRb.collisionDetectionMode = _origCollision;
        _heldRb.interpolation = _origInterpolation;

        if (_moveRoutine != null)
        {
            StopCoroutine(_moveRoutine);
            _moveRoutine = null;
        }

        _heldRb = null;
        _heldTf = null;
    }

    private void Throw()
    {
        if (_heldRb == null) return;

        // сохраним ссылку, чтобы кинуть после возврата настроек
        Rigidbody rb = _heldRb;

        // убираем из рук (восстановим физику)
        Drop();

        // добавим импульс вперёд относительно камеры
        Vector3 forward = camera.cam.transform.forward;
        rb.AddForce(forward * _throwForce, ForceMode.Impulse);

        // (опционально) добавим долю вашей скорости для "скилл-шота"
        // если есть доступ к вектору скорости контроллера, раскомментируйте строку ниже и замените GetVelocity():
        // rb.AddForce(GetVelocity() * 0.35f, ForceMode.Impulse);
    //
    // // пример: если в вашем контроллере есть публичный геттер скорости — используйте его
    // private Vector3 GetVelocity() => controller != null ? controller.GetVelocity() : Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        if (camera == null || camera.cam == null)
            return;

        Gizmos.color = Color.red;

        Ray ray = camera.cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

        Gizmos.DrawRay(ray.origin, ray.direction * _rayDistance);
    }

}
