using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.UI;
namespace RoyalThrone.Presentation
{
    // Keep existing menu logic and button events; replace its wood/candy presentation.
    public static class RoyalMenuSkin
    {
        public static void Apply(Transform root,Font display)
        {
            foreach(string name in new[]{"MenuPlay","MenuPause","MenuComplete","MenuFailed","PreFailed"})
            {
                var menu=root.Find(name);if(!menu)continue;
                foreach(var text in menu.GetComponentsInChildren<Text>(true))
                {text.font=display;text.color=new Color(1,.96f,.82f);text.resizeTextForBestFit=true;text.resizeTextMinSize=16;if(text.text=="Choose the boost:")text.text="CHOOSE YOUR BOOSTERS";if(text.text=="Target:")text.text="GOALS";}
                foreach(var image in menu.GetComponentsInChildren<Image>(true))
                {
                    if(!image.sprite)continue;string art=image.sprite.name.ToLowerInvariant();
                    if(image.name=="Image"&&image.transform.parent==menu)
                    {Surface(image);continue;}
                    if(image.GetComponent<Button>()&&image.name.ToLowerInvariant().Contains("play"))
                    {
                        Surface(image);var labels=image.GetComponentsInChildren<Text>(true);
                        foreach(var label in labels)label.text="PLAY";
                        if(labels.Length==0)
                        {
                            var labelObject=new GameObject("Royal Play label",typeof(RectTransform));var rect=labelObject.GetComponent<RectTransform>();rect.SetParent(image.transform,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                            var label=labelObject.AddComponent<Text>();label.text="PLAY";label.font=display;label.fontSize=44;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(1,.96f,.82f);label.raycastTarget=false;
                        }
                    }
                    if(art.Contains("panel")||art.Contains("wood")||art.Contains("target_back")||art.Contains("score_back"))Surface(image);
                }
                var main=menu.Find("Image");if(!main)continue;
                string[] boosts={"Boost1","Boost2","Boost3"};string[] arts={"orb","rocket","tnt"};
                for(int i=0;i<boosts.Length;i++)foreach(var t in main.GetComponentsInChildren<Transform>(true))if(t.name==boosts[i])
                {var image=t.GetComponent<Image>();if(image){image.sprite=RoyalPieceSkin.Art(arts[i]);image.color=Color.white;image.preserveAspect=true;}}
            }
        }
        static void Surface(Image image)
        {
            if(image.transform.Find("Royal surface"))return;
            image.color=Color.clear;
            var go=new GameObject("Royal surface",typeof(RectTransform));var rect=go.GetComponent<RectTransform>();rect.SetParent(image.transform,false);rect.SetAsFirstSibling();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var surface=go.AddComponent<RoyalPanelGraphic>();surface.topColor=new Color(.13f,.39f,.8f);surface.bottomColor=new Color(.025f,.12f,.34f);surface.cornerRadius=28;surface.raycastTarget=false;
            var button=image.GetComponent<Button>();if(button)button.targetGraphic=surface;
        }
    }
}
