using UnityEngine;
using UnityEngine.Events;

namespace OpenWiXR.Texturing
{
    public abstract class TextureSource : MonoBehaviour
    {
        public UnityEvent ReadyEvent = new UnityEvent();
        protected bool isReady = false;

        protected Texture texture;
        public int Width { get => texture.width; }
        public int Height { get => texture.height; }

        public abstract Color32[] GetData();

        public abstract bool IsReady();
    }
}