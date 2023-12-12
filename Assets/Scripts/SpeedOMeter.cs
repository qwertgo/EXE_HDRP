using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class SpeedOMeter : MonoBehaviour
{
    [Header("Camera")] 
    [SerializeField] private float fovMin;
    [SerializeField] private float fovMax;
    [SerializeField] private float minOffset;
    [SerializeField] private float maxOffset;
    
    private PlayerController player => GameVariables.instance.player;
    private CinemachineVirtualCamera virtualCamera => GameVariables.instance.virtualCamera;
    private CinemachineTransposer cameraTransposer;

    private void Start()
    {
        cameraTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
    }

    void Update()
    {
        float speedT = player.rb.velocity.magnitude / player.baseMaxSpeed;

        virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(fovMin, fovMax, speedT);
        cameraTransposer.m_FollowOffset = new Vector3(0, 1.9f , Mathf.Lerp(minOffset, maxOffset, speedT));
    }
}
