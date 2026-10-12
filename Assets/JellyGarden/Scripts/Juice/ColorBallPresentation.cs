using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    // Runtime presentation of the locked artwork; no source-frame or gameplay mutations.
    public static class ColorBallPresentation
    {
        static Material material;
        static readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        public static void Apply(SpriteRenderer view,Sprite next,float blend,float width,float height,float energy,float envelope=1)
        {
            if(!material)material=new Material(Resources.Load<Shader>("RoyalColorBall/ColorBallPresentation")){name="Color Ball frame presentation"};
            view.sharedMaterial=material;
            properties.Clear();properties.SetTexture("_NextTex",next.texture);
            properties.SetFloat("_FrameBlend",blend);properties.SetFloat("_DiscWidth",width);
            properties.SetFloat("_DiscHeight",height);properties.SetFloat("_Energy",energy);
            properties.SetFloat("_Envelope",envelope);view.SetPropertyBlock(properties);
        }
    }
}
