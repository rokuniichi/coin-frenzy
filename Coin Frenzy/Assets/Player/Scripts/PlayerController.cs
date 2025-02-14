﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    private const float GRAVITY_CONST = 15.0f;
    private const float DOWNFORCE = -0.1f;
    private const float RAY_Y_OFFSET = 0.2f;
    private const float RAY_DISTANCE = 0.5f;
    private const float SPEED = 500.0f;
    private const float SPEED_MODIFIER = 2.0f;
    private const float JUMP_FORCE = 8.0f;
    private const float POWERUP_DURATION = 3.0f;
    private const float MAX_MAGNITUDE = 10.0f;

    [SerializeField]
    private FixedJoystick movementJoystick;
    [SerializeField]
    private FixedButton jumpButton;
    [SerializeField]
    private GameObject playerModel;

    private GameManager gm;
    private TrailRenderer trail;
    private Camera mainCamera;
    private Rigidbody playerRb;
    private Animator anim;
    private CapsuleCollider col;
    private Coroutine lastPowerupHandler;
    private Vector3 velocity;
    private Vector3 tempVelocity;
    private float magnitude;
    private float verticalVelocity;
    private float cameraAngleY;
    private bool jump;
    private float speedModifier;

    private void Start()
    {
        gm = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        trail = GetComponent<TrailRenderer>();
        playerRb = GetComponent<Rigidbody>();
        anim = playerModel.GetComponent<Animator>();
        col = GetComponent<CapsuleCollider>();
        trail.enabled = false;
        mainCamera = Camera.main;
        speedModifier = 1;
    }
    private void Update()
    {

    }
    private void FixedUpdate()
    {
        MoveAndRotatePlayer();
        AnimatePlayer(jump);
    }
    private void MoveAndRotatePlayer()
    {
        // Moving and rotating player
        cameraAngleY = mainCamera.transform.rotation.eulerAngles.y;

        velocity = new Vector3(movementJoystick.Horizontal, 0, movementJoystick.Vertical) * SPEED * Time.deltaTime * speedModifier;
        transform.rotation = Quaternion.AngleAxis(cameraAngleY +
        Vector3.SignedAngle(Vector3.forward, velocity.normalized + Vector3.forward * 0.01f, Vector3.up), Vector3.up);
        velocity = Quaternion.AngleAxis(cameraAngleY, Vector3.up) * velocity;

        if (IsGrounded())
        {
            verticalVelocity = DOWNFORCE;
            if (jumpButton.isPressed)
            {
                jump = true;
                verticalVelocity = JUMP_FORCE;
            }
        }
        else
        {
            jump = false;
            verticalVelocity -= GRAVITY_CONST * Time.deltaTime;
        }

        playerRb.velocity = new Vector3(velocity.x, verticalVelocity, velocity.z);
    }

    private void LateUpdate()
    {
        playerModel.transform.position = transform.position;
        playerModel.transform.rotation = transform.rotation;
    }

    private void AnimatePlayer(bool jump)
    {
        if (jump)
        {
            anim.SetTrigger("jump_t");
        }
        anim.SetBool("grounded_b", IsGrounded());
        tempVelocity = playerRb.velocity;
        tempVelocity.y = 0;
        magnitude = tempVelocity.magnitude;
        anim.SetFloat("speed_f", magnitude);
        anim.SetFloat("modifier_f", magnitude / MAX_MAGNITUDE);
        Debug.Log(magnitude / MAX_MAGNITUDE);
    }


    private bool IsGrounded()
    {
        Ray groundRay = new Ray(new Vector3(col.bounds.center.x, col.bounds.center.y - col.bounds.extents.y + RAY_Y_OFFSET, col.bounds.center.z), Vector3.down);
        return Physics.Raycast(groundRay.origin, groundRay.direction, 0.3f);
        //Debug.DrawRay(new Vector3(col.bounds.center.x, col.bounds.center.y - col.bounds.extents.y + RAY_Y_CONST, col.bounds.center.z), Vector3.down, Color.red, 1.0f);
    }

    IEnumerator PowerupCollectHandler()
    {
        speedModifier = SPEED_MODIFIER;
        trail.enabled = true;
        yield return new WaitForSeconds(POWERUP_DURATION);
        trail.enabled = false;
        speedModifier = 1;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Coin"))
        {
            gm.CoinCollectHandler();
            gm.PlaySoundOnPlayer(gm.coinSound);
            Destroy(collision.gameObject);
        }
        else if (collision.gameObject.CompareTag("PowerUp"))
        {
            gm.PlaySoundOnPlayer(gm.powerUpSound);
            Destroy(collision.gameObject);

            if (lastPowerupHandler != null)
            {
                StopCoroutine(lastPowerupHandler);
            }

            lastPowerupHandler = StartCoroutine(PowerupCollectHandler());
        }
        else if (collision.gameObject.CompareTag("Exit"))
        {
            gm.ExitHandler();
            Destroy(collision.gameObject);
        }
    }
}
