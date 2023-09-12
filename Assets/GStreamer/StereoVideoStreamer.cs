using OpenWiXR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class StereoVideoStreamer : MonoBehaviour
{
    [SerializeField] VideoStreamerConfig config;

    [ReadOnly][SerializeField] private bool _created = false;
    private CustomCameraCapture[] cameraCaptures;
    GstCustomVideoStreamer _streamer;

    [ReadOnly] public RenderTexture _tempRenderTarget;
	[ReadOnly] public Texture2D TempTex;

	public GstUnityImageGrabber _grabber;
    private bool _preparedTexture;
    private bool _setResolution = true;
	private bool _isStreaming = false;

    public bool HasData { get; private set; }

    public void StartStreaming()
    {
        cameraCaptures = GetComponentsInChildren<CustomCameraCapture>();
        cameraCaptures[cameraCaptures.Length - 1].OnPostRenderEvent.AddListener(Execute);
        if(cameraCaptures.Length != 2)
        {
            Debug.LogError("There should be two cameras.");
            return;
        }

        if (!_isStreaming)
        {
            PrepareTexture();

            _streamer.CreateStream();
            _streamer.Stream();
            _isStreaming = true;
        }
    }

    public void StopStreaming()
    {
        if (_isStreaming)
        {
            _streamer.SetGrabber(null);
            _streamer.Pause();
            Thread.Sleep(100);
            _streamer.Stop();
            _streamer.Close();

            _grabber.Destroy();
            DestroyTexture();

            _created = false;
            DestroyTexture();

            if (cameraCaptures.Length > 0)
                cameraCaptures[cameraCaptures.Length - 1].OnPostRenderEvent.RemoveListener(Execute);
        }
    }

    void OnEnable()
	{

	}

	void OnDisable()
	{

	}

	void Destroy()
	{
		
    }

	void PrepareTexture()
	{
		if (_preparedTexture) return;

		_tempRenderTarget = RenderTexture.GetTemporary(config.Width, config.Height);
		foreach (var cap in cameraCaptures)
		{
			cap.SetRenderTarget(_tempRenderTarget);
		}
		_preparedTexture = true;
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
		
		_preparedTexture = false;
	}

	// Use this for initialization
	void Start()
    {
        if (!config)
        {
            return;
        }

        Initialize(config);
    }

    void OnDestroy()
	{
		Destroy();
	}

	void Update()
    {
        //if (!_created && _grabber != null && HasData)
        //{
        //    //important to create stream after data is confirmed
        //    _streamer.CreateStream();
        //    _streamer.Stream();
        //    _created = true;
        //}

    }

    private void Execute()
    {
        if (!_preparedTexture)
        {
            return;
        }

        var tempRT = RenderTexture.GetTemporary(config.Width, config.Height);
        //Graphics.Blit(source, tempRT);
        if (TempTex == null)
        {
            TempTex = new Texture2D(config.Width, config.Height, TextureFormat.RGB24, false);
        }

        //RenderTexture.active = _tempRenderTarget;
        Graphics.Blit(_tempRenderTarget, tempRT);

        TempTex.ReadPixels(new Rect(0, 0, config.Width, config.Height), 0, 0, false);
        TempTex.Apply();
        _grabber.SetTexture2D(TempTex);
        _grabber.Update();
        HasData = true;

        //Destroy(tempTex);
        RenderTexture.ReleaseTemporary(tempRT);
    }

    public void Initialize(VideoStreamerConfig videoStreamerConfig)
    {
        config = videoStreamerConfig;
        Initialize();
    }

    private void Initialize()
    {
        if (!config)
            throw new Exception("VideoStreamerConfig is not set.");

        _streamer = new GstCustomVideoStreamer();
        _grabber = new GstUnityImageGrabber();

        _streamer.SetGrabber(_grabber);
        _streamer.SetPipelineString(config.GetPipeline());
        _streamer.SetResolution(config.Width, config.Height, config.Fps);
    }
}
