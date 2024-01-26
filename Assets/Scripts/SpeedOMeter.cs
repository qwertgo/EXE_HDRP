using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class SpeedOMeter : MonoBehaviour
{
    [SerializeField] private float lerpSpeed = .9f;
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
    [SerializeField] private AudioSource calmWindAudioSource;
    [SerializeField] private AudioSource harshWindAudioSource;
    [SerializeField] private Vector2 windVolume;

    private float lerpedT = 0;
    
    private PlayerController player => GameVariables.instance.player;
    private CinemachineVirtualCamera virtualCamera => GameVariables.instance.virtualCamera;
    private CinemachineTransposer cameraTransposer;

    private void Start()
    {
        cameraTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
    }

    private void Update()
    {
        float t = Mathf.Max(player.rb.velocity.magnitude - player.baseMaxSpeed , 0) / (player.boostForce * .25f);

        float k = 1f - Mathf.Pow(lerpSpeed, Time.deltaTime);
        lerpedT = Mathf.Lerp(lerpedT, t, k);
        float lerpInvertedT = -lerpedT + 1;
        
        //camera
        virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(fovMin, fovMax, lerpedT);

        float currentZOffset = Mathf.Lerp(cameraZOffset.x, cameraZOffset.y, lerpedT);
        float currentYOffset = Mathf.Lerp(cameraYOffset.x, cameraYOffset.y, lerpedT);
        cameraTransposer.m_FollowOffset = new Vector3(0, currentYOffset, currentZOffset);

        //waterVFX
        float waterHeight = Mathf.Lerp(waterVfxHeight.x, waterVfxHeight.y, lerpedT);
        waterVFXMaterial.SetFloat("_Height", waterHeight);

        float waterStretch = Mathf.Lerp(waterVfxStretch.x, waterVfxStretch.y, lerpedT);
        waterVFXMaterial.SetFloat("_Stretch", waterStretch);
        
        //Sound
        calmWindAudioSource.volume =  lerpInvertedT;
        harshWindAudioSource.volume = lerpedT;
    }

    private void OnDestroy()
    {
        waterVFXMaterial.SetFloat("_Height", 1);
        waterVFXMaterial.SetFloat("_Stretch", 1);
    }
}
