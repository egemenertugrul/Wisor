using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IPD : MonoBehaviour
{
    [Range(0.050f, 0.075f)]
    public float Distance = 0.064f;
    [Range(0.0f, 0.1f)]
    public float OffsetX = 0.088f;
    [Range(0.0f, 0.5f)]
    public float OffsetY = 0.0f;

    private CustomCameraCapture[] eyes;
    private CustomCameraCapture leftEye;
    private CustomCameraCapture rightEye;

    void Start()
    {
        eyes = GetComponentsInChildren<CustomCameraCapture>(false);
        if (eyes.Length == 2)
        {
            leftEye = eyes[0];
            rightEye = eyes[1];
        }
    }

    void Update()
    {
        if (!leftEye || !rightEye)
            return;

        float halfDistance = Distance / 2;

        leftEye.transform.localPosition = new Vector3(-halfDistance, 0, 0);
        rightEye.transform.localPosition = new Vector3(halfDistance, 0, 0);

        leftEye.DistortionMaterial.SetFloat("_offsetX", -OffsetX);
        leftEye.DistortionMaterial.SetFloat("_offsetY", OffsetY);

        rightEye.DistortionMaterial.SetFloat("_offsetX", OffsetX);
        rightEye.DistortionMaterial.SetFloat("_offsetY", OffsetY);
    }
}
