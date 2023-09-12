using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SLAMTarget : MonoBehaviour
{
    public void SetPosition(Vector3 targetPosition)
    {
        transform.localPosition = targetPosition;
    }

    public void SetRotation(Quaternion rotation)
    {
        transform.localRotation = rotation;
    }

    void Update()
    {

    }
}