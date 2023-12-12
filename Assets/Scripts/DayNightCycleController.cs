using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycleController : MonoBehaviour
{
    [Range(0, 24)] //24 Hours Cycle
    [SerializeField] float orbitSpeed = 1.0f;
    public float timeOfDay;
    public Light moon;
    private bool isNight;

    public Light sun1;
    public Light sun2;
    private void Update()
    {
        timeOfDay += Time.deltaTime * orbitSpeed;
        if (timeOfDay > 24)
            timeOfDay = 0;
        UpdateTime();
    }
    private void OnValidate()
    {
        UpdateTime();
    }
    private void Start()
    {
        sun2.enabled = false;
        StartDay1();
    }
    private void UpdateTime()
    {
        float alpha = timeOfDay / 24.0f;
        float sun1Rotation = Mathf.Lerp(-90, 270, alpha);
        float moonRotation = sun1Rotation - 130;
        float sun2Rotation = sun1Rotation + 60;
        sun1.transform.rotation = Quaternion.Euler(sun1Rotation, 0, 0);
        sun2.transform.rotation = Quaternion.Euler(sun2Rotation, 0, 0);
        moon.transform.rotation = Quaternion.Euler(moonRotation, 0, 0);

        CheckNightDayTransition();
    }
    private void CheckNightDayTransition()
    {
        if (isNight)
        {
            if(moon.transform.rotation.eulerAngles.x > 180)
            {
                StartDay2();
            }
        }
        else
        {
            if (sun1.transform.rotation.eulerAngles.x > 180)
            {
                StartNight();
            }
        }
    }
    private void StartDay1()
    {
        isNight = false;
        sun1.shadows = LightShadows.Soft;
        //sun2.shadows = LightShadows.None;
        moon.shadows = LightShadows.None;
    }
    private void StartNight ()
    {
        isNight = true;
        sun2.enabled = true;
        moon.shadows = LightShadows.Soft;
        sun1.shadows = LightShadows.None;
        sun2.shadows = LightShadows.None;
    }
    private void StartDay2 ()
    {
        isNight = false;
        sun1.enabled = false;
        sun2.shadows = LightShadows.Soft;
        //sun1.shadows = LightShadows.None;
        moon.shadows = LightShadows.None;
    }
}
