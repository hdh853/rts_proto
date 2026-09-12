Shader "BackpackRTS/PrototypeToon" {
 Properties { _Color ("Color", Color) = (1,1,1,1) }
 SubShader { Tags { "RenderType"="Opaque" } LOD 100
  Pass { Tags { "LightMode"="ForwardBase" }
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fwdbase
   #include "UnityCG.cginc"
   #include "Lighting.cginc"
   #include "AutoLight.cginc"
   struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
   struct v2f { float4 pos:SV_POSITION; float3 normal:TEXCOORD0; SHADOW_COORDS(1) };
   fixed4 _Color;
   v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.normal=UnityObjectToWorldNormal(v.normal); TRANSFER_SHADOW(o); return o; }
   fixed4 frag(v2f i):SV_Target { float light=max(0,dot(normalize(i.normal),normalize(_WorldSpaceLightPos0.xyz))); light=floor(light*4)/4; float shadow=SHADOW_ATTENUATION(i); return fixed4(_Color.rgb*(.43+light*shadow*.72),1); }
   ENDCG
  }
  UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
 }
 Fallback "Diffuse"
}
