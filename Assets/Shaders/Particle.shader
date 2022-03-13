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
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma target 5.0

			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile_instancing
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
			o.position = float4(particleBuffer[instance_id].x, particleBuffer[instance_id].y, particleBuffer[instance_id].z, 1.0f);
			o.instance = int(instance_id);
			// o.position.z += sin(_Time.y) * 10.0f;
			if (segmentBuffer[instance_id] == 0 || (int)(o.position.z) % 10 != 0)//((instance_id / _Width) % (20) != 0 && segmentBuffer[instance_id - 1] == 1 && segmentBuffer[instance_id + 1] == 1 && segmentBuffer[instance_id - _Width] == 1 && segmentBuffer[instance_id + _Width] == 1))
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
				return;
			}
			o.keep.x = 1;
			o.keep.y = p[0].keep.y;
			float4 position = float4(p[0].position.x + 0.5f * 1000.0f * (_Value + 0.01), p[0].position.y + 0.5f * 1000.0f * (_Value + 0.01), p[0].position.z + 0.5f * 1000.0f * (_Value + 0.01), p[0].position.w + 0.5f * 1000.0f * (_Value + 0.01));
			float size = 5 + clamp(rand(float2(o.instance, _Time.y)) * 200.0f * (_Value + 0.01), 0, 100);
			if (p[0].keep.x == 1)
				size = clamp(rand(float2(o.instance, _Time.y)) * 100.0f * _Value, 2.5, 100);
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
			o.position = UnityObjectToClipPos(position + B);
			o.uv = float2(0, 1);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + C);
			o.uv = float2(1, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + D);
			o.uv = float2(1, 1);
			triStream.Append(o);
			triStream.RestartStrip();
			o.position = UnityObjectToClipPos(position + D);
			o.uv = float2(0, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + B);
			o.uv = float2(0, 1);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + E);
			o.uv = float2(1, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + F);
			o.uv = float2(1, 1);
			triStream.Append(o);
			triStream.RestartStrip();
			o.position = UnityObjectToClipPos(position + B);
			o.uv = float2(0, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + A);
			o.uv = float2(0, 1);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + F);
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
			o.position = UnityObjectToClipPos(position + E);
			o.uv = float2(1, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + D);
			o.uv = float2(1, 1);
			triStream.Append(o);
			triStream.RestartStrip();
			o.position = UnityObjectToClipPos(position + G);
			o.uv = float2(0, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + H);
			o.uv = float2(0, 1);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + F);
			o.uv = float2(1, 0);
			triStream.Append(o);
			o.position = UnityObjectToClipPos(position + E);
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
			return ((float4(1.0f, 1.0f, 1.0f, 1.0f) * (1.0f - i.position.z / i.position.w)));
		}

		ENDCG
		}
		Pass 
		{
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma target 5.0
			
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile_instancing
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
				o.position = float4(particleBuffer[instance_id].x, particleBuffer[instance_id].y, particleBuffer[instance_id].z, 1.0f);
				o.instance = int(instance_id);
				// o.position.z += sin(_Time.y) * 10.0f;
				//o.keep.y = pow(1.0 - (o.position.z - _MinZ) / (_MaxZ - _MinZ), 2);
				if (segmentBuffer[instance_id] == 0 || (int)(o.position.z) % int(15 - (15 * _Value))  != 0 || instance_id % int(10 - (10 * _Value)) != 0)// || ((instance_id / _Width) % (20) != 0 && segmentBuffer[instance_id - 1] == 1 && segmentBuffer[instance_id + 1] == 1 && segmentBuffer[instance_id - _Width] == 1 && segmentBuffer[instance_id + _Width] == 1))
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
				o.keep.x = p[0].keep.x;
				o.keep.y = 0;
				if (o.keep.x == 0 || segmentBuffer[(p[0].instance + (_Width * _Height) * rand(float2(p[0].position.xy))) % (_Width * _Height)] == 0)
				{
					return;
				}
				float4 position2 = float4(particleBuffer[(o.instance + (_Width * _Height) * rand(float2(p[0].position.xy))) % (_Width * _Height)].xyz, 1.0);
				float value = EaseInOut(_Value);
				float4 pos = float4(p[0].position.x + 0.5f * 1000.0f * (_Value + 0.01), p[0].position.y + 0.5f * 1000.0f * (_Value + 0.01), p[0].position.z + 0.5f * 1000.0f * (_Value + 0.01), p[0].position.w + 0.5f * 1000.0f * (_Value + 0.01));;
				o.position = UnityObjectToClipPos(pos);
				lineStream.Append(o);
				pos = float4(position2.x + 0.5f * 1000.0f * (_Value + 0.01), position2.y + 0.5f * 1000.0f * (_Value + 0.01), position2.z + 0.5f * 1000.0f * (_Value + 0.01), position2.w + 0.5f * 1000.0f * (_Value + 0.01));;
				o.position = UnityObjectToClipPos(pos);
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
				}
			return (float4(0.75f, 0.75f, 0.75f, 0.75f) * (1.0f - i.position.z / i.position.w)); // *CalcLuminance(tex2D(_MainTex, float2(i.instance % _Width / _Width, i.instance / _Width / _Height)).xyz));// *i.keep.y);
			}
			
			ENDCG
		}
	}

	Fallback Off
}
