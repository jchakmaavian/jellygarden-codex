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
        Item item; Vector3 originalScale, itemRestScale; bool ready, scaleReady;
        BoxCollider2D hitBox;Vector2 fittedCell;
        // Combined with the wider camera framing, the visible pieces are about 1.5x
        // their previous screen size. Fit the cell instead of letting adjacent art overlap.
        public const float CellFill = .94f;
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
        void OnEnable(){ApplyNow();}
        void LateUpdate(){ApplyNow();}
        public void ApplyNow()
        {
            if(!ready){item=GetComponent<Item>();if(!item||!item.sprRenderer)return; originalScale=item.sprRenderer.transform.localScale;hitBox=GetComponent<BoxCollider2D>();ready=true;}
            var sprite=For(item.currentType,item.color);
            if(item.currentType==ItemsTypes.INGREDIENT)sprite=RoyalThrone.Presentation.RoyalVisualCatalog.Replace(item.sprRenderer.sprite);
            if(!sprite)return;
            item.sprRenderer.sprite=sprite;
            if(!item.destroying)
            {
                var tr=item.sprRenderer.transform;
                if(!scaleReady&&item.square){itemRestScale=item.transform.localScale;scaleReady=true;}
                var lm=LevelManager.THIS;
                if(scaleReady&&lm){
                    var size=sprite.bounds.size;
                    float fit=Mathf.Min(lm.squareWidth/Mathf.Max(.01f,size.x*Mathf.Abs(itemRestScale.x)),lm.squareHeight/Mathf.Max(.01f,size.y*Mathf.Abs(itemRestScale.y)))*CellFill;
                    tr.localScale=new Vector3(fit,fit,originalScale.z);
                    // The inherited collider covered only 73% of a cell. Keep the
                    // enlarged visible edges tappable while leaving a gap to neighbours.
                    var cell=new Vector2(lm.squareWidth,lm.squareHeight);
                    if(hitBox&&fittedCell!=cell){
                        hitBox.size=new Vector2(cell.x/Mathf.Max(.01f,Mathf.Abs(itemRestScale.x)),cell.y/Mathf.Max(.01f,Mathf.Abs(itemRestScale.y)))*.96f;
                        hitBox.offset=Vector2.zero;fittedCell=cell;
                    }
                }else tr.localScale=originalScale;
                tr.localRotation=Quaternion.Euler(0,0,item.currentType==ItemsTypes.VERTICAL_STRIPPED?90:0);
            }
        }
    }
}
