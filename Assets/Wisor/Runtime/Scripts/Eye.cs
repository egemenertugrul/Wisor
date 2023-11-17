using UnityEngine;
using UnityEngine.Events;

namespace Wisor
{
    [RequireComponent(typeof(Camera))]
    public class Eye : MonoBehaviour
    {
        public enum Side { Left, Right }
        public Side side;

        public Camera camera;
        [ReadOnly] public RenderTexture rt;

        //private Texture2D _tempTex;
        private int width;
        private int height;
        [ReadOnly] public RenderTexture tempRT;

        public UnityEvent OnPostRenderEvent = new UnityEvent();
        private Rect previousCameraViewportRect;

        public Material DistortionMaterial;

        public void SetRenderTarget(RenderTexture renderTex)
        {
            rt = renderTex;

            width = rt.width / 2;
            height = rt.height;

            if (tempRT != null)
                RenderTexture.ReleaseTemporary(tempRT);

            tempRT = RenderTexture.GetTemporary(width, height);

            if (!camera)
                camera = GetComponent<Camera>();

            //camera.targetTexture = tempRT;
        }

        private void OnEnable()
        {
            camera = GetComponent<Camera>();
            //camera.targetTexture = tempRT;

            previousCameraViewportRect = new Rect(camera.rect);
        }

        private void OnDisable()
        {
            camera.targetTexture = null;

            RenderTexture.ReleaseTemporary(tempRT);
            tempRT = null;
        }

        private void OnPreRender()
        {
            previousCameraViewportRect = new Rect(camera.rect);

            //if(rt)
            //    camera.rect = new Rect(0, 0, 1, 1);

            //camera.targetTexture = tempRT;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (DistortionMaterial)
                Graphics.Blit(source, tempRT, DistortionMaterial);
            else
                Graphics.Blit(source, tempRT);

            //if (_tempTex == null)
            //{
            //	_tempTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            //}

            if (rt)
            {
                int offsetX = 0;
                if (side == Side.Right)
                    offsetX = width;
                Graphics.CopyTexture(tempRT, 0, 0, 0, 0,
                    tempRT.width, tempRT.height, rt, 0, 0, offsetX, 0);
            }

            //Graphics.Blit(tempRT, destination);
            Graphics.Blit(source, destination);

        }

        private void OnPostRender()
        {
            //camera.rect = previousCameraViewportRect;

            //camera.targetTexture = null;

            OnPostRenderEvent.Invoke();
        }
    }
}