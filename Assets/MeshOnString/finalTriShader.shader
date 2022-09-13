Shader "Debug/finalTriShader"
{
    Properties {

    _Color ("Color", Color) = (1,1,1,1)
    _Size ("Size", float) = .01
    }


  SubShader{
    Cull Off
    Pass{

      CGPROGRAM
      
      #pragma target 4.5

      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

struct Vert{
  float3 pos;
  float3 nor;
  float3 tang;
  float3 oPos;
  float3 oNor;
  float3 oTang;
  float2 uv;
  float4 color;
};


    



      uniform float _Size;
      uniform float3 _Color;

      
      StructuredBuffer<Vert> _VertBuffer;
      StructuredBuffer<int> _TriBuffer;


      //A simple input struct for our pixel shader step containing a position.
      struct varyings {
          float4 pos      : SV_POSITION;
          float3 nor      : TEXCOORD0;
          float3 worldPos : TEXCOORD1;
          float3 eye      : TEXCOORD2;
          float2 debug    : TEXCOORD3;
          float2 uv       : TEXCOORD4;
          float2 uv2       : TEXCOORD6;
          float id        : TEXCOORD5;
          float type :TEXCOORD7;
          float4 color : TEXCOORD8;
      };

//Our vertex function simply fetches a point from the buffer corresponding to the vertex index
//which we transform with the view-projection matrix before passing to the pixel program.
varyings vert (uint id : SV_VertexID){


  varyings o;


Vert v = _VertBuffer[_TriBuffer[id]];

      o.worldPos = v.pos;
      o.nor =v.nor;
      o.uv = v.uv;
      o.color = v.color;
      o.pos = mul (UNITY_MATRIX_VP, float4(o.worldPos,1.0f));


  return o;

}

      //Pixel function returns a solid color for each point.
      float4 frag (varyings v) : COLOR {
        float3 col = 1;

      
   
          return float4(v.color.xyz,1 );
      }

      ENDCG

    }
  }


}
