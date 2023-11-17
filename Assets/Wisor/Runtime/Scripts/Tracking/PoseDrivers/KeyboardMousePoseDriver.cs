using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Wisor.Communications;
using static Wisor.Communications.WebSocketsClient;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace Wisor.Tracking
{
    public sealed class KeyboardMousePoseDriver : PoseDriver
    {
        private bool isInitialized = false;

        public void Initialize()
        {
            isInitialized = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            //UpdateRotation(Quaternion.Euler(new Vector3(0, 0, 0)));
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            Vector2 mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            
            float sensitivity = 5.0f;

            // -- Rotation

            Vector3 rotation = target.rotation.eulerAngles;
            rotation.y += mouseMovement.x * sensitivity;
            rotation.x -= mouseMovement.y * sensitivity;

            Quaternion targetRotation = Quaternion.Euler(rotation);
            UpdateRotation(targetRotation);

            // -- Translation

            float moveSpeed = 5.0f;

            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            bool down = Input.GetKey(KeyCode.Q);
            bool up = Input.GetKey(KeyCode.E);
            float moveUpDown = 0f;
            if (down)
            {
                moveUpDown = -1;
            }
            else if(up)
            {
                moveUpDown = 1;
            }

            Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput).normalized;

            moveDirection = target.TransformDirection(moveDirection);
            moveDirection.y += moveUpDown;
            TranslatePosition(moveDirection * moveSpeed * Time.deltaTime);

        }
    }
}