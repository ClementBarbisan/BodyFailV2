﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Particle"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
		_Value("Detect Disruption", Range(0,1)) = 0.0
	}

	SubShader 
	{
		Pass 
		{
			Tags{ "LightMode" = "Deferred" }
			LOD 100
			ZWrite Off
			//Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma target 5.0

			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile_instancing multi_compile_prepassfinal noshadowmask nodynlightmap nodirlightmap nolightmap
			#include "UnityCG.cginc"

			// Pixel shader input
			struct PS_INPUT
			{
				float4 position : SV_POSITION;
				uint instance : SV_InstanceID;
				float2 keep : TEXCOORD0;
				float2 uv : TEXCOORD1;
			};

		// Particle's data, shared with the compute shader
		StructuredBuffer<float3> particleBuffer;
		StructuredBuffer<int> segmentBuffer;

		sampler2D _MainTex;

		// Properties variables
		uniform int _Width;
		uniform int _WidthTex;
		uniform int _Height;
		uniform int _HeightTex;
		uniform float _MinZ;
		uniform float _MaxZ;
		uniform float _Value;
		uniform float _Rotation;

		float rand(in float2 uv)
		{
			float2 noise = (frac(sin(dot(uv, float2(12.9898, 78.233)*2.0)) * 43758.5453));
			return abs(noise.x + noise.y) * 0.5;
		}

		// Vertex shader
		PS_INPUT vert(uint instance_id : SV_instanceID)
		{
			PS_INPUT o = (PS_INPUT)0;
			// Position
			o.position = float4(sin(particleBuffer[instance_id].y * instance_id * _Time.y) * cos(particleBuffer[instance_id].x * instance_id * _Time.y), sin(particleBuffer[instance_id].z * instance_id * _Time.y) * sin(particleBuffer[instance_id].y * instance_id * _Time.y), sin(particleBuffer[instance_id].x * instance_id * _Time.y) * cos(particleBuffer[instance_id].z * instance_id * _Time.y), 1.0f) * 100 * (_Value + 0.01) + float4(particleBuffer[instance_id].x, particleBuffer[instance_id].y, particleBuffer[instance_id].z, 1.0f);
			o.instance = int(instance_id);
			//o.keep.y = pow(1.0 - (o.position.z - _MinZ) / (_MaxZ - _MinZ), 2);
			if (segmentBuffer[instance_id] == 0 || o.position.z == 0 || instance_id % 5 != 0)//((instance_id / _Width) % (20) != 0 && segmentBuffer[instance_id - 1] == 1 && segmentBuffer[instance_id + 1] == 1 && segmentBuffer[instance_id - _Width] == 1 && segmentBuffer[instance_id + _Width] == 1))
			{
				o.keep.x = 0;
			}
			else if (segmentBuffer[instance_id - 1] != segmentBuffer[instance_id + 1] || segmentBuffer[instance_id - _Width] != segmentBuffer[instance_id + _Width])
				o.keep.x = 2;
			else
				o.keep.x = 1;
			return o;
		}

		[maxvertexcount(24)]
		void geom(point PS_INPUT p[1], inout TriangleStream<PS_INPUT> triStream)
		{
			PS_INPUT o;
			o.instance = p[0].instance;
			o.uv = float2(0, 0);
			if (p[0].keep.x == 0)
			{
				o.keep.x = 0;
				o.keep.y = 0;
				return;
			}
			o.keep.x = 1;
			o.keep.y = p[0].keep.y;
			float4 position = float4(p[0].position.x + rand(float2(p[0].position.z, o.instance) - 0.5f) * 1000.0f * (_Value + 0.01), p[0].position.y + rand(float2(p[0].position.x, o.instance) - 0.5f) * 1000.0f * (_Value + 0.01), p[0].position.z + rand(float2(p[0].position.y, o.instance) - 0.5f) * 1000.0f * (_Value + 0.01), p[0].position.w);
			float4 positionZ = float4(p[0].position.x + rand(float2(o.instance, p[0].position.y) - 0.5f) * 1000.0f * (_Value + 0.01), p[0].position.y + rand(float2(o.instance, p[0].position.z) - 0.5f) * 1000.0f * (_Value + 0.01), p[0].position.z + rand(float2(o.instance, p[0].position.x) - 0.5f) * 1000.0f * (_Value + 0.01), p[0].position.w);
			float size = 5 + clamp(rand(float2(o.instance, _Time.y)) * 200.0f * (_Value + 0.01), 0, 100);
			if (p[0].keep.x == 1)
				size = clamp(rand(float2(o.instance, _Time.y)) * 100.0f * _Value, 5, 100);
			float4 A = float4(-size / 2 * (5 * _Value + 1), size / 2, size / 2, 0);
			float4 B = float4(size / 2 * (5 * _Value + 1), size / 2, size / 2, 0);
			float4 C = float4(-size / 2 * (5 * _Value + 1), size / 2, -size / 2, 0);
			float4 D = float4(size / 2 * (5 * _Value + 1), size / 2, -size / 2, 0);
			float4 E = float4(size / 2 * (5 * _Value + 1), -size / 2, -size / 2, 0);
			float4 F = float4(size / 2 * (5 * _Value + 1), -size / 2, size / 2, 0);
			float4 G = float4(-size / 2 * (5 * _Value + 1), -size / 2, size / 2, 0);
			float4 H = float4(-size / 2 * (5 * _Value + 1), -size / 2, -size / 2, 0);
			o.position = UnityObjectToClipPos(position + A);
			o.uv = float2(0, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(positionZ + B);
			o.uv = float2(0, 1);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + C);
			o.uv = float2(1, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(positionZ + D);
			o.uv = float2(1, 1);
			triStream.Append(o);
			triStream.RestartStrip();
			o.position = UnityObjectToClipPos(positionZ + D);
			o.uv = float2(0, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(positionZ + B);
			o.uv = float2(0, 1);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(positionZ + E);
			o.uv = float2(1, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(positionZ + F);
			o.uv = float2(1, 1);
			triStream.Append(o);
			triStream.RestartStrip();
			o.position = UnityObjectToClipPos(positionZ + B);
			o.uv = float2(0, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + A);
			o.uv = float2(0, 1);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(positionZ + F);
			o.uv = float2(1, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + G);
			o.uv = float2(1, 1);
			triStream.Append(o);
			triStream.RestartStrip();
			o.position = UnityObjectToClipPos(position + G);
			o.uv = float2(0, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + A);
			o.uv = float2(0, 1);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + H);
			o.uv = float2(1, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + C);
			o.uv = float2(1, 1);
			triStream.Append(o);
			triStream.RestartStrip();
			o.position = UnityObjectToClipPos(position + H);
			o.uv = float2(0, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + C);
			o.uv = float2(0, 1);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(positionZ + E);
			o.uv = float2(1, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(positionZ + D);
			o.uv = float2(1, 1);
			triStream.Append(o);
			triStream.RestartStrip();
			o.position = UnityObjectToClipPos(position + G);
			o.uv = float2(0, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + H);
			o.uv = float2(0, 1);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(positionZ + F);
			o.uv = float2(1, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(positionZ + E);
			o.uv = float2(1, 1);
			triStream.Append(o);
			triStream.RestartStrip();
		}

		float CalcLuminance(float3 color)
		{
			return dot(color, float3(0.299f, 0.587f, 0.114f)) * 3;
		}

		// Pixel shader
		float4 frag(PS_INPUT i) : COLOR
		{
			if (i.keep.x == 0)
			{
				discard;
				return (float4(0, 0, 0, 0));
			}
			half2 fw = fwidth(i.uv);
			half2 edge2 = min(smoothstep(0, fw * 2, i.uv),
				smoothstep(0, fw * 2, 1 - i.uv));
			half edge = 1 - min(edge2.x, edge2.y);
			//return(tex2D(_MainTex, float2(i.instance % _WidthTex / (_WidthTex * _HeightTex), i.instance / _WidthTex / (_WidthTex * _HeightTex))));
			return ((float4(0.5f, 0.5f, 0.5f, 1.0f) * (1.0f - i.position.z / i.position.w) + edge * float4(1.0, 1.0, 1.0, 1.0)));// *CalcLuminance(tex2D(_MainTex, float2(i.instance % _Width, i.instance / _Width)).xyz));
		}

		ENDCG
			}
				CGPROGRAM
				// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Standard fullforwardshadows

				// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

				sampler2D _MainTex;

			struct Input {
				float2 uv_MainTex;
			};

			half _Glossiness;
			half _Metallic;
			fixed4 _Color;

			// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
			// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
			// #pragma instancing_options assumeuniformscaling
			UNITY_INSTANCING_BUFFER_START(Props)
				// put more per-instance properties here
				UNITY_INSTANCING_BUFFER_END(Props)

				void surf(Input IN, inout SurfaceOutputStandard o) {
				// Albedo comes from a texture tinted by color
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
				o.Albedo = c.rgb;
				// Metallic and smoothness come from slider variables
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = c.a;
			}
			ENDCG
		
	}

	Fallback Off
}
