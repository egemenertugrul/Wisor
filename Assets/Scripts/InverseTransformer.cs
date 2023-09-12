using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseTransformer : MonoBehaviour
{
    public Transform target;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!target)
            return;

        Matrix4x4 targetMat = new Matrix4x4();
        targetMat.SetTRS(target.position, target.rotation, Vector3.one);
        Matrix4x4 invMat = targetMat.inverse;

        transform.position = invMat.GetPosition();
        transform.rotation = invMat.rotation;
    }
}
