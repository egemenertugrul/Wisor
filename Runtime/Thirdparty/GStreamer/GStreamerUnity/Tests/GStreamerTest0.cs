using UnityEngine;
using System.Collections;

[RequireComponent(typeof(GstNetworkTexture))]
public class GStreamerTest0 : MonoBehaviour {
	
	private GstNetworkTexture m_Texture = null;

	public string Host = "127.0.0.1";
	public int port=7000;
	// Use this for initialization
	void Start () {
		m_Texture = gameObject.GetComponent<GstNetworkTexture>();
		
		// Check to make sure we have an instance.
		if (m_Texture == null)
		{
			DestroyImmediate(this);
		}
		
		m_Texture.Initialize ();

		m_Texture.ConnectToHost (Host, port);
		m_Texture.Play ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
