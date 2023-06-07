using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

public abstract class TextureSource : MonoBehaviour
{
    public UnityEvent ReadyEvent = new UnityEvent();
    protected bool isReady = false;

    public Texture texture;
    public int Width { get => texture.width; }
    public int Height { get => texture.height; }

    public abstract Color32[] GetData();

    public abstract bool IsReady();
}