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
    public CinemachineVirtualCamera virtualCamera;
    public GameTimer gameTimer;
    public RectTransform timeSlider;
    public int fireflyCount;
    // public float globalVolume = 1;

    [HideInInspector] public UnityEvent onPause;
    [HideInInspector] public UnityEvent onUnpause;
    [HideInInspector] public Camera cam;
    
    private Transform debugSphereTransform;

    public bool isPaused;
    private void Awake()
    {
        if (instance)
            Destroy(this);
        else
            instance = this;
        
        cam = Camera.main;
    }
}
