using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class GameVariables : MonoBehaviour
{
    public static GameVariables instance;

    public PlayerController player;
    public EnemyMovement enemy;
    public DayNightCycleController dayNightCycleController;
    public CinemachineVirtualCamera virtualCamera;
    public GameTimer gameTimer;
    public UnityEvent onPause;
    public UnityEvent onUnpause;

    [FormerlySerializedAs("collectedFireflies")] public int fireflyCount;
    public bool isPaused;
    private void Awake()
    {
        if (instance)
            Destroy(this);
        else
            instance = this;
    }
    
}
