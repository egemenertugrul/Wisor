using System;
using UnityEngine;

namespace OpenWiXR.Tracking
{
    public abstract class PoseDriver : MonoBehaviour
    {
        [SerializeField] protected Transform target;

        public void UpdateRotation(Quaternion rotation)
        {
            if (!target)
                Debug.LogError("PoseDriver target is undefined.");
            target.localRotation = rotation;
        }

        public void UpdatePositionAndRotation(Vector3 position, Quaternion rotation)
        {
            if (!target)
                Debug.LogError("PoseDriver target is undefined.");
            target.localPosition = position;
            target.localRotation = rotation;
        }

        public void SetTarget(Transform poseDriverTarget)
        {
            target = poseDriverTarget;
        }
    }
}