using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class SpeedOMeter : MonoBehaviour
{
    [Header("Camera")] 
    [SerializeField] private float fovMin;
    [SerializeField] private float fovMax;
    [SerializeField] private Vector2 cameraZOffset;
    [SerializeField] private Vector2 cameraYOffset;

    [Header("WaterVFX")]
    [SerializeField] private Material waterVFXMaterial;
    [SerializeField] private Vector2 waterVfxHeight;
    [SerializeField] private Vector2 waterVfxStretch;

    [Header("Audio")]
    [SerializeField] private AudioSource windAudioSource;
    [SerializeField] private Vector2 windVolume;

    private float lerpedSpeedT = 0;
    
    private PlayerController player => GameVariables.instance.player;
    private CinemachineVirtualCamera virtualCamera => GameVariables.instance.virtualCamera;
    private CinemachineTransposer cameraTransposer;

    private void Start()
    {
        cameraTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
    }

    private void Update()
    {
        float speedT = player.rb.velocity.magnitude / player.baseMaxSpeed;

        float k = 1f - Mathf.Pow(.25f, Time.deltaTime);
        lerpedSpeedT = Mathf.Lerp(lerpedSpeedT, speedT, k);
        
        //camera
        virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(fovMin, fovMax, speedT);

        float currentZOffset = Mathf.Lerp(cameraZOffset.x, cameraZOffset.y, speedT);
        float currentYOffset = Mathf.Lerp(cameraYOffset.x, cameraYOffset.y, speedT);
        cameraTransposer.m_FollowOffset = new Vector3(0, currentYOffset, currentZOffset);

        //waterVFX
        float waterHeight = Mathf.Lerp(waterVfxHeight.x, waterVfxHeight.y, lerpedSpeedT);
        waterVFXMaterial.SetFloat("_Height", waterHeight);

        float waterStretch = Mathf.Lerp(waterVfxStretch.x, waterVfxStretch.y, lerpedSpeedT);
        waterVFXMaterial.SetFloat("_Stretch", waterStretch);
        
        //Sound
        windAudioSource.volume = Mathf.Lerp(windVolume.x, windVolume.y, speedT * speedT);
    }

    private void OnDestroy()
    {
        waterVFXMaterial.SetFloat("_Height", 1);
        waterVFXMaterial.SetFloat("_Stretch", 1);
    }
}
