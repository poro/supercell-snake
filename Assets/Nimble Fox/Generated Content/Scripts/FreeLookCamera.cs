using UnityEngine;

public class FreeLookCamera : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float lookSpeed = 3f;
    [SerializeField] private float scrollSpeed = 10f;

    private float _rotationX;
    private float _rotationY;
    private bool _looking;

    private void Start()
    {
        Vector3 euler = transform.eulerAngles;
        _rotationX = euler.y;
        _rotationY = euler.x;
    }

    private void Update()
    {
        // WASD / Arrow keys always move the camera
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float up = 0f;
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) up = 1f;
        if (Input.GetKey(KeyCode.Q)) up = -1f;

        Vector3 move = transform.right * h + transform.forward * v + Vector3.up * up;
        transform.position += move * moveSpeed * Time.deltaTime;

        // Click (left or right) + drag to look around
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            _looking = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Escape to release the cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _looking = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (_looking)
        {
            _rotationX += Input.GetAxis("Mouse X") * lookSpeed;
            _rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
            _rotationY = Mathf.Clamp(_rotationY, -90f, 90f);
            transform.rotation = Quaternion.Euler(_rotationY, _rotationX, 0f);
        }

        // Two-finger scroll / scroll wheel to zoom forward/back
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            transform.position += transform.forward * scroll * scrollSpeed;
        }
    }
}
