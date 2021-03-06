﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public enum PlayerState
{
    walk, attack, interact, pathfinding
}

public class PlayerMovement : MonoBehaviour
{
    public PlayerState      currentState;
    public float            speed;
    private Rigidbody2D     myRigidbody;
    private Vector3         change;
    private Animator        animator;
    private C2Client        client;

    public int Level { get; set; } = 1;
    public int Exp { get; set; } = 0;

    [SerializeField] private Stat       hp;
    [SerializeField] private Stat       mp;
    [SerializeField] private Portrait   portrait;

    void Start()
    {
        hp.Initialize(200, 200);
        mp.Initialize(200, 200);
        portrait.SetLevel(5); 

        currentState    = PlayerState.walk;
        animator        = GetComponent<Animator>();
        myRigidbody     = GetComponent<Rigidbody2D>();

        animator.SetFloat("moveX", 0);
        animator.SetFloat("moveY", -1);
    }

    // Update is called once per frame
    void Update()
    {
        if (UIManager.Instance.CurrentState != UIState.Play)
            return;

        //// hp bar test
        //if (Input.GetKeyDown(KeyCode.I))
        //{
        //    hp.CurrentValue -= 10;
        //    mp.CurrentValue -= 10;
        //}
        //if (Input.GetKeyDown(KeyCode.O))
        //{
        //    hp.CurrentValue += 10;
        //    mp.CurrentValue += 10;
        //}


        change = Vector3.zero;
        if(Input.GetKeyDown(KeyCode.UpArrow) == true)
        {
            change.y = +0.8f;
        }
        else if(Input.GetKeyDown(KeyCode.DownArrow) == true)
        {
            change.y = -0.8f;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) == true)
        {
            change.x = -.8f;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) == true)
        {
            change.x = +.8f;
        }



        // path finding 우선.
        if (currentState != PlayerState.attack && Input.GetButtonDown("attack"))
        {
            StartCoroutine(AttackCo());
        }
        else if (currentState == PlayerState.walk)
        {
            UpdateAnimatorAndMove();
        }
    }



    private IEnumerator AttackCo()
    {
        animator.SetBool("attacking", true);
        currentState = PlayerState.attack;

        yield return null;

        animator.SetBool("attacking", false);

        yield return new WaitForSeconds(.3f);
        currentState = PlayerState.walk;
    }

    private void UpdateAnimatorAndMove()
    {
        if (change != Vector3.zero)
        {
            MoveCharacter();
            animator.SetFloat("moveX", change.x);
            animator.SetFloat("moveY", change.y);
            animator.SetBool("moving", true);
        }
        else
        {
            animator.SetBool("moving", false);
        }
    }

    public void ParseLoginPacket( sc_packet_login_ok payload )
    {

    }

    void MoveCharacter()
    {
        //change.Normalize();
        myRigidbody.MovePosition(transform.position + change);
    }

    public void MoveCharacterUsingServerPostion(int y, int x)
    {
        Vector3 vector = new Vector3();
        vector.x = x;
        vector.y = y;
        Debug.Log($"move server postion x {x}, y {y}");
        myRigidbody.MovePosition(vector);
    }

    public void SetHP(int minHp, int maxHp)
    {
        hp.Initialize(minHp, maxHp);
    }

    public void SetLevel(int level)
    {
        //portrait.
    }
}
