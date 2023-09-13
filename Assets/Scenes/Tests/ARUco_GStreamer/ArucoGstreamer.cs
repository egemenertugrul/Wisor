using OpenWiXR.Texturing;
using System;
using UnityEngine;

namespace ArucoUnity.Cameras
{
    /// <summary>
    /// Captures images of a webcam.
    /// </summary>
    public class ArucoGstreamer : ArucoCamera
    {
        // Constants

        protected const int cameraId = 0;

        // Editor fields

        [SerializeField]
        [Tooltip("The id of the webcam to use.")]
        private int webcamId;

        public GStreamerSource GstSource;

        // IArucoCamera properties

        public override int CameraNumber { get { return 1; } }

        public override string Name { get; protected set; }

        // Properties

        /// <summary>
        /// Gets or set the id of the webcam to use.
        /// </summary>
        public int WebcamId { get { return webcamId; } set { webcamId = value; } }

        // MonoBehaviour methods

        /// <summary>
        /// Initializes <see cref="WebcamController"/> and subscribes to.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Unsubscribes to <see cref="WebcamController"/>.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        // ConfigurableController methods

        /// <summary>
        /// Calls <see cref="WebcamController.Configure"/> and sets <see cref="Name"/>.
        /// </summary>
        protected override void Configuring()
        {
            base.Configuring();
        }

        /// <summary>
        /// Calls <see cref="WebcamController.StartWebcams"/>.
        /// </summary>
        protected override void Starting()
        {
            base.Starting();
        }

        /// <summary>
        /// Calls <see cref="WebcamController.StopWebcams"/>.
        /// </summary>
        protected override void Stopping()
        {
            base.Stopping();
        }

        /// <summary>
        /// Blocks <see cref="ArucoCamera.OnStarted"/> until <see cref="WebcamController.IsStarted"/>.
        /// </summary>
        protected override void OnStarted()
        {
            if (!GstSource.IsReady())
            {
                GstSource.ReadyEvent.AddListener(() => WebcamController_Started(null));
            } else
            {
                WebcamController_Started(null);
            }

        }

        // ArucoCamera methods

        /// <summary>
        /// Copy current webcam images to <see cref="ArucoCamera.NextImages"/>.
        /// </summary>
        protected override bool UpdatingImages()
        {
            Array.Copy(GstSource.GetTextureData(), NextImageDatas[cameraId], ImageDataSizes[cameraId]);
            return true;
        }

        // Methods

        /// <summary>
        /// Configures <see cref="ArucoCamera.Textures"/> and calls <see cref="ArucoCamera.OnStarted"/>.
        /// </summary>
        protected virtual void WebcamController_Started(WebcamController webcamController)
        {
            var webcamTexture = GstSource.GetTexture();
            Textures[cameraId] = new Texture2D(webcamTexture.width, webcamTexture.height, webcamTexture.format, false);
            base.OnStarted();
        }
    }
}