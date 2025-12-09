using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour 
{
    [SerializeField] float movementSpeed = 5f;

    [SerializeField] float midpoint = 2.0f;
    [SerializeField] float jumpHeight = 5f;
    [SerializeField] float jumpDelay = 1.5f;
    [SerializeField] private bool isSprinting = false;
    [SerializeField] private float runSpeed = 3.5f;
    [SerializeField] private bool canJump = true;
    [SerializeField] private SpawnPointManager points;
    private float force = 0.0f;
    private CharacterController characterController;
    public Camera camera;
    private Vector3 moveVector = new Vector3(0, -9.8f, 0);
    private float moveHorizontal;
    private float moveVertical;
    private float speedMultiplier;
    private float yRotation;
    private Vector3 jumpVec;
    private Transform moveTransform;

    private bool isLobby = false;

    private void Start()
    {
        if (!TryGetComponent(out characterController))
        {
            Debug.LogError("No CharacterController found on the player");
            enabled = false;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
            
        camera.enabled = false;
        //are we the owner, if so enable the camera, else disable it
        StartCoroutine(WaitAndSpawn());

    }

    public void CursorControll(bool shouldLock)
    {
        if(shouldLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true; 
        }
    }
    public IEnumerator WaitAndSpawn()
    {
        //this is bullshit
        yield return new WaitForSeconds(0.75f);
        
        points = SpawnPointManager.Instance;
        isLobby = SceneManager.GetActiveScene().name == "Lobby";
         if (!isLobby)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
       
        if(!isLobby)
            camera.enabled = IsOwner;
       
        
        
        points?.SpawnPlayerIn(gameObject);
    }


    private float verticalRotation = 0f;

    private void RotateCamera()
    { 
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        
        // Rotate the player horizontally
        transform.Rotate(Vector3.up * mouseX);
        
        // Adjust vertical rotation and clamp it
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        
        // Apply vertical rotation to the camera
        camera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    } 
    private void Update()
    {
        if (IsOwner)
        {
            Move();
            RotateCamera();    
        }
        
    }

    private void Move()
    {
        //get the axis input
        moveHorizontal = Input.GetAxis("Horizontal");
        moveVertical = Input.GetAxis("Vertical");
        //see if the player should run
        speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? runSpeed : 1f;

        //get the transform of the player
        moveTransform = transform;
        //calculate the move vector
        moveVector = (moveTransform.forward * moveVertical + moveTransform.right * moveHorizontal) * (movementSpeed * speedMultiplier);
    
        //sync the rotation of the camera with the player
        yRotation = camera.transform.rotation.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        

        force -= 9.81f * Time.deltaTime;
        jumpVec = new Vector3(0, force, 0);
        characterController.Move((moveVector + jumpVec) * Time.deltaTime);
    }

    public void Jump()
    {
        if (canJump)
        {
            StartCoroutine(StartJump(jumpDelay));
        }
    }
    
    private IEnumerator StartJump(float delay)
    {
        canJump = false;
        
        force = (2 * jumpHeight / delay) - 9.81f * delay;
        
        yield return new WaitForSeconds(delay);
        
        canJump = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(transform.position, transform.forward);
    }
}
