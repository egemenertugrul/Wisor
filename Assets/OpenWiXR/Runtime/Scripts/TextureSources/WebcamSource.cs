using UnityEngine;

namespace OpenWiXR.Texturing
{
    public class WebcamSource : TextureSource
    {
        private WebCamTexture webcamTexture;

        void Awake()
        {
            texture = new WebCamTexture();
            webcamTexture = texture as WebCamTexture;

            print(string.Format("Active webcam: {0}", webcamTexture.deviceName));

            webcamTexture.Play();

            //Color32[] m_Pixels = webcamTexture.GetPixels32();
            //textureHandle = GCHandle.Alloc(m_Pixels, GCHandleType.Pinned);

            //textureHandle = webcamTexture.GetNativeTexturePtr();

            Renderer previewRenderer = GetComponentInChildren<Renderer>();
            if (previewRenderer)
                previewRenderer.material.mainTexture = texture;

        }

        override public bool IsReady()
        {
            return webcamTexture.isPlaying;
        }

        void Update()
        {

        }

        public override Color32[] GetData()
        {
            return webcamTexture.GetPixels32();
        }
    }
}