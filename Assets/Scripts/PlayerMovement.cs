using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerMovement : NetworkBehaviour
{
    public float speed = 1f;

    public Animator movmentAnimator;
    public SpriteRenderer spriteRenderer;
    public Rigidbody2D rb;
    public bool isGrounded = false;
    private Vector3 velocity = Vector3.zero;
    public float distance = 0.6f;
    public float radius = 0.2f;
    private bool lastFlipX;
    public enum PlayerAnimState
    {
        Idle,
        Run,
        Jump
    }

    public PlayerAnimState currentState;
    LayerMask playerMask;
    [SerializeField] private bool version = true;


    public override void OnNetworkSpawn() 
    {
        if(!TryGetComponent(out movmentAnimator))
        {
            print("No animator found");
        }
        if(!TryGetComponent(out spriteRenderer))
        {
            print("No sprite renderer found");
        }
        if(!TryGetComponent(out rb))
        {
            print("No rigidbody found");
        }
        playerMask = LayerMask.GetMask("Player");
    }

    void FixedUpdate()
    {
        if(IsOwner)
        {
            
            GetInput();
            if (version)
            {
                ApplyMotion();
            }
            else
            {
                HandleMovementServerRPC(velocity,NetworkObjectId);    
            }
            
            CheckIfGrounded();
        }
       
    }

    private void Update()
    {
        if (IsOwner)
        {
            ShouldFlipPlayer();
            UpdateAnimations();
        }
    }

    private void ShouldFlipPlayer()
    {
        bool newFlipX = velocity.x switch
        {
            // Everyone updates flip based on local velocity
            < -0.01f => true,
            > 0.01f => false,
            _ => spriteRenderer.flipX
        };
        
        
        if (newFlipX != lastFlipX)
        {
            lastFlipX = newFlipX;
            SubmitFlipServerRpc(newFlipX);
        } 
        
    }

    private void CheckIfGrounded()
    {
        //circle cast down to check if grounded
        //ignore the player layer
        var ignoreLayer = ~playerMask;
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, radius, Vector3.down, distance, ignoreLayer);
        if(hit.collider != null)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    private void UpdateAnimations()
    {

        float cache = (speed * Time.deltaTime);
        if (!isGrounded)
        {
            if (velocity.y >= cache)
            {
                ChangeAnimationState(PlayerAnimState.Jump);
            }
            
        }
        else
        {
            if (velocity.x >= cache || velocity.x <= -cache)
            {
                //get the x axis and normalize it so it is either 1 or -1
                ChangeAnimationState(PlayerAnimState.Run);
            }
            
            else if (velocity.sqrMagnitude < 0.0001f)
            {
                ChangeAnimationState(PlayerAnimState.Idle);
            }

        }
    }

    private void GetInput()
    {
            velocity = Vector3.zero;
            if(Input.GetKey(KeyCode.UpArrow))
            {
                velocity += Vector3.up;
            }
            
            if(Input.GetKey(KeyCode.RightArrow))
            {
                velocity += Vector3.right;
            }
            if(Input.GetKey(KeyCode.LeftArrow))
            {
                velocity += Vector3.left;
            }  

    }

    private void ApplyMotion()
    {
            velocity *= (speed * Time.deltaTime);

            transform.position += new Vector3(velocity.x, velocity.y, 0);
    }

    private void ChangeAnimationState(PlayerAnimState state)
    {
        if (currentState == state) return; // don’t restart the same anim

        switch (state)
        {
            case PlayerAnimState.Idle:
                movmentAnimator.CrossFade("Idle-Animation", 0.1f, 0);
                break;
            case PlayerAnimState.Run:
                movmentAnimator.CrossFade("run-Animation", 0.1f, 0);
                break;
            case PlayerAnimState.Jump:
                movmentAnimator.CrossFade("jumpAnimation", 0.1f, 0);
                break;
        }

        currentState = state;
    }

    [ServerRpc]
    private void SubmitFlipServerRpc(bool flipX)
    {
        BroadcastFlipClientRpc(flipX);
    }

    [ClientRpc]
    private void BroadcastFlipClientRpc(bool flipX)
    {
        spriteRenderer.flipX = flipX;
    }

    [ServerRpc]
    void HandleMovementServerRPC(Vector3 vel, ulong id)
    {
        
        HandleMovementClientRPC(vel);
    }

    [ClientRpc]
    void HandleMovementClientRPC(Vector3 vel)
    {
        transform.position += (vel * Time.deltaTime);
    }
    
    //on draw gizmos draw a circle at the players feet to show the grounded check
    private void OnDrawGizmos() 
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(transform.position.x, transform.position.y - distance, transform.position.z), radius);
    }
}