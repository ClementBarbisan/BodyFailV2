// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Particle"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
	}

	SubShader 
	{
		Pass 
		{
			Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" "LightMode" = "Deferred" }
			LOD 100
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha
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
			
			sampler2D _MainTex;
			float4 _MainTex_ST;

			// Properties variables
			uniform int _Width;
			uniform int _Height;
			
			// Vertex shader
			PS_INPUT vert(uint instance_id : SV_instanceID)
			{
				PS_INPUT o = (PS_INPUT)0;
				// Position
				o.position = float4(particleBuffer[instance_id], 1.0f);
				o.instance = int(instance_id);
				if (segmentBuffer[instance_id] == 0 || o.position.z == 0 || (instance_id / _Width) % 20 != 0)
				{
					o.keep.x = 0;
				}
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
				o.keep.y = 1;
				float size = 5;
				float4 B = float4(size, 0, 0, 0);
				float4 C = float4(0, 0, -size, 0);
				float4 D = float4(size, 0, -size, 0);
				float4 E = float4(size, -size, -size, 0);
				float4 F = float4(size, -size, 0, 0);
				float4 G = float4(0, -size, 0, 0);
				float4 H = float4(0, -size, -size, 0);
				o.position = UnityObjectToClipPos(p[0].position);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + B);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + C);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + D);
				triStream.Append(o);
				triStream.RestartStrip();
				o.position = UnityObjectToClipPos(p[0].position + D);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + B);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + E);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + F);
				triStream.Append(o);
				triStream.RestartStrip();
				o.position = UnityObjectToClipPos(p[0].position + B);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + F);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + G);
				triStream.Append(o);
				triStream.RestartStrip();
				o.position = UnityObjectToClipPos(p[0].position + G);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + H);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + C);
				triStream.Append(o);
				triStream.RestartStrip();
				o.position = UnityObjectToClipPos(p[0].position + H);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + C);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + E);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + D);
				triStream.Append(o);
				triStream.RestartStrip();
				o.position = UnityObjectToClipPos(p[0].position + G);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + H);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + F);
				triStream.Append(o);
				o.position = UnityObjectToClipPos(p[0].position + E);
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
				return (float4(1.0f, 1.0f, 1.0f, 1.0f));
			}
			
			ENDCG
		}
	}

	Fallback Off
}
