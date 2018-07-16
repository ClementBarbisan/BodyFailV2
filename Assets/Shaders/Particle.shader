// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Particle"
{
	Properties
	{
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
				o.position = float4(sin(particleBuffer[instance_id].y * instance_id * _Time.y) * cos(particleBuffer[instance_id].x * instance_id * _Time.y), sin(particleBuffer[instance_id].z * instance_id * _Time.y) * sin(particleBuffer[instance_id].y * instance_id * _Time.y), sin(particleBuffer[instance_id].x * instance_id * _Time.y) * cos(particleBuffer[instance_id].z * instance_id * _Time.y), 1.0f) * 100 * _Value + float4(particleBuffer[instance_id], 1.0f);
				o.instance = int(instance_id);
				o.keep.y = pow(1.0 - (o.position.z - _MinZ) / (_MaxZ - _MinZ), 2);
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

			[maxvertexcount(24)]
			void geom(point PS_INPUT p[1], inout TriangleStream<PS_INPUT> triStream)
			{
				PS_INPUT o;
				o.instance = p[0].instance;
				if (p[0].keep.x == 0)
				{
					o.keep.x = 0;
					o.keep.y = 0;
					return;
				}
				o.keep.x = 1;
				o.keep.y = p[0].keep.y;
				float4 position = float4(p[0].position.x + rand(float2(p[0].position.z, o.instance) - 0.5f) * 1000.0f * _Value, p[0].position.y + rand(float2(p[0].position.x, o.instance) - 0.5f) * 1000.0f * _Value, p[0].position.z + rand(float2(p[0].position.y, o.instance) - 0.5f) * 1000.0f * _Value, p[0].position.w);
				float4 positionZ = float4(p[0].position.x + rand(float2(o.instance, p[0].position.y) - 0.5f) * 1000.0f * _Value, p[0].position.y + rand(float2(o.instance, p[0].position.z) - 0.5f) * 1000.0f * _Value, p[0].position.z + rand(float2(o.instance, p[0].position.x) - 0.5f) * 1000.0f * _Value, p[0].position.w);
				float size = 1 + clamp(rand(float2(o.instance, _Time.y)) * 100.0f * _Value, 0, 100);
				if (p[0].keep.x == 1)
					size = clamp(rand(float2(o.instance, _Time.y)) * 100.0f * _Value, 5, 100);
				float4 B = float4(size * 5, 0, 0, 0);
				float4 C = float4(0, 0, -size, 0);
				float4 D = float4(size * 5, 0, -size, 0);
				float4 E = float4(size * 5, -size, -size, 0);
				float4 F = float4(size * 5, -size, 0, 0);
				float4 G = float4(0, -size, 0, 0);
				float4 H = float4(0, -size, -size, 0);
				o.position = UnityObjectToClipPos(position);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(positionZ + B);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(position + C);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(positionZ + D);
				triStream.Append(o);
				triStream.RestartStrip();
				o.position = UnityObjectToClipPos(positionZ + D);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(positionZ + B);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(positionZ + E);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(positionZ + F);
				triStream.Append(o);
				triStream.RestartStrip();
				o.position = UnityObjectToClipPos(positionZ + B);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(position);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(positionZ + F);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(position + G);
				triStream.Append(o);
				triStream.RestartStrip();
				o.position = UnityObjectToClipPos(position + G);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(position);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(position + H);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(position + C);
				triStream.Append(o);
				triStream.RestartStrip();
				o.position = UnityObjectToClipPos(position + H);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(position + C);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(positionZ + E);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(positionZ + D);
				triStream.Append(o);
				triStream.RestartStrip();
				o.position = UnityObjectToClipPos(position + G);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(position + H);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(positionZ + F);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(positionZ + E);
				triStream.Append(o);
				triStream.RestartStrip();
			}

			// Pixel shader
			float4 frag(PS_INPUT i) : COLOR
			{
				if (i.keep.x == 0)
				{
					discard;
					return (float4(0, 0, 0, 0));
				}
				return (float4(1.0f, 1.0f, 1.0f, 1.0f) * i.keep.y);
			}
			
			ENDCG
		}
	}

	Fallback Off
}
