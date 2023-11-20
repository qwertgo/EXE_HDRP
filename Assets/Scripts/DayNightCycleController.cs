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
    private void UpdateTime()
    {
        float alpha = timeOfDay / 24.0f;
        float sun1Rotation = Mathf.Lerp(-90, 270, alpha);
        float moonRotation = sun1Rotation - 160;
        float sun2Rotation = sun1Rotation + 30;
        sun2.transform.rotation = Quaternion.Euler(sun1Rotation, -150.0f, 0);
        sun2.transform.rotation = Quaternion.Euler(sun2Rotation, -150.0f, 0);
        moon.transform.rotation = Quaternion.Euler(moonRotation, +150.0f, 0);

        CheckNightDayTransition();
    }
    private void CheckNightDayTransition()
    {
        if (isNight)
        {
            if(moon.transform.rotation.eulerAngles.x > 180)
            {
                StartDay();
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
    private void StartDay ()
    {
        isNight = false;
        sun1.shadows = LightShadows.Soft;
        moon.shadows = LightShadows.None;
    }
    private void StartNight ()
    {
        isNight = true;
        moon.shadows = LightShadows.Soft;
        sun1.shadows = LightShadows.None;
    }
}
