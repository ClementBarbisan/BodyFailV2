// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Ray"
{
	Properties
	{
		//_MainTex("Main Texture", 2D) = "white" {}
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
			};
			
			// Particle's data, shared with the compute shader
			StructuredBuffer<float3> particleBuffer;
			StructuredBuffer<int> segmentBuffer;
			
			//sampler2D _MainTex;

			// Properties variables
			uniform int _Width;
			uniform int _Height;
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
				o.position = float4(sin(particleBuffer[instance_id].y * instance_id * _Time.y) * cos(particleBuffer[instance_id].x * instance_id * _Time.y), sin(particleBuffer[instance_id].z * instance_id * _Time.y) * sin(particleBuffer[instance_id].y * instance_id * _Time.y), sin(particleBuffer[instance_id].x * instance_id * _Time.y) * cos(particleBuffer[instance_id].z * instance_id * _Time.y), 1.0f) * 100 * _Value + float4(particleBuffer[instance_id].x, particleBuffer[instance_id].y, particleBuffer[instance_id].z, 1.0f);
				o.instance = int(instance_id);
				//o.keep.y = pow(1.0 - (o.position.z - _MinZ) / (_MaxZ - _MinZ), 2);
				if (segmentBuffer[instance_id] == 0 || o.position.z == 0 || ((instance_id / _Width) % 20 != 0 && segmentBuffer[instance_id - 1] == 1 && segmentBuffer[instance_id + 1] == 1 && segmentBuffer[instance_id - _Width] == 1 && segmentBuffer[instance_id + _Width] == 1))
				{
					o.keep.x = 0;
				}
				else if (segmentBuffer[instance_id - 1] != segmentBuffer[instance_id + 1] || segmentBuffer[instance_id - _Width] != segmentBuffer[instance_id + _Width])
					o.keep.x = 2;
				else	
					o.keep.x = 1;
				return o;
			}

			float EaseInOut(float t)
			{
				return (t*t);
				if (t > 0.5f)
					return 2.0f * (t * t);
				t -= 0.5f;
				return 2.0f * t * (1.0f - t) + 0.5;
			}

			[maxvertexcount(2)]
			void geom(point PS_INPUT p[1], inout LineStream<PS_INPUT> lineStream)
			{
				PS_INPUT o;
				o.instance = p[0].instance;
				o.keep.x = p[0].keep.x == 0.0 ? 0.0 : p[0].keep.x;
				o.keep.y = 0;
				if (o.keep.x == 0.0 || segmentBuffer[(p[0].instance + 161803) % (_Width * _Height)] == 0)
				{
					o.keep.x = 0;
					return;
				}
				float4 position2 = float4(particleBuffer[(o.instance + 161803) % (_Width * _Height)].xyz, 1.0);
				float value = EaseInOut(_Value);
				float4 pos = float4(p[0].position.x + rand(float2(p[0].position.z, o.instance) - 0.5f) * 1000.0f * value, p[0].position.y + rand(float2(p[0].position.x, o.instance) - 0.5f) * 1000.0f * value, p[0].position.z + rand(float2(p[0].position.y, o.instance) - 0.5f) * 1000.0f * value, p[0].position.w);
				o.position = UnityObjectToClipPos(p[0].position + pos);
				lineStream.Append(o);
				pos = float4(position2.x + rand(float2(position2.z, o.instance) - 0.5f) * 1000.0f * value, position2.y + rand(float2(position2.x, o.instance) - 0.5f) * 1000.0f * value, position2.z + rand(float2(position2.y, o.instance) - 0.5f) * 1000.0f * value, position2.w);
				o.position = UnityObjectToClipPos(position2 + pos);
				lineStream.Append(o);
				lineStream.RestartStrip();
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
			return (float4(1.0f, 1.0f, 1.0f, 1.0f) * (1.0f - i.position.z / i.position.w)); // *CalcLuminance(tex2D(_MainTex, float2(i.instance % _Width / _Width, i.instance / _Width / _Height)).xyz));// *i.keep.y);
			}
			
			ENDCG
		}
	}

	Fallback Off
}
