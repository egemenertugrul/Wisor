using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SLAMTarget : MonoBehaviour
{
    public float PositionDiffThreshold = 0.2f;
    private Vector3 positionOffset, lastReceivedTargetPosition;

    public void SetPosition(Vector3 targetPosition)
    {
        //var diff = (lastReceivedTargetPosition - targetPosition);
        //lastReceivedTargetPosition = targetPosition;

        //if (diff.magnitude > PositionDiffThreshold)
        //{
        //    positionOffset = transform.position;
        //    print(string.Format("Difference is larger than: {0}", PositionDiffThreshold));
        //}

        //transform.position = positionOffset + targetPosition;
        transform.localPosition = targetPosition;
    }

    public void SetRotation(Quaternion rotation)
    {
        transform.localRotation = rotation;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
