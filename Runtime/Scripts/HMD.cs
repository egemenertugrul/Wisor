using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wisor
{
    public class HMD : MonoBehaviour
    {
        [Range(0.040f, 0.075f)]
        public float IPDDistance = 0.064f;
        [Range(0, 180)]
        public float FOV = 90f;

        [Range(0.0f, 0.1f)]
        public float OffsetX = 0.088f;
        [Range(0.0f, 0.5f)]
        public float OffsetY = 0.0f;

        private Eye[] eyes;
        private Eye leftEye;
        private Eye rightEye;

        void Start()
        {
            eyes = GetComponentsInChildren<Eye>(false);
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

            float halfDistance = IPDDistance / 2;

            leftEye.camera.fieldOfView = FOV;
            rightEye.camera.fieldOfView = FOV;

            leftEye.transform.localPosition = new Vector3(-halfDistance, 0, 0);
            rightEye.transform.localPosition = new Vector3(halfDistance, 0, 0);

            leftEye.DistortionMaterial.SetFloat("_offsetX", -OffsetX);
            leftEye.DistortionMaterial.SetFloat("_offsetY", OffsetY);

            rightEye.DistortionMaterial.SetFloat("_offsetX", OffsetX);
            rightEye.DistortionMaterial.SetFloat("_offsetY", OffsetY);
        }
    }
}