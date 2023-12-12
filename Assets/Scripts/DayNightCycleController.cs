using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DayNightCycleController : MonoBehaviour
{
    [SerializeField] private float sunStartRotation;
    [SerializeField] private float sunDestinationRotation;
    [SerializeField] private float moonStartRotation;
    [SerializeField] private float moonDestinationRotation;
    
    // private bool isNight;
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
        
        float sunRotation = Mathf.Lerp(sunStartRotation, sunDestinationRotation, t);
        float moonRotation = Mathf.Lerp(moonStartRotation, moonDestinationRotation, t);
        
        sun.transform.rotation = Quaternion.Euler(sunRotation, 0, 0);
        moon.transform.rotation = Quaternion.Euler(moonRotation, 0, 0);
    }
    // private void CheckNightDayTransition()
    // {
    //     if (sun1.transform.eulerAngles.x > 180 && !isNight && sun1.enabled)
    //     {
    //         StartNight();
    //     }
    //     else if (moon.transform.eulerAngles.x > 180 && isNight)
    //     {
    //         StartDay2();
    //     }
    // }
    // private void StartDay1()
    // {
    //     Debug.Log("StartDay 1");
    //     isNight = false;
    //     sun1.shadows = LightShadows.Soft;
    //     moon.shadows = LightShadows.None;
    // }
    // private void StartNight ()
    // {
    //     Debug.Log("Start Night");
    //     isNight = true;
    //     sun2.enabled = true;
    //     sun1.enabled = false;
    //     moon.shadows = LightShadows.Soft;
    //     sun1.shadows = LightShadows.None;
    //     sun2.shadows = LightShadows.None;
    // }
    // private void StartDay2 ()
    // {
    //     Debug.Log("Start Day 2");
    //     isNight = false;
    //     sun2.shadows = LightShadows.Soft;
    //     moon.shadows = LightShadows.None;
    // }
}
