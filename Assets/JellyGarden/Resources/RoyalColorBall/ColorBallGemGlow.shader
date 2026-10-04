// A Color Ball target's light, cut to the gem's own silhouette (2026-10-04). Drawn on a quad a little larger than the
// gem's sprite so the glow can reach past the sprite's rectangle; samples outside the sprite read as empty, so the shape
// is always the gem's outline and never its rectangle. Mode 0 sits behind the gem: a bright band hugging the outline
// that fades into a soft glow in the gem's colour. Mode 1 sits on the gem, added on top: a thin rim just inside the
// edge, a shine sweeping across, and a gem-shaped flash.
Shader "RoyalThrone/Color Ball Gem Glow"
{
 Properties
 {
  [PerRendererData] _MainTex("Gem",2D)="white"{}
  [PerRendererData] _UVRect("Sprite UV rect (min, size)",Vector)=(0,0,1,1)
  [PerRendererData] _Map("Quad UV to sprite fraction (scale, offset)",Vector)=(1,1,0,0)
  [PerRendererData] _Unit("Gem width in sprite fractions",Vector)=(1,1,0,0)
  [PerRendererData] _Tint("Gem colour",Color)=(1,.85,.4,1)
  [PerRendererData] _Color("Strength",Color)=(1,1,1,1)
  [PerRendererData] _Mode("0 glow behind, 1 light on the gem",Float)=0
  [PerRendererData] _Grow("Glow breath 0-1",Float)=.5
  [PerRendererData] _Flash("Gem-shaped flash",Float)=0
  [PerRendererData] _Shine("Shine sweep position",Float)=-1
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane"}
  Cull Off ZWrite Off Lighting Off
  // Premultiplied: alpha 1 blends normally, alpha 0 adds light.
  Blend One OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;float2 f:TEXCOORD0;};
   sampler2D _MainTex;float4 _UVRect,_Map,_Unit,_Tint,_Color;float _Mode,_Grow,_Flash,_Shine;
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.f=v.uv*_Map.xy+_Map.zw;return o;}
   // The gem's alpha at a point given as a fraction of its sprite; zero outside the sprite.
   float A(float2 f)
   {
    float2 inside=step(0,f)*step(f,1);
    return tex2Dlod(_MainTex,float4(_UVRect.xy+saturate(f)*_UVRect.zw,0,0)).a*inside.x*inside.y;
   }
   fixed4 frag(v2f i):SV_Target
   {
    float a=A(i.f);
    if(_Mode<.5)
    {
     // edge: a hairline of light right at the outline; band: the gem's colour, lit, around it; glow: soft, beyond.
     float edge=0,band=0,glow=0;
     float r0=.014,r1=.035+.012*_Grow,r2=.075+.025*_Grow,r3=.13+.04*_Grow;
     [unroll] for(int k=0;k<12;k++)
     {
      float t=k*.5235988;float2 d=float2(cos(t),sin(t))*_Unit.xy;
      edge=max(edge,A(i.f+d*r0));
      band=max(band,A(i.f+d*r1));
      glow+=A(i.f+d*r2)*.6+A(i.f+d*r3)*.4;
     }
     glow=saturate(glow/12*1.7);
     float alpha=saturate(max(band*.92,glow*.8))*_Color.a;
     float3 lit=lerp(_Tint.rgb,float3(1,1,1),.42);
     float3 col=lerp(lerp(_Tint.rgb,lit,band),float3(1,.99,.94),edge*.75);
     return fixed4(col*alpha,alpha);
    }
    float inner=a;
    [unroll] for(int k=0;k<8;k++){float t=k*.7853982;inner=min(inner,A(i.f+float2(cos(t),sin(t))*_Unit.xy*.035));}
    float rim=saturate(a-inner);
    float s=(i.f.x+i.f.y)*.5-_Shine;float shine=exp(-s*s/.004)*a;
    float light=saturate(rim*.6+shine*.5+_Flash*a)*_Color.a;
    return fixed4(float3(1,.98,.92)*light,0);
   }
   ENDCG
  }
 }
}
