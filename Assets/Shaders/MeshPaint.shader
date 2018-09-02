
Shader "MeshPaint/TerrainDiffuse"
{
	Properties 
	{
		_Color ("Color", Color) = (1,1,1,1)

		_SplatBottom ("SplatBottom", 2D) = "white" {}
		_Splat1 ("Splat1", 2D) = "white" {}
		_Splat2 ("Splat2", 2D) = "white" {}
		_Splat3 ("Splat3", 2D) = "white" {}
		_Splat4 ("Splat4", 2D) = "white" {}
		_Blend ("Blend", 2D) = "black" {}
	}
	
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM

		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input 
		{
			float2 texcoord : TEXCOORD0;
		};
		
		fixed4 _Color;
		sampler2D _Blend;
		sampler2D _SplatBottom, _Splat1, _Splat2, _Splat3, _Splat4;
		float4 _SplatBottom_ST, _Splat1_ST, _Splat2_ST, _Splat3_ST, _Splat4_ST;

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.texcoord = v.texcoord;
		}
		
		void surf (Input IN, inout SurfaceOutput o)
		{
			float2 uv_SplatBottom = TRANSFORM_TEX(IN.texcoord, _SplatBottom);
			float2 uv_Splat1 = TRANSFORM_TEX(IN.texcoord, _Splat1);
			float2 uv_Splat2 = TRANSFORM_TEX(IN.texcoord, _Splat2);
			float2 uv_Splat3 = TRANSFORM_TEX(IN.texcoord, _Splat3);
			float2 uv_Splat4 = TRANSFORM_TEX(IN.texcoord, _Splat4);

			float4 layerbtm = tex2D(_SplatBottom, uv_SplatBottom);
			float4 layer1 = tex2D(_Splat1, uv_Splat1);
			float4 layer2 = tex2D(_Splat2, uv_Splat2);
			float4 layer3 = tex2D(_Splat3, uv_Splat3);
			float4 layer4 = tex2D(_Splat4, uv_Splat4);
			float4 blend = tex2D(_Blend, IN.texcoord);

			float4 color = layerbtm;
			color = (1.0 - blend.r) * color + blend.r * layer1;
			color = (1.0 - blend.g) * color + blend.g * layer2;
			color = (1.0 - blend.b) * color + blend.b * layer3;
			color = (1.0 - blend.a) * color + blend.a * layer4;

			o.Albedo = color;
			o.Alpha = 0;
		}

		ENDCG
	}

	FallBack "Diffuse"
}
