using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class VideoStreamer : MonoBehaviour
{
	public int Width = 1280, Height = 720;

	//public CustomCameraCapture camCap;
	public string Pipeline;

	public int fps = 30;

	public bool _created = false;
    private CustomCameraCapture[] cameraCaptures;
    GstCustomVideoStreamer _streamer;

	public RenderTexture _tempRenderTarget;
	public Texture2D TempTex;

	public GstUnityImageGrabber _grabber;
    private bool _prepared;
    private bool _setResolution = true;

    public bool HasData { get; private set; }

	void OnEnable()
	{
		cameraCaptures = GetComponentsInChildren<CustomCameraCapture>();
		cameraCaptures[cameraCaptures.Length - 1].OnPostRenderEvent.AddListener(Execute);
		PrepareTexture();
	}

	void OnDisable()
	{
		DestroyTexture();
	
		if(cameraCaptures.Length > 0)
			cameraCaptures[cameraCaptures.Length - 1].OnPostRenderEvent.RemoveListener(Execute);
	}

	void Destroy()
	{
		_streamer.SetGrabber(null);
		_streamer.Pause();
		Thread.Sleep(100);
		_streamer.Stop();
		_streamer.Close();

		_grabber.Destroy();
		DestroyTexture();

		_created = false;
		return;
	}

	void PrepareTexture()
	{
		if (_prepared) return;

		_tempRenderTarget = RenderTexture.GetTemporary(Width, Height);
		foreach (var cap in cameraCaptures)
		{
			cap.SetRenderTarget(_tempRenderTarget);
		}
		_prepared = true;
	}

	void DestroyTexture()
	{
		//var cameras = GetComponentsInChildren<Camera>();

		// Release the temporary render target.
		foreach (var cap in cameraCaptures)
		{
			var camera = cap.camera;
			if (_tempRenderTarget != null && _tempRenderTarget == camera.targetTexture)
			{
				camera.targetTexture = null;
				RenderTexture.ReleaseTemporary(_tempRenderTarget);
				_tempRenderTarget = null;
			}
		}
		
		_prepared = false;
	}

	// Use this for initialization
	void Start()
	{
		Debug.Log("start");

		_streamer = new GstCustomVideoStreamer();
		_grabber = new GstUnityImageGrabber();

		_streamer.SetGrabber(_grabber);
		_streamer.SetPipelineString(Pipeline);
		_streamer.SetResolution(Width, Height, fps);
	}

	void OnDestroy()
	{
		Destroy();
	}

	void Update()
    {
        if (!_created && _grabber != null && HasData)
        {
            //important to create stream after data is confirmed
            _streamer.CreateStream();
            _streamer.Stream();
            _created = true;
        }

    }

    private void Execute()
    {
        if (_prepared)
        {
            var tempRT = RenderTexture.GetTemporary(Width, Height);
            //Graphics.Blit(source, tempRT);
            if (TempTex == null)
            {
                TempTex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            }

            //RenderTexture.active = _tempRenderTarget;
            Graphics.Blit(_tempRenderTarget, tempRT);

            TempTex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
            TempTex.Apply();
            _grabber.SetTexture2D(TempTex);
            _grabber.Update();
            HasData = true;

            //Destroy(tempTex);
            RenderTexture.ReleaseTemporary(tempRT);

            //RenderTexture.active = null;
        }
    }
}
