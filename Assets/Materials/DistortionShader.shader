// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader"Hidden/Distortion Shader" 
{  
//    Properties  
//    {  
//        _MainTex ("Texture", 2D) = "white" {}  
//        sampleTex ("sampleTex", 2D) = "black" {}  
//        //_K ("K", Float) = -0.15  
//        //_KCube ("KCube", Float) = 0.5  
//        _LensDistortionStrength ("LensDistortionStrength", Float) = 0.5
//        _LensDistortionTightness ("LensDistortionTightness", Float) = 0.5
//        _OutOfBoundColour ("OutOfBoundColour", Color ) = (0,0,0,0)   
//    }  
//    SubShader  
//    {  
//        // No culling or depth  
//Cull Off
//ZWrite Off
//ZTest Always
   
//        Pass  
//        {  
//            CGPROGRAM  
//            #pragma vertex vert  
//            #pragma fragment frag  
              
//#include "UnityCG.cginc"  
   
//struct appdata
//{
//    float4 vertex : POSITION;
//    float2 uv : TEXCOORD0;
//};
   
//struct v2f
//{
//    float2 uv : TEXCOORD0;
//    float4 vertex : SV_POSITION;
//};
   
//v2f vert(appdata v)
//{
//    v2f o;
//    o.vertex = UnityObjectToClipPos(v.vertex);
//    o.uv = v.uv;
//    return o;
//}
               
//sampler2D _MainTex;
//sampler2D sampleTex;

////fixed _K;
////fixed _KCube;
//float _LensDistortionStrength;
//float _LensDistortionTightness;
//float4 _OutOfBoundColour;
   
//fixed4 frag(v2f i) : SV_Target
//{

//    const float2 uv_centered = i.uv * 2 - 1; //change UV range from (0,1) to (-1,1)
//    const float distortionMagnitude = length(uv_centered); //get value with 1 at corner and 0 at middle
    
//    const float smoothDistortionMagnitude = pow(distortionMagnitude, _LensDistortionTightness); //use exponential function
//    //  const float smoothDistortionMagnitude=1-sqrt(1-pow(distortionMagnitude,_LensDistortionTightness));//use circular function
//    //  const float smoothDistortionMagnitude = pow(sin(UNITY_HALF_PI * distortionMagnitude), _LensDistortionTightness); // use sinusoidal function
    
    
//    float2 uvDistorted = i.uv + uv_centered * smoothDistortionMagnitude * _LensDistortionStrength; //vector of distortion and add it to original uv

//    if (uvDistorted[0] < 0 || uvDistorted[0] > 1 || uvDistorted[1] < 0 || uvDistorted[1] > 1)
//    {
//        return _OutOfBoundColour; //uv out of bound so display out of bound color
//    }
//    else
//    {
//        return tex2D(_MainTex, uvDistorted);
//    }
//}
//            ENDCG  
//        }  
//    }  

		Properties{
		_MainTex("MainTex", 2D) = "white" {}
		_distortion("distortion", range(-3, 3)) = -0.7
		_isRight("isRight", range(0, 1)) = 0.0
		_offsetX("offsetX", range(-0.5, 0.5)) = 0.0
			_cubicDistortion("cubicDistortion", range(0, 3)) = 0.4
			_scale("scale", range(0, 3)) = 1
        _OutOfBoundColour ("OutOfBoundColour", Color ) = (0,0,0,0)   

	}
		SubShader{
			pass{
			Tags{ "LightMode" = "ForwardBase" }
Cull off
				CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 4.0 
#include "UnityCG.cginc"
float _offsetX;

float _distortion;
float _cubicDistortion;
bool _isRight;
float _scale;

float4 _OutOfBoundColour;


sampler2D _MainTex;
fixed4 _MainTex_ST;
struct v2f
{
    fixed4 pos : SV_POSITION;
    fixed2 uv_MainTex : TEXCOORD0;

};

v2f vert(appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv_MainTex = TRANSFORM_TEX(v.texcoord, _MainTex);
    return o;
}

float2 barrel(float2 uv)
{
    float2 h = uv.xy - float2(0.5, 0.5);
    
    h.x += _offsetX.x;
    
    float r2 = h.x * h.x; // + h.y * h.y;

    if (_isRight)
    {
        if (h.x > 0.3)
            return float2(-5, -5);
        
        if (h.x < 0)
            r2 *= 3;
    }
    if (!_isRight)
    {
        if (h.x < -0.30)
            return float2(-5, -5);
        
        if (h.x > 0)
            r2 *= 3;
    }
    
    float f = 1.0 + r2 * (_distortion + _cubicDistortion * r2);
    
    float dec = f * 0.5f;
    float2 ret = float2(f * h.x + 0.5f, h.y + dec);
    
    if (ret[0] < 0 || ret[0] > 1 || ret[1] < 0 || ret[1] > 1)
    {
        return float2(-5, -5); //uv out of bound so display out of bound color
    }
    
    
    //ret.x += _offsetX;
    //return f * _scale * h + 0.5;
    return ret;
    //return f * h + 0.5;
}

fixed4 frag(v2f i) : COLOR
{
    float2 barreled = barrel(i.uv_MainTex);
    
    if (barreled[0] < -1 && barreled[1] < -1)
    {
        return _OutOfBoundColour;
    }
    
    fixed4 distorted = tex2D(_MainTex, barreled);
    
    return distorted;
}
			ENDCG
}//

}
	
}  