using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PlayerMovement : MonoBehaviour, IPunObservable
{
    private CharacterController _characterController;
    private Camera _camera;

    [SerializeField] private float _mouseSensitivity = 100.0f;
    [SerializeField] private float _currentSpeed = 10.0f;
    [SerializeField] private float _defaultSpeed = 10.0f;
    [SerializeField] private float _additionalSpeed = 10.0f;
    [SerializeField] private float _jumpHeight = 2.0f;
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private float _stopDelay = 4.0f;

    private float _verticalVelocity = 0f;
    private bool _isGrounded;

    private PhotonView _photonView;
    private Vector3 _networkPosition;
    private Quaternion _networkRotation;

    private void Start()
    {
        _photonView = GetComponent<PhotonView>();
        _characterController = GetComponent<CharacterController>();
        _camera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        if (_photonView.IsMine)
        {
            _camera.depth = 1.0f;
            MovePlayer();
            HandleJump();
            RotatePlayer();
        }
        else
        {
            SmoothNetworkMovement();
        }
    }

    private void RotatePlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        float cameraRotationX = _camera.transform.localEulerAngles.x - mouseY;
        if (cameraRotationX > 180f) cameraRotationX -= 360f;
        cameraRotationX = Mathf.Clamp(cameraRotationX, 15f, 35f);

        _camera.transform.localEulerAngles = new Vector3(cameraRotationX, 0, 0);
    }

    private void MovePlayer()
    {
        _isGrounded = _characterController.isGrounded;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        Vector3 velocity = move * _currentSpeed + Vector3.up * _verticalVelocity;

        _characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (_isGrounded)
        {
            _verticalVelocity = _verticalVelocity < 0 ? -2f : _verticalVelocity;
            if (Input.GetButtonDown("Jump"))
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }
        _verticalVelocity += _gravity * Time.deltaTime;
    }

    private void SmoothNetworkMovement()
    {
        transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * 10);
        transform.rotation = Quaternion.Lerp(transform.rotation, _networkRotation, Time.deltaTime * 10);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            _networkPosition = (Vector3)stream.ReceiveNext();
            _networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }
    [PunRPC]
    public void ConvertIntoDemon()
    {
        StartCoroutine(Convert());
    }
    public IEnumerator Convert()
    {
        _currentSpeed = 0;
        yield return new WaitForSeconds(_stopDelay);
        _currentSpeed = _defaultSpeed + _additionalSpeed;
    }
    [PunRPC]
    public void ConvertIntoPlayerAgain()
    {
        _currentSpeed = _defaultSpeed;
    }
}
