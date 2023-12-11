using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameVariables : MonoBehaviour
{
    public static GameVariables instance;

    public PlayerController player;
    public bool isPaused;
    public UnityEvent onPause;
    public UnityEvent onUnpause;

    private void Awake()
    {
        if (instance)
            Destroy(this);
        else
            instance = this;
    }
    
}
