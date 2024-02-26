using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void PlayedLookAroundAnimation()
    {
        animator.SetInteger("i", 1);
    }

    public void PlayedWaitingAnimation()
    {
        animator.SetInteger("i", 0);
    }
    
}
