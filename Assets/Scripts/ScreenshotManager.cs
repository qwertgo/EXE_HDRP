using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenshotManager : MonoBehaviour
{
    [SerializeField] int ScreenshotSize = 3;
    private int screenshotNumber = 1;
    private string screenshotName = "screenshot.png";
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            string screenshot = screenshotNumber.ToString() + screenshotName;
            ScreenCapture.CaptureScreenshot(screenshot, ScreenshotSize);
            screenshotNumber++;
        }
    }
}
