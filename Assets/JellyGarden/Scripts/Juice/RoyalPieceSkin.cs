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
        SpriteRenderer contactShadow;
        // Sized against the cell, not the art, so every piece grounds the same way.
        const float ShadowWidth=.66f,ShadowHeight=.22f,ShadowDrop=.40f,ShadowAlpha=.30f;
        bool hintPose;Vector3 hintRestPosition,hintOffset;float hintScale=1;
        // Combined with the wider camera framing, the visible pieces are about 1.5x
        // their previous screen size. Fit the cell instead of letting adjacent art overlap.
        public const float CellFill = .94f;
        public const float RocketIdleCellFill = .78f;
        public const float MaxDestroyCellFill = 1.12f;
        public static Sprite Art(string name)
        {if(artCache.TryGetValue(name,out var sprite)&&sprite)return sprite;sprite=Resources.Load<Sprite>("RoyalGameplay/"+name);artCache[name]=sprite;return sprite;}
        public static Sprite For(ItemsTypes type,int color)
        {return RoyalGameplayArt.For(type,color)??LegacyFor(type,color);}
        public static Sprite LegacyFor(ItemsTypes type,int color)
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
        public void SetHintPose(Vector3 localOffset,float scale)
        {
            if(!ready)ApplyNow();if(!item||!item.sprRenderer||item.destroying)return;
            if(!hintPose)hintRestPosition=item.sprRenderer.transform.localPosition;
            hintPose=true;hintOffset=localOffset;hintScale=Mathf.Clamp(scale,1,1.035f);ApplyNow();
        }
        public void ClearHintPose()
        {
            if(!hintPose)return;hintPose=false;
            if(item&&item.sprRenderer)item.sprRenderer.transform.localPosition=hintRestPosition;
            hintOffset=Vector3.zero;hintScale=1;ApplyNow();
        }
        /// <summary>
        /// Keeps the piece's contact ellipse under it. Boosters run their own rigs above the
        /// board and keep the shadow too: it is what reads as the cell they occupy.
        /// </summary>
        void PoseContactShadow(LevelManager lm)
        {
            if(!lm||!item||!item.sprRenderer)return;
            bool wanted=!item.destroying&&item.currentType!=ItemsTypes.INGREDIENT;
            if(!contactShadow)
            {
                if(!wanted)return;
                var go=new GameObject("Royal contact shadow");
                go.transform.SetParent(transform,false);
                contactShadow=go.AddComponent<SpriteRenderer>();
                contactShadow.sprite=RoyalContactShadow.Blob;
                contactShadow.sharedMaterial=RoyalContactShadow.Material;
            }
            contactShadow.enabled=wanted;
            if(!wanted)return;
            var source=item.sprRenderer;
            contactShadow.sortingLayerID=source.sortingLayerID;
            contactShadow.sortingOrder=source.sortingOrder-1;
            contactShadow.color=new Color(.02f,.05f,.16f,ShadowAlpha);
            float scale=Mathf.Abs(transform.lossyScale.x)<.0001f?1:Mathf.Abs(transform.lossyScale.x);
            var tr=contactShadow.transform;
            tr.localRotation=Quaternion.identity;
            tr.localPosition=new Vector3(0,-lm.squareHeight*ShadowDrop/scale,.05f);
            tr.localScale=new Vector3(lm.squareWidth*ShadowWidth/scale,lm.squareHeight*ShadowHeight/scale,1);
        }

        public void ApplyNow()
        {
            if(!ready){item=GetComponent<Item>();if(!item||!item.sprRenderer)return; originalScale=item.sprRenderer.transform.localScale;hitBox=GetComponent<BoxCollider2D>();ready=true;}
            var sprite=For(item.currentType,item.color);
            if(item.currentType==ItemsTypes.INGREDIENT)sprite=RoyalThrone.Presentation.RoyalVisualCatalog.Replace(item.sprRenderer.sprite,true);
            if(!sprite)return;
            item.sprRenderer.sprite=sprite;
            // The legacy Animator keys Sprite.localScale to one. Keep the static
            // art fit while the Item parent supplies the code-driven destroy pose.
            bool codeDestroy=item.destroying&&item.currentType==ItemsTypes.NONE&&JuiceConfig.Instance.EnableJuice;
            if(!item.destroying||codeDestroy)
            {
                var tr=item.sprRenderer.transform;
                if(!scaleReady&&item.square){itemRestScale=item.transform.localScale;scaleReady=true;}
                var lm=LevelManager.THIS;
                if(scaleReady&&lm){
                    var size=sprite.bounds.size;
                    bool idleRocket=item.currentType==ItemsTypes.HORIZONTAL_STRIPPED||item.currentType==ItemsTypes.VERTICAL_STRIPPED;
                    float fill=idleRocket?RocketIdleCellFill:CellFill;
                    float fit=Mathf.Min(lm.squareWidth/Mathf.Max(.01f,size.x*Mathf.Abs(itemRestScale.x)),lm.squareHeight/Mathf.Max(.01f,size.y*Mathf.Abs(itemRestScale.y)))*fill;
                    tr.localScale=new Vector3(fit,fit,originalScale.z);
                    // The inherited collider covered only 73% of a cell. Keep the
                    // enlarged visible edges tappable while leaving a gap to neighbours.
                    var cell=new Vector2(lm.squareWidth,lm.squareHeight);
                    if(!item.destroying&&hitBox&&fittedCell!=cell){
                        hitBox.size=new Vector2(cell.x/Mathf.Max(.01f,Mathf.Abs(itemRestScale.x)),cell.y/Mathf.Max(.01f,Mathf.Abs(itemRestScale.y)))*.96f;
                        hitBox.offset=Vector2.zero;fittedCell=cell;
                    }
                }else tr.localScale=originalScale;
                PoseContactShadow(lm);
                tr.localRotation=Quaternion.Euler(0,0,item.currentType==ItemsTypes.VERTICAL_STRIPPED&&!RoyalGameplayArt.Art("rocket-vertical")?90:0);
                if(!item.destroying&&hintPose){tr.localPosition=hintRestPosition+hintOffset;tr.localScale*=hintScale;}
                if(item.currentType==ItemsTypes.PACKAGE&&!item.destroying){ TNT3DAnimator.Find(item)?.PoseSprite(tr); TNTFrameAnimator.Attach(item); }
                if(item.currentType==ItemsTypes.BOMB&&!item.destroying) ColorBallFrameAnimator.Attach(item);
                if(item.currentType==ItemsTypes.HELICOPTER&&!item.destroying) RoyalPropellerPieceV2.Attach(item);
                if((item.currentType==ItemsTypes.HORIZONTAL_STRIPPED||item.currentType==ItemsTypes.VERTICAL_STRIPPED)&&!item.destroying) RoyalRocketPieceAnimator.Attach(item)?.Pose(tr);
                if(codeDestroy&&lm&&lm.GameField)
                {
                    Vector3 visible=item.sprRenderer.bounds.size;
                    float maxX=lm.squareWidth*Mathf.Abs(lm.GameField.lossyScale.x)*MaxDestroyCellFill;
                    float maxY=lm.squareHeight*Mathf.Abs(lm.GameField.lossyScale.y)*MaxDestroyCellFill;
                    float cap=Mathf.Min(1,maxX/Mathf.Max(.001f,visible.x),maxY/Mathf.Max(.001f,visible.y));
                    tr.localScale*=cap;
                }
            }
            // A piece on its way out drops its shadow with it, in the same frame.
            else if(contactShadow)contactShadow.enabled=false;
        }
    }
}
