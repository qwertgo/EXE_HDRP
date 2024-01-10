using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

public class DayNightCycleController : MonoBehaviour
{
    [SerializeField] private float sunStartRotation;
    [SerializeField] private float sunDestinationRotation;
    [SerializeField] private float moonStartRotation;
    [SerializeField] private float moonDestinationRotation;

    [SerializeField] private Color startColor;
    [SerializeField] private Color endColor;

    public float gameTime = 60;

    [SerializeField] private Light sun;
    [SerializeField] private Light moon;

    private void Update()
    {
        UpdateTime();
    }

    private void UpdateTime()
    {
        float t = Mathf.Min(Time.time / gameTime, 1);

        Color lerpedColor = Color.Lerp(startColor, endColor, t);
        moon.color = lerpedColor;

        /*float lerpedFogDistance = Mathf.Lerp(startFogDistance, endFogDistance, t);
        HDRISky hDRISky;
        if (globalVolume.profile.TryGet(out hDRISky))
        {
            hDRISky.groundLevel.value = lerpedFogDistance;
        }*/


        float sunRotation = Mathf.Lerp(sunStartRotation, sunDestinationRotation, t);
        float moonRotation = Mathf.Lerp(moonStartRotation, moonDestinationRotation, t);

        sun.transform.rotation = Quaternion.Euler(sunRotation, 0, 0);
        moon.transform.rotation = Quaternion.Euler(moonRotation, 0, 0);
    }
}