using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Wisor
{
    public class HMD_Raycaster : MonoBehaviour
    {
        [SerializeField] public UnityEvent<GameObject> OnHit = new UnityEvent<GameObject>();
        [SerializeField] private LayerMask layerMask = ~0; // Everything by default
        [SerializeField] private bool showCrosshair = true;
        
        private GameObject crosshair;
        private Material crosshairMaterial;

        private void Start()
        {
            if (showCrosshair)
            {
                crosshair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crosshair.transform.parent = transform;
                crosshair.transform.localPosition = Vector3.forward * 1;
                crosshair.transform.localScale = Vector3.one * 0.045f;
                crosshairMaterial = new Material(Shader.Find("Unlit/Color"));
                crosshairMaterial.color = Color.red;
                crosshair.GetComponent<Renderer>().material = crosshairMaterial;
            }
        }

        void FixedUpdate()
        {
            RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.TransformDirection(Vector3.forward), Mathf.Infinity, layerMask);

            if(hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform.gameObject.Equals(crosshair))
                        continue;
                    if(crosshair)
                        crosshairMaterial.color = Color.yellow;
                    OnHit.Invoke(hit.transform.gameObject);
                }
            }
            else
            {
                if (crosshair)
                    crosshairMaterial.color = Color.white;
            }
        }
    }
}