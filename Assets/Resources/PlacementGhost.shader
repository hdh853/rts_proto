Shader "BackpackRTS/PlacementGhost" {
 Properties { _Color("Color",Color)=(0.2,1,0.4,0.4) }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha OneMinusSrcAlpha ZWrite Off
  Pass { CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _Color;
   struct appdata {float4 vertex:POSITION;};struct v2f {float4 position:SV_POSITION;};
   v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);return o;}
   fixed4 frag(v2f i):SV_Target{return _Color;}
  ENDCG }
 }
}
