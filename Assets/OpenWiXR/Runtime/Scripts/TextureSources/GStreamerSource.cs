using UnityEngine;

namespace OpenWiXR.Texturing
{
    public class GStreamerSource : TextureSource
    {
        //public RenderTexture renderTexture;
        public BaseVideoPlayer videoPlayer;
        private Texture2D texture2D;
        private RenderTexture renderTexture;

        private Texture2D GetT2D(UnityEngine.Texture tex)
        {
            UnityEngine.Texture mainTexture = tex;

            if (!texture2D)
            {
                texture2D = new Texture2D(mainTexture.width, mainTexture.height, TextureFormat.RGB24, false);
            }

            RenderTexture currentRT = RenderTexture.active;

            if (!renderTexture)
            {
                renderTexture = new RenderTexture(mainTexture.width, mainTexture.height, 32);
            }

            Graphics.Blit(mainTexture, renderTexture);

            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();

            RenderTexture.active = currentRT;

            return texture2D;
        }

        public override Color32[] GetData()
        {
            if (videoPlayer == null)
            {
                return null;
            }

            if (videoPlayer.VideoTexture == null)
            {
                return null;
            }
            var tex2d = GetT2D(videoPlayer.VideoTexture);
            Color32[] data = tex2d.GetPixels32();
            //Destroy(tex2d);
            texture = tex2d;

            return data;
        }

        public Texture2D GetTexture()
        {
            return GetT2D(videoPlayer.VideoTexture);
        }

        public byte[] GetTextureData()
        {
            return GetTexture().GetRawTextureData();
        }

        //public IntPtr GetDataPtr()
        //{
        //    return videoPlayer.VideoTexture.GetNativeTexturePtr();
        //    //var tex2d = GetT2D(videoPlayer.VideoTexture);
        //    //Color32[] data = tex2d.GetPixels32();
        //    //Destroy(tex2d);
        //    //texture = tex2d;

        //    //return data;
        //}

        public override bool IsReady()
        {
            return videoPlayer.VideoTexture != null;
        }

        void Start()
        {
            //StartCoroutine(FrameEndRoute());

        }

        void Update()
        {
            texture = videoPlayer.VideoTexture;
            if (IsReady() != isReady)
            {
                isReady = true;
                ReadyEvent.Invoke();
            }
        }
    }
}