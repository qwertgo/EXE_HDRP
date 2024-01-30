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
    [SerializeField] private float timeForSunToSet = 10;
    private float sunsetSpeed;
    private float sunStartIntensity;

    [SerializeField] private float sunDestinationRotation;
    [SerializeField] private float moonDestinationRotation;
    private Quaternion sunStartRotation;
    private Quaternion sunEndRotation;
    private Quaternion moonStartRotation;
    private Quaternion moonEndRotation;

    [FormerlySerializedAs("startColor")][SerializeField] private Color moonColorStart;
    [FormerlySerializedAs("endColor")][SerializeField] private Color moonColorEnd;

    [SerializeField] private Light sun;
    [SerializeField] private Light moon;


    private void Start()
    {
        sunStartRotation = sun.transform.rotation;
        sunEndRotation = Quaternion.Euler(sunDestinationRotation, 0, 0);

        moonStartRotation = moon.transform.rotation;
        moonEndRotation = Quaternion.Euler(moonDestinationRotation, 0, 0);

        sunsetSpeed = 1 / timeForSunToSet;
        sunStartIntensity = sun.intensity;
        StartCoroutine(Sunset());

       


    }

    IEnumerator Sunset()
    {
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * sunsetSpeed;
            SetSunsetValues(t);
            yield return null;
        }

        SetSunsetValues(1);
    }

    private void SetSunsetValues(float t)
    {
        Color lerpedColor = Color.Lerp(moonColorStart, moonColorEnd, t);
        moon.color = lerpedColor;

        float sunIntensityT = (t - .5f) * 2;
        sunIntensityT = Mathf.Clamp01(sunIntensityT);
        sun.intensity = Mathf.Lerp(sunStartIntensity, 0, sunIntensityT);

        sun.transform.rotation = Quaternion.Lerp(sunStartRotation, sunEndRotation, t);
        moon.transform.rotation = Quaternion.Lerp(moonStartRotation, moonEndRotation, t);
    }

    
}