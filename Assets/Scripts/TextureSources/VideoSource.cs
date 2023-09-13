using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace OpenWiXR.Texturing
{
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoSource : TextureSource
    {
        private VideoPlayer videoPlayer;

        void Awake()
        {
            videoPlayer = GetComponent<VideoPlayer>();
            videoPlayer.sendFrameReadyEvents = true;

            videoPlayer.frameReady += VideoPlayer_frameReady;
            videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;
        }

        private void VideoPlayer_prepareCompleted(VideoPlayer source)
        {
        }

        private void VideoPlayer_frameReady(VideoPlayer source, long frameIdx)
        {
            //texture = source.texture;
            if (!texture)
            {
                //texture = videoPlayer.texture;
                texture = new Texture2D(source.texture.width, source.texture.height, TextureFormat.RGB24, false);
            }


            Rect rectReadPicture = new Rect(0, 0, Width, Width);
            RenderTexture prevTex = RenderTexture.active;
            RenderTexture.active = videoPlayer.targetTexture;

            ((Texture2D)texture).ReadPixels(rectReadPicture, 0, 0);
            ((Texture2D)texture).Apply();

            RenderTexture.active = prevTex;
        }

        override public bool IsReady()
        {
            return videoPlayer.isPrepared;
        }

        public override Color32[] GetData()
        {
            if (texture == null)
            {
                return null;
            }

            Color32[] data = ((Texture2D)texture).GetPixels32();
            return data;
        }

        //public void NextFrame()
        //{
        //    videoPlayer.frame++;
        //    videoPlayer.Play();
        //    videoPlayer.Pause();
        //}

        void Update()
        {

        }

    }
}