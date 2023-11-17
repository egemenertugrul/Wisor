using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wisor.Demo
{
    public class EgemenInteractionDemo : MonoBehaviour
    {
        [SerializeField] HMD_Raycaster raycaster;
        private Animator animator;

        void Start()
        {
            animator = GetComponent<Animator>();
            raycaster.OnHit.AddListener((gameObject) =>
            {
                if (gameObject.GetInstanceID() != this.gameObject.GetInstanceID())
                    return;
                animator.SetTrigger("Wave");
            });
        }

        void Update()
        {

        }
    }
}