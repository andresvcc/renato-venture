using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    public CharacterController2D controller;
    public Animator animator;

    public float runSpeed = 40f;

    float horizontalMove = 0f;
    float verticalInput = 0f;
    bool jump = false;
    bool jumpHeld = false;
    bool axisUp = false;
    bool dash = false;

    void Update()
    {

        horizontalMove = Input.GetAxisRaw("Horizontal") * runSpeed;
        float axisVertical = Input.GetAxisRaw("Vertical");
        verticalInput = axisVertical;
        axisUp = axisVertical > 0f;
        jumpHeld = Input.GetKey(KeyCode.Space);

        animator.SetFloat("Speed", Mathf.Abs(horizontalMove));

        if (controller != null && controller.inWater && jumpHeld)
        {
            verticalInput = 1f;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jump = true;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            dash = true;
        }

        /*if (Input.GetAxisRaw("Dash") == 1 || Input.GetAxisRaw("Dash") == -1) //RT in Unity 2017 = -1, RT in Unity 2019 = 1
{
    if (dashAxis == false)
    {
        dashAxis = true;
        dash = true;
    }
}
else
{
    dashAxis = false;
}
*/

    }

    public void OnFall()
    {
        animator.SetBool("IsJumping", true);
    }

    public void OnLanding()
    {
        animator.SetBool("IsJumping", false);
    }

    void FixedUpdate()
    {
        // On continue à envoyer horizontalMove * Time.fixedDeltaTime pour garder la compatibilité
        controller.Move(horizontalMove * Time.fixedDeltaTime, jump, dash, verticalInput, jumpHeld, axisUp);
        jump = false;
        dash = false;
  
    }
}
