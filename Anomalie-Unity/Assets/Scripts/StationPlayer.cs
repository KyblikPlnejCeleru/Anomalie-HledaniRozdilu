using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class StationPlayer : MonoBehaviour
{
    [SerializeField] private float _speed = 5.5f;
    [SerializeField] private float _sprint = 8.5f;
    [SerializeField] private float _gravity = 18f;
    [SerializeField] private float _jump = 6f;
    [SerializeField] private float _sensitivity = 0.12f;
    [SerializeField] private float _maxPitch = 85f;

    private CharacterController _body;
    private Transform _cam;
    private float _pitch;
    private float _vertical;

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Awake()
    {
        _body = GetComponent<CharacterController>();
        _cam = Camera.main.transform;
        LockCursor();
    }

    private void Update()
    {
        if (Time.timeScale < 0.01f) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        float x = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float z = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        float speed = kb.leftShiftKey.isPressed ? _sprint : _speed;
        Vector3 move = (transform.forward * z + transform.right * x) * speed;

        if (_body.isGrounded)
        {
            _vertical = kb.spaceKey.wasPressedThisFrame ? _jump : -2f;
        }
        else
        {
            _vertical -= _gravity * Time.deltaTime;
        }
        move.y = _vertical;
        _body.Move(move * Time.deltaTime);

        Vector2 delta = mouse.delta.ReadValue() * _sensitivity;
        transform.Rotate(0f, delta.x, 0f);
        _pitch = Mathf.Clamp(_pitch - delta.y, -_maxPitch, _maxPitch);
        _cam.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}
