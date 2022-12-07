using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IPD : MonoBehaviour
{
    [Range(0.050f, 0.075f)]
    public float Distance = 0.064f;
    //public float InnerRotation = 6.81f;
    [Range(0.05f, 0.1f)]
    public float OffsetX = 0.088f;
    public bool IsAuto = true;

    private const double MagicConvergenceDistance = 0.263775356161;

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
        rightEye.DistortionMaterial.SetFloat("_offsetX", OffsetX);

        //if (IsAuto)
        //{
        //    InnerRotation = Mathf.Atan2(halfDistance, (float)MagicConvergenceDistance) * Mathf.Rad2Deg;
        //}
        //var leftEuler = leftEye.transform.localRotation.eulerAngles;
        //leftEye.transform.localRotation = Quaternion.Euler(leftEuler.x, -InnerRotation, leftEuler.z);

        //var rightEuler = rightEye.transform.localRotation.eulerAngles;
        //rightEye.transform.localRotation = Quaternion.Euler(rightEuler.x, InnerRotation, rightEuler.z);
    }
}
