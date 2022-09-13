Shader "SpaceFace/Particles" {
    Properties {

    _Color ("Color", Color) = (1,1,1,1)
    _Size ("Size", float) = .01
    _HueStart ("_HueStart", float) = .01
    _HueSize ("_HueSize", float) = .01
    
        _SpriteSize("SpriteSize",int) = 6
      _MainTex ("Texture", 2D) = "white" {}
      _ColorMap ("Texture", 2D) = "white" {}
      _InputColorMap ("Texture", 2D) = "white" {}
      _SizeMap ("Texture", 2D) = "white" {}
    }


  SubShader{
    Cull Off// inside SubShader
//Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

// inside Pass
//ZWrite Off
//Blend One One // Additive
//
//Blend SrcAlpha OneMinusSrcAlpha // Traditional transparency
//Blend One OneMinusSrcAlpha // Premultiplied transparency
//Blend One One // Additive
//Blend OneMinusDstColor One // Soft Additive
//Blend DstColor Zero // Multiplicative
//Blend DstColor SrcColor // 2x Multiplicative
    Pass{

      CGPROGRAM
      
      #pragma target 4.5

      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"
      float3 hsv(float h, float s, float v)
{
  return lerp( float3( 1.0 , 1, 1 ) , clamp( ( abs( frac(
    h + float3( 3.0, 2.0, 1.0 ) / 3.0 ) * 6.0 - 3.0 ) - 1.0 ), 0.0, 1.0 ), s ) * v;
}

                  float hash( float n ){
        return frac(sin(n)*43758.5453);
      }



      struct Vert{
      float3 pos;
      float3 vel;
      float3 nor;
      float3 tan;
      float2 uv;
      float used;
      float3 triIDs;
      float3 triWeights;
      float3 debug;
    };

      



      uniform int _Count;
      uniform float _Size;
      uniform float3 _Color;
      int _SpriteSize;

      
      sampler2D _MainTex;
      sampler2D _ColorMap;
      sampler2D _InputColorMap;
      sampler2D _SizeMap;
      StructuredBuffer<Vert> _VertBuffer;
      float _HueStart;
      float _HueSize;


      //uniform float4x4 worldMat;

      //A simple input struct for our pixel shader step containing a position.
      struct varyings {
          float4 pos      : SV_POSITION;
          float3 nor      : TEXCOORD0;
          float3 worldPos : TEXCOORD1;
          float3 eye      : TEXCOORD2;
          float3 debug    : TEXCOORD3;
          float2 uv       : TEXCOORD4;
          float2 uv2       : TEXCOORD6;
          float id        : TEXCOORD5;
      };


float4x4 _Transform;

//float _Multiplier;
//Our vertex function simply fetches a point from the buffer corresponding to the vertex index
//which we transform with the view-projection matrix before passing to the pixel program.
varyings vert (uint id : SV_VertexID){

  varyings o;

  int base = id / 6;
  int alternate = id %6;

  if( base < _Count ){

    

      Vert v = _VertBuffer[base];

      float3 extra = float3(0,0,0);

      float3 f =v.nor;

    float3 l = normalize(cross(f ,  v.tan ));//UNITY_MATRIX_V[0].xyz;
    float3 u = normalize(cross(f , l ));
    
    float2 uv = float2(0,0);

    if( alternate == 0 ){ extra = -l - u * 2; uv = float2(0,0); }
    if( alternate == 1 ){ extra =  l - u * 2; uv = float2(1,0); }
    if( alternate == 2 ){ extra =  l + u * 2; uv = float2(1,1); }
    if( alternate == 3 ){ extra = -l - u * 2; uv = float2(0,0); }
    if( alternate == 4 ){ extra =  l + u * 2; uv = float2(1,1); }
    if( alternate == 5 ){ extra = -l + u * 2; uv = float2(0,1); }
    
      float4 size = tex2Dlod(_SizeMap , float4( v.uv , 0 , 0 ));
      //Vert v = _VertBuffer[base % _Count];
      float3 pos = (v.pos) + extra * _Size * v.debug.x *_Size* size.x * size.x;



    
      o.worldPos = pos;//mul( _Transform , float4( localPos , 1 )).xyz;///(v.pos) + extra * _Size  * v.debug.x;
      o.eye = _WorldSpaceCameraPos - o.worldPos;
      o.nor =v.nor;
      o.uv = v.uv;

             float col = hash( float(base * 10));
                float row = hash( float(base * 20));

      o.uv2 = (uv + floor(_SpriteSize * float2( col , row )))/float(_SpriteSize);
      o.id = col;// float(base);
      o.debug = v.debug;
      o.pos = mul (UNITY_MATRIX_VP, float4(o.worldPos,1.0f));

  }

  return o;

}


// All components are in the range [0…1], including hue.
float3 rgb2hsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}
 

// All components are in the range [0…1], including hue.
float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

//Pixel function returns a solid color for each point.
float4 frag (varyings v) : COLOR {
//  if( length( v.uv2 -.5)> .5){ discard;}

float4  col = tex2D(_MainTex,v.uv2);

if( col.a <  .1){ discard; }


  float3 fCol = length( col.xyz ) * hsv( _HueStart +  (length( col.xyz ) * .3 + sin( v.id * 100 )  * .1) * _HueSize, 1,1);
  float match = dot( _WorldSpaceLightPos0.xyz , normalize(v.nor));

  float4 colMap = tex2D(_ColorMap,1-saturate(match) +  hash(v.id * .01 )*.2);

  float4 colMap1 = tex2D(_InputColorMap , v.uv );


  float3 hsvVal = rgb2hsv( colMap1.xyz );

   fCol = hsv2rgb( float3( hsvVal.x + .2 * hash(v.id) - .1 , hsvVal.y + .2 * hash(v.id) - .1 ,hsvVal.z+ .2 * hash(v.id) - .1 ));
  fCol = fCol;//v.debug;//length( v.uv2-.5) * 2* hsv( sin( v.id * 10),1,1) * dot(v.nor,float3(0,0,1) );

  //fCol = tex2D(_SizeMap , v.uv );
    return float4(fCol ,.2 );
}

      ENDCG

    }
  }

  Fallback Off


}
