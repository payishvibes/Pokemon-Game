using System;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

namespace PokemonGame.Game
{
    using UnityEngine;

    public class PlayerMovement : MonoBehaviour
    {
        [Header("Assigning")]
        public CharacterController controller;
        public Transform cam;
        public Transform groundDetectorPos;

        private Player _player;

        [Header("Control Values")]
        public float normalSpeed = 6f;

        private float _turnSmoothTime = 0.1f;
        private float _turnSmoothVelocity;
        public bool canMove = true;
        [SerializeField] float speed;
        [SerializeField] private LayerMask ground;
        [SerializeField] private CinemachineInputAxisController cameraInputController;

        private int _frameSkips = 0;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private void Start()
        {
            speed = normalSpeed;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (_frameSkips >= 2)
            {
                if (canMove && !_player.interacting)
                {
                    cameraInputController.Controllers[0].Enabled = true;
                    cameraInputController.Controllers[1].Enabled = true;
                    
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    Vector2 direction = InputSystem.actions.FindAction("Move").ReadValue<Vector2>();
                
                    if (direction.magnitude >= 0.1f)
                    {
                        float targetAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg + cam.eulerAngles.y;
                        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, angle, 0f);
                    
                        Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                        controller.Move( speed * Time.deltaTime * moveDirection.normalized);
                    }
                }
                else
                {
                    cameraInputController.Controllers[0].Enabled = false;
                    cameraInputController.Controllers[1].Enabled = false;
                    
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
            else
            {
                _frameSkips++;
            }
            
            if (Physics.Raycast(groundDetectorPos.position, Vector3.down, out RaycastHit hit, 10f, ground))
            {
                Vector3 newPos = transform.position;
                newPos.y = hit.point.y+transform.localScale.y/2;
                transform.position = newPos;
            }
        }
    }
}