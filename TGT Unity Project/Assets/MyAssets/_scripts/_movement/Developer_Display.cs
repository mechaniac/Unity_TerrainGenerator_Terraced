using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Developer_Display : MonoBehaviour
{
    public TMP_Text frameRateText;
    public float updateRate = 0.5f; // Update the frame rate display every 0.5 seconds

    private float nextUpdate = 0f;
    private float fps = 0f;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
    }
    void Update()
    {
        // Calculate frame rate
        fps = 1f / Time.deltaTime;

        // Update frame rate display every 'updateRate' seconds
        if (Time.time > nextUpdate)
        {
            nextUpdate += updateRate;
            frameRateText.text = "FPS: " + Mathf.Round(fps);
        }
    }
}
