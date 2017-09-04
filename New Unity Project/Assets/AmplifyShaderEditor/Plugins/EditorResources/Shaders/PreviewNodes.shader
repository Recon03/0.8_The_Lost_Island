Shader "PreviewNodes"
{
	SubShader
	{
		CGINCLUDE
		#include "UnityCG.cginc"
		/*float4x4 getUnityMatrix( float id ) {
			if ( id == 0 )
				return UNITY_MATRIX_V;
			else if ( id == 1 )
				return UNITY_MATRIX_P;
			else
				return UNITY_MATRIX_MVP;
		}*/

		float _EditorTime;
		float _EditorDeltaTime;

		float4x4 transformX( float3 ang ) {
			float cosX = cos(ang.x);
			float sinX = sin(ang.x);
			/*float cosY = cos(ang.y);
			float sinY = sin(ang.y);
			float cosZ = cos(ang.z);
			float sinZ = sin(ang.z);*/

			float m00 = 1;
			float m01 = 0;
			float m02 = 0;
			float m03 = 0.0;
  			
			float m04 = 0; 
			float m05 = cosX; 
			float m06 = -sinX;
			float m07 = 0.0;
  			
			float m08 = 0;
			float m09 = sinX;
			float m10 = cosX;
			float m11 = 0.0;
  			
			float m12 = 0;
			float m13 = 0;
			float m14 = 0;
			float m15 = 1.0;

			float4x4 m = float4x4(m00, m01, m02, m03,
								m04, m05, m06, m07,
								m08, m09, m10, m11,
								m12, m13, m14, m15);
			return m;
		}

		float4x4 transformY( float3 ang ) {
			/*float cosX = cos(ang.x);
			float sinX = sin(ang.x);*/
			float cosY = cos(ang.y);
			float sinY = sin(ang.y);
			/*float cosZ = cos(ang.z);
			float sinZ = sin(ang.z);*/

			float m00 = cosY;
			float m01 = 0;
			float m02 = sinY;
			float m03 = 0.0;
  			
			float m04 = 0; 
			float m05 = 1; 
			float m06 = 0;
			float m07 = 0.0;
  			
			float m08 = -sinY;
			float m09 = 0;
			float m10 = cosY;
			float m11 = 0.0;
  			
			float m12 = 0;
			float m13 = 0;
			float m14 = 0;
			float m15 = 1.0;

			float4x4 m = float4x4(m00, m01, m02, m03,
								m04, m05, m06, m07,
								m08, m09, m10, m11,
								m12, m13, m14, m15);
			return m;
		}

		float4x4 transformZ( float3 ang ) {
			float cosZ = cos(ang.z);
			float sinZ = sin(ang.z);

			float m00 = cosZ;
			float m01 = -sinZ;
			float m02 = 0;
			float m03 = 0.0;
  			
			float m04 = sinZ; 
			float m05 = cosZ; 
			float m06 = 0;
			float m07 = 0.0;
  			
			float m08 = 0;
			float m09 = 0;
			float m10 = 1;
			float m11 = 0.0;
  			
			float m12 = 0;
			float m13 = 0;
			float m14 = 0;
			float m15 = 1.0;

			float4x4 m = float4x4(m00, m01, m02, m03,
								m04, m05, m06, m07,
								m08, m09, m10, m11,
								m12, m13, m14, m15);
			return m;
		}

		float4x4 build_transform(float3 pos, float3 ang) 
		{
			float cosX = cos(ang.x);
			float sinX = sin(ang.x);
			float cosY = cos(ang.y);
			float sinY = sin(ang.y);
			float cosZ = cos(ang.z);
			float sinZ = sin(ang.z);
		
			float m00 = cosX;
			float m01 = 0;
			float m02 = sinX;
			float m03 = 0.0;
  			
			float m04 =  sinY * sinX; 
			float m05 =  cosY; 
			float m06 = -sinY * cosX;
			float m07 = 0.0;
  			
			float m08 = -cosY * sinX;
			float m09 =  sinY;
			float m10 =  cosY * cosX;
			float m11 = 0.0;
  			
			float m12 = pos.x;
			float m13 = pos.y;
			float m14 = pos.z;
			float m15 = 1.0;
			
			float4x4 m = float4x4(m00, m01, m02, m03,
								m04, m05, m06, m07,
								m08, m09, m10, m11,
								m12, m13, m14, m15);
			return m;
		}

		ENDCG

		Pass
		{
			Name "RangedFloat" // 0 - Float Input
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			float _InputFloat;
			
			float4 frag( v2f_img i ) : SV_Target
			{
				return _InputFloat;
			}
			ENDCG
		}

		Pass
		{
			Name "AllVectors" // 1 - Vector Input
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			float4 _InputVector;

			float4 frag( v2f_img i ) : SV_Target
			{
				return _InputVector;
			}
			ENDCG
		}

		Pass
		{
			Name "MaskingPort" // 2 - Used for masking output ports
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			float _Port;
			sampler2D _MaskTex;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = tex2D( _MaskTex, i.uv );
				float4 c = 0;
				if ( _Port == 1 )
					c = a.x;
				else if ( _Port == 2 )
					c = a.y;
				else if ( _Port == 3 )
					c = a.z;
				else if ( _Port == 4 )
					c = a.w;

				return c;
			}
			ENDCG
		}

		Pass
		{
			Name "UVtexCoord" // 3 - UV texcoord vertex node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag( v2f_img i ) : SV_Target
			{
				return float4( i.uv, 0, 0 );
			}
			ENDCG
		}

		Pass
		{
			Name "UVtexCoordTillingOffset" // 4 - UV texcoord node fragment with tilling and offset
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Tilling;
			sampler2D _Offset;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 t = tex2D( _Tilling, i.uv );
				float4 o = tex2D( _Offset, i.uv );
				return float4( i.uv * t.xy + o.xy, 0, 0 );
			}
			ENDCG
		}

		Pass
		{
			Name "SimpleAdd" // 5 - Simple Add Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return a + b;
			}
			ENDCG
		}

		Pass
		{
			Name "SimpleMultiply" // 6 - Simple Multiply Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return a * b;
			}
			ENDCG
		}

		Pass
		{
			Name "SimpleSubtract" // 7 - Simple Subtract Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Atexsub;
			sampler2D _Btexsub;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = tex2D( _Atexsub, i.uv );
				float4 b = tex2D( _Btexsub, i.uv );
				return a - b;
			}
			ENDCG
		}

		Pass
		{
			Name "ComponentMask" // 8 - Component mask node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _MaskTex;
			float4 _Ports;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = tex2D( _MaskTex, i.uv );
				return a * _Ports;
			}
			ENDCG
		}


		Pass
		{
			Name "Relay" // 9 - Relay Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Relay;

			float4 frag(v2f_img i) : SV_Target
			{
				return tex2D(_Relay, i.uv);
			}
			ENDCG
		}
				
		Pass
		{
			Name "Append" // 10 - Append node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _X;
			sampler2D _Y;
			sampler2D _Z;
			sampler2D _W;

			float4 frag(v2f_img i) : SV_Target
			{
				float x = tex2D(_X, i.uv).x;
				float y = tex2D(_Y, i.uv).y;
				float z = tex2D(_Z, i.uv).z;
				float w = tex2D(_W, i.uv).w;

				return float4(x,y,z,w);
			}
			ENDCG
		}
		
		Pass
		{
			Name "WorldNormal" // 11 - World Normal node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _WorldNormal;

			float4 frag(v2f_img i) : SV_Target
			{
				float2 p = 2 * i.uv - 1;
				float r = sqrt( dot(p,p) );
				if ( r < 1 )
				{
					float2 uvs;
					float f = ( 1 - sqrt( 1 - r ) ) / r;
					uvs.x = p.x * f;
					uvs.y = p.y * f;
					float3 normal = normalize(float3( uvs, f-1));
					return float4(normal, 1);
					//float3 n = normalize(_WorldSpaceCameraPos);
					//float3 forward = float3( 0,0,1);
					//float3 a = atan2( n, forward );
					////return a.z;
					//float3 supY = float3( n.y, -n.x, n.z);
					//float3 supX = float3( n.y, n.x, n.z);
					//float3 supZ = float3( 0, a.z, 0);
					//
					//return atan2(n.x , n.y);

					//float4x4 transX = transformX( supX );
					//float4x4 transY = transformY( supZ );
					//float4x4 transZ = transformZ( supZ );
					////float4x4 trans = build_transform( float3(0,0,0), sup );

					//normal = mul( transY, normal ).rgb;
					////normal = mul( transY, normal ).rgb;
					////normal =  mul( transZ, mul( transY, mul(transX, normal).rgb).rgb).rgb;
					//return float4(normal, 1);
				}
				else {
					return 0;
				}

			}
			ENDCG
		}

		Pass
		{
			Name "WorldViewDir" // 12 - World view dir node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _WorldViewDir;

			float4 frag(v2f_img i) : SV_Target
			{
				float2 p = 2 * i.uv - 1;
				float r = sqrt( dot(p,p) );
				if ( r < 1 )
				{
					float2 uvs;
					float f = ( 1 - sqrt( 1 - r ) ) / r;
					uvs.x = p.x;
					uvs.y = p.y;// *f;
					float3 worldPos = normalize(float3(0,0,-5) - (float3( uvs,f-1)));



					return float4((worldPos), 1);
				}
				else {
					return 0;
				}


				/*normalize(UnityWorldSpaceViewDir(worldPos));*/

				return tex2D(_WorldViewDir, i.uv);
			}
			ENDCG
		}

		Pass
		{
			Name "TangentViewDir" // 13 - World view dir node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _TangentViewDir;

			float4 frag(v2f_img i) : SV_Target
			{
				return tex2D(_TangentViewDir, i.uv);
			}
			ENDCG
		}
		
		Pass
		{
			Name "Panner" // 14 - UV panner node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _UVs;
			sampler2D _PanTime;
			float _UsingEditor;
			float _SpeedY;
			float _SpeedX;

			float4 frag(v2f_img i) : SV_Target
			{
				float time = _EditorTime;
				if ( _UsingEditor == 0 ) 
				{
					time = tex2D( _PanTime, i.uv ).r;
				}

				return tex2D( _UVs, i.uv) + time * float4( _SpeedX, _SpeedY, 0, 0 );
			}
			ENDCG
		}

		Pass
		{
			Name "Fract" // 15 - Fract node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _Tex;

			float4 frag(v2f_img i) : SV_Target
			{
				return frac(tex2D(_Tex, i.uv));
			}
			ENDCG
		}

		Pass
		{
			Name "Normalize" // 16 - Normalize node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _Tex;

			float4 frag(v2f_img i) : SV_Target
			{
				return float4(normalize(tex2D(_Tex, i.uv).rgb), 1);
			}
			ENDCG
		}

		Pass
		{
			Name "Dot" // 17 - Dot node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Atex;
			sampler2D _Btex;

			float4 frag(v2f_img i) : SV_Target
			{
				return dot(tex2D(_Atex, i.uv).rgb, tex2D(_Btex, i.uv).rgb);
			}
			ENDCG
		}

		Pass
		{
			Name "Saturate" // 18 - Saturate node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return saturate(tex2D(_A, i.uv));
			}
			ENDCG
		}

		Pass
		{
			Name "OneMinus" // 19 - One Minus node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return 1-tex2D(_A, i.uv);
			}
			ENDCG
		}

		Pass
		{
			Name "Power" // 20 - Power node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Atex;
			sampler2D _Btex;

			float4 frag(v2f_img i) : SV_Target
			{
				return pow(tex2D(_Atex, i.uv),tex2D(_Btex, i.uv));
			}
			ENDCG
		}

		Pass
		{
			Name "MultiplyMatrix" // 21 - Simple Multiply Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float _MatrixId;
			sampler2D _Btexmul;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 b = tex2D( _Btexmul, i.uv );
				return b;
				//return mul(getUnityMatrix(_MatrixId), b);
			}
			ENDCG
		}

		Pass
		{
			Name "Sampler" // 22 - Sampler Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _Sampler;
			sampler2D _UVs;
			float _CustomUVs;
			float _NotLinear;

			float4 frag( v2f_img i ) : SV_Target
			{
				float2 uvs = i.uv;
				if ( _CustomUVs == 1 )
					uvs = tex2D( _UVs, i.uv ).xy;
				float4 c = tex2D( _Sampler, uvs);
				if ( _NotLinear ) {
					c.rgb = LinearToGammaSpace( c );
				}
				return c;
			}
			ENDCG
		}

		Pass
		{
			Name "WorldPos" // 23 - World view dir node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag(v2f_img i) : SV_Target
			{
				float2 p = 2 * i.uv - 1;
				float r = sqrt( dot(p,p) );
				if ( r < 1 )
				{
					float2 uvs;
					float f = ( 1 - sqrt( 1 - r ) ) / r;
					uvs.x = p.x;
					uvs.y = p.y;
					float3 worldPos = float3( uvs, f-1);

					return float4 (worldPos, 1);
				}
				else {
					return 0;
				}
			}
			ENDCG
		}

		Pass
		{
			Name "PreviewTime" // 24 - Time Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 t = _EditorTime;
				t.x = _EditorTime / 20;
				t.z = _EditorTime * 2;
				t.w = _EditorTime * 3;
				return t;
			}
			ENDCG
		}

		Pass
		{
			Name "SinTime" // 25 - Sin Time Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 t = _EditorTime;
				t.x = _EditorTime / 8;
				t.y = _EditorTime / 4;
				t.z = _EditorTime / 2;
				return sin(t);
			}
			ENDCG
		}

		Pass
		{
			Name "CosTime" // 26 - Cos Time Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 t = _EditorTime;
				t.x = _EditorTime / 8;
				t.y = _EditorTime / 4;
				t.z = _EditorTime / 2;
				return cos(t);
			}
			ENDCG
		}

		Pass
		{
			Name "DeltaTime" // 27 - Delta Time Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 t = _EditorDeltaTime;
				t.y = 1 / _EditorDeltaTime;
				t.z = _EditorTime;
				t.w = 1 / _EditorTime;
				return cos(t);
			}
			ENDCG
		}

		Pass
		{
			Name "Rotator" // 28 - UV rotator node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _UVs;
			sampler2D _Anchor;
			sampler2D _RotTimeTex;
			float _UsingEditor;

			float4 frag(v2f_img i) : SV_Target
			{
				float time = _EditorTime;
				if ( _UsingEditor == 0 ) 
				{
					time = tex2D( _RotTimeTex, i.uv ).r;
				}

				float cosT = cos( time );
				float sinT = sin( time );

				float2 a = tex2D( _Anchor, i.uv ).rg;
				return float4( mul( tex2D( _UVs, i.uv ).xy - a, float2x2( cosT, -sinT, sinT, cosT ) ) + a, 0, 1 );
			}
			ENDCG
		}

		Pass
		{
			Name "SinOp" // 29 - Sin Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _Atex;

			float4 frag(v2f_img i) : SV_Target
			{
				return sin(tex2D( _Atex, i.uv ));
			}
			ENDCG
		}

		Pass
		{
			Name "AbsOp" // 30 - Abs Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Atex;

			float4 frag(v2f_img i) : SV_Target
			{
				return abs(tex2D( _Atex, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "CeilOp" // 31 - Ceil Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Atex;

			float4 frag(v2f_img i) : SV_Target
			{
				return ceil(tex2D( _Atex, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "ClampOp" // 32 - Clamp Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Value;
			sampler2D _Min;
			sampler2D _Max;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 value = tex2D( _Value, i.uv );
				float4 min = tex2D( _Min, i.uv );
				float4 max = tex2D( _Max, i.uv );
				return clamp(value, min, max);
			}
			ENDCG
		}
				
		Pass
		{
			Name "DDXOp" // 33 - DDX Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _DDX;

			float4 frag(v2f_img i) : SV_Target
			{
				return ddx(tex2D( _DDX, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "DDYOp" // 34 - DDY Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _DDY;

			float4 frag(v2f_img i) : SV_Target
			{
				return ddy(tex2D( _DDY, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "DistanceOp" // 35 - Distance Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return distance(a, b);
			}
			ENDCG
		}
				
		Pass
		{
			Name "DivideOp" // 36 - Divide Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return a / b;
			}
			ENDCG
		}
				
		Pass
		{
			Name "ExpOp" // 37 - Exp Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return exp(tex2D( _A, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "Exp2Op" // 38 - Exp2 Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return exp2(tex2D( _A, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "FloorOp" // 39 - Floor Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return floor(tex2D( _A, i.uv ));
			}
			ENDCG
		}

		Pass
		{
			Name "FmodOp" // 40 - Fmod Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return fmod(a, b);
			}
			ENDCG
		}
				
		Pass
		{
			Name "FWidthOp" // 41 - FWidth Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return fwidth(tex2D( _A, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "LengthOp" // 42 - Length Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return length(tex2D( _A, i.uv ));
			}
			ENDCG
		}

				
		Pass
		{
			Name "LerpOp" // 43 - Lerp Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;
			sampler2D _Alpha;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				float4 alpha = tex2D( _Alpha, i.uv );
				return lerp(a,b,alpha);
			}
			ENDCG
		}
				
		Pass
		{
			Name "LogOp" // 44 - Log Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return log(tex2D( _A, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "Log10Op" // 45 - Log10 Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return log10(tex2D( _A, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "Log2Op" // 46 - Log2 Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return log2(tex2D( _A, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "MaxOp" // 47 - Max Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return max( a, b );
			}
			ENDCG
		}

		Pass
		{
			Name "MinOp" // 48 - Min Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return min( a, b );
			}
			ENDCG
		}
				
		Pass
		{
			Name "NegateOp" // 49 - Negate Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return -(tex2D( _A, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "RemainderOp" // 50 - Remainder Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return ( a % b );
			}
			ENDCG
		}
				
		Pass
		{
			Name "RemapOp" // 51 - Remap Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Value;
			sampler2D _MinOld;
			sampler2D _MaxOld;
			sampler2D _MinNew;
			sampler2D _MaxNew;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 value = tex2D( _Value, i.uv );
				float4 minold = tex2D( _MinOld, i.uv );
				float4 maxold = tex2D( _MaxOld, i.uv );
				float4 minnew = tex2D( _MinNew, i.uv );
				float4 maxnew = tex2D( _MaxNew, i.uv );

				return (minnew + (value - minold) * (maxnew - minnew) / (maxold - minold));
			}
			ENDCG
		}

		Pass
		{
			Name "RoundOp" // 52 - Round Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return round(tex2D( _A, i.uv ));
			}
			ENDCG
		}

		Pass
		{
			Name "Rsqrt" // 53 - Rsqrt Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return rsqrt(tex2D( _A, i.uv ));
			}
			ENDCG
		}

		Pass
		{
			Name "ScaleOffset" // 54 - ScaleOffset Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Value;
			sampler2D _Scale;
			sampler2D _Offset;

			float4 frag(v2f_img i) : SV_Target
			{
				float4	v = tex2D( _Value, i.uv );
				float4	s = tex2D( _Scale, i.uv );
				float4	o = tex2D( _Offset, i.uv );
				return v * s + o;
			}
			ENDCG
		}

		Pass
		{
			Name "SamplerNormal" // 55 - Sampler Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityStandardUtils.cginc"

			sampler2D _Sampler;
			sampler2D _UVs;
			sampler2D _NormalScale;
			float _CustomUVs;

			float4 frag( v2f_img i ) : SV_Target
			{
				float2 uvs = i.uv;
				if ( _CustomUVs == 1 )
					uvs = tex2D( _UVs, i.uv ).xy;
				float n = tex2D( _NormalScale, uvs ).r;
				float4 c = tex2D( _Sampler, uvs );
				float3 u = UnpackScaleNormal(c, n);
				return float4( LinearToGammaSpace(u), 1);
			}
			ENDCG
		}

		Pass
		{
			Name "SignOp" // 56 - Sign Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return sign(tex2D( _A, i.uv ));
			}
			ENDCG
		}

		Pass
		{
			Name "SimplifiedFmodOp" // 57 - SFmod Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return frac( a / b ) * b;
			}
			ENDCG
		}

		Pass
		{
			Name "SmoothStepOp" // 58 - SmoothStep Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Alpha;
			sampler2D _Min;
			sampler2D _Max;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 min = tex2D( _Min, i.uv );
				float4 max = tex2D( _Max, i.uv );
				float4 alpha = tex2D( _Alpha, i.uv );

				return smoothstep(min, max, alpha);
			}
			ENDCG
		}

		Pass
		{
			Name "SqrtOp" // 59 - Sqrt Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return sqrt(tex2D( _A, i.uv ));
			}
			ENDCG
		}

		Pass
		{
			Name "StepOp" // 60 - Step Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return step( a, b );
			}
			ENDCG
		}

		Pass
		{
			Name "TruncOp" // 61 - Trunc Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return trunc(tex2D( _A, i.uv ));
			}
			ENDCG
		}

		Pass
		{
			Name "Cross" // 62 - Cross Vector
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Lhs;
			sampler2D _Rhs;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 l = tex2D( _Lhs, i.uv );
				float4 r = tex2D( _Rhs, i.uv );
				return float4(cross(l.rgb, r.rgb),1);
			}
			ENDCG
		}
				
		Pass
		{
			Name "Reflect" // 63 - Reflect Vector
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Incident;
			sampler2D _Normal;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 inc = tex2D( _Incident, i.uv );
				float4 nor = tex2D( _Normal, i.uv );
				return reflect(inc, nor);
			}
			ENDCG
		}
				
		Pass
		{
			Name "Refract" // 64 - Refract vector
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _Incident;
			sampler2D _Normal;
			sampler2D _Eta;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 inc = tex2D( _Incident, i.uv );
				float4 nor = tex2D( _Normal, i.uv );
				float4 eta = tex2D( _Eta, i.uv );
				return refract( inc, nor, eta );
			}
			ENDCG
		}
				
		Pass
		{
			Name "VertexColor" // 65 - Vertex Color
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				fixed4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			v2f vert( appdata_t v )
			{
				v2f o = (v2f)0;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = v.texcoord;
				o.color = v.color;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				return i.color;
			}
			ENDCG
		}
				
		Pass
		{
			Name "BlendNormals" // 66 - Blend Normals
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityStandardUtils.cginc"

			sampler2D _A;
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				float3 a = tex2D( _A, i.uv ).rgb;
				float3 b = tex2D( _B, i.uv ).rgb;
				return float4(BlendNormals(a, b), 0);
			}
			ENDCG
		}
				
		Pass
		{
			Name "CosOp" // 67 - Cos Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return cos(tex2D( _A, i.uv ));
			}
			ENDCG
		}
				
		Pass
		{
			Name "TanOp" // 68 - Tan Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return tan(tex2D( _A, i.uv ));
			}
			ENDCG
		}
								
		Pass
		{
			Name "PiNode" // 69 - Pi Love
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return tex2D( _A, i.uv ).r * UNITY_PI;
			}
			ENDCG
		}
								
		Pass
		{
			Name "TauNode" // 70 - Tau Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			float4 frag(v2f_img i) : SV_Target
			{
				return UNITY_PI * 2;
			}
			ENDCG
		}
								
		Pass
		{
			Name "DegreesNode" // 71 - Degrees Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return degrees(tex2D( _A, i.uv ));
			}
			ENDCG
		}
								
		Pass
		{
			Name "AsinOP" // 72 - Asin Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return asin(tex2D( _A, i.uv ));
			}
			ENDCG
		}
								
		Pass
		{
			Name "AcosOP" // 73 - Acos Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return acos(tex2D( _A, i.uv ));
			}
			ENDCG
		}
								
		Pass
		{
			Name "AtanOP" // 74 - Atan Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return atan(tex2D( _A, i.uv ));
			}
			ENDCG
		}
								
		Pass
		{
			Name "RadiansNode" // 75 - Radians Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return radians(tex2D( _A, i.uv ));
			}
			ENDCG
		}
								
		Pass
		{
			Name "SinH" // 76 - SinH Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return sinh(tex2D( _A, i.uv ));
			}
			ENDCG
		}
								
		Pass
		{
			Name "CosH" // 77 - CosH Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return cosh(tex2D( _A, i.uv ));
			}
			ENDCG
		}
		
		Pass
		{
			Name "TanH" // 78 - TanH Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return tanh(tex2D( _A, i.uv ));
			}
			ENDCG
		}
								
		Pass
		{
			Name "Atan2" // 79 - Atan2 Op
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			sampler2D _B;

			float4 frag(v2f_img i) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				float4 b = tex2D( _B, i.uv );
				return atan2(a, b);
			}
			ENDCG
		}
		
		Pass
		{
			Name "LightColor" // 80 - Light Color Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityShaderVariables.cginc"

			float4 LightColor0;

			float4 frag(v2f_img i) : SV_Target
			{
				return LightColor0;
			}
			ENDCG
		}
								
		Pass
		{
			Name "BreakToComponents" // 81 - Break To Components
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return tex2D( _A, i.uv );
			}
			ENDCG
		}

		Pass
		{
			Name "TextureProperty" // 82 - Texture Property Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _Sampler;
			float _NotLinear;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 c = tex2D( _Sampler, i.uv);
				if ( _NotLinear ) {
					c.rgb = LinearToGammaSpace( c );
				}
				return c;
			}
			ENDCG
		}

		Pass
		{
			Name "RegVar" // 83 - Register Var
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return tex2D( _A, i.uv );
			}
			ENDCG
		}

		Pass
		{
			Name "GetVar" // 84 - Get Var
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;

			float4 frag(v2f_img i) : SV_Target
			{
				return tex2D( _A, i.uv );
			}
			ENDCG
		}

		Pass
		{
			Name "OutputMask" // 85 - Output Mask
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _MainTex;
			float4 _Ports;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = tex2D( _MainTex, i.uv );
				return a * _Ports;
			}
			ENDCG
		}

		Pass
		{
			Name "ScaleNode" // 86 - Scale Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			sampler2D _A;
			float _ScaleFloat;

			float4 frag( v2f_img i ) : SV_Target
			{
				float4 a = tex2D( _A, i.uv );
				return a * _ScaleFloat;
			}
			ENDCG
		}

		Pass
		{
			Name "LightDir" // 87 - Scale Node
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4 frag( v2f_img i ) : SV_Target
			{
				float2 p = 2 * i.uv - 1;
				float r = sqrt( dot(p,p) );
				if ( r < 1 )
				{
					float2 uvs;
					float f = ( 1 - sqrt( 1 - r ) ) / r;
					uvs.x = p.x;
					uvs.y = p.y;
					float3 worldPos = float3( uvs, f-1);

					return float4 (UnityWorldSpaceLightDir(worldPos), 1);
				}
				else {
					return 0;
				}
			}
			ENDCG
		}
	}
}
