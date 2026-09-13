using UnityEngine;
using System.Collections.Generic;
namespace JellyGarden.Scripts.Juice
{
    // LateUpdate wins over the legacy Sprite Animator's keyed candy images.
    [DisallowMultipleComponent]
    public sealed class RoyalPieceSkin : MonoBehaviour
    {
        public static readonly string[] Colours={"ruby","shield","crown","leaf","lily","vase"};
        static Sprite[] colours; static Sprite rocket,tnt,orb,heli;
        static readonly Dictionary<string,Sprite> artCache=new Dictionary<string,Sprite>();
        Item item; Vector3 originalScale; bool ready;
        public static Sprite Art(string name)
        {if(artCache.TryGetValue(name,out var sprite)&&sprite)return sprite;sprite=Resources.Load<Sprite>("RoyalGameplay/"+name);artCache[name]=sprite;return sprite;}
        public static Sprite For(ItemsTypes type,int color)
        {
            if(colours==null||!colours[0])
            { colours=new Sprite[6];for(int i=0;i<6;i++)colours[i]=Art(Colours[i]);rocket=Art("rocket");tnt=Art("tnt");orb=Art("orb");heli=Art("helicopter"); }
            if(type==ItemsTypes.BOMB)return orb;
            if(type==ItemsTypes.PACKAGE)return tnt;
            if(type==ItemsTypes.HELICOPTER)return heli;
            if(type==ItemsTypes.HORIZONTAL_STRIPPED||type==ItemsTypes.VERTICAL_STRIPPED)return rocket;
            return type==ItemsTypes.INGREDIENT?null:colours[Mathf.Clamp(color,0,5)];
        }
        void LateUpdate()
        {
            if(!ready){item=GetComponent<Item>();if(!item||!item.sprRenderer)return; originalScale=item.sprRenderer.transform.localScale;ready=true;}
            var sprite=For(item.currentType,item.color); if(!sprite)return;
            item.sprRenderer.sprite=sprite;
            if(!item.destroying)
            {
                var tr=item.sprRenderer.transform;
                tr.localScale=originalScale;
                tr.localRotation=Quaternion.Euler(0,0,item.currentType==ItemsTypes.VERTICAL_STRIPPED?90:0);
            }
        }
    }
}
