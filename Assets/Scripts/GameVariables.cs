using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameVariables : MonoBehaviour
{
    public static GameVariables instance;

    public PlayerController player;

    private void Awake()
    {
        if (instance)
            Destroy(this);
        else
            instance = this;
    }
    
}
