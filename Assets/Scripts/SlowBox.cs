using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowBox : MonoBehaviour
{
    private PlayerController player;
    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        player = GameVariables.instance.player;

    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag.Equals("Player"))
            player.SlowDown();
    }

    // private void OnTriggerExit(Collider other)
    // {
    //     if(other.tag.Equals("Player"))
    //         player.ReturnToDefaultSpeed();
    // }
}
