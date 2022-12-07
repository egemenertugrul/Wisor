using System;
using System.Runtime.InteropServices;
using UnityEngine;

public abstract class TextureSource : MonoBehaviour
{
    public Texture texture;
    public int Width { get => texture.width; }
    public int Height { get => texture.height; }

    public abstract Color32[] GetData();

    public abstract bool IsReady();
}