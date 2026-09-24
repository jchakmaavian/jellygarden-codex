#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    // Asset-only reskin. Animation clips, prefab transforms and gameplay timing are never rewritten.
    public static class TNTSunArt
    {
        const string Folder="Design/TNTSun";
        const string Art="Assets/ReferenceGameplay/Art/Resources/ReferenceGameplay/Sprites/Items-SpecialItems-TntItem-";
        const string Letter=Art+"Animation-tnt_yazi__22066.png";
        const string Icon=Art+"tnt__16602.png";
        const string Combo=Art+"Combo-kemer__21603.png";
        static Sprite Load(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(path);
        static SpriteRenderer Piece(Transform root,Sprite sprite,Vector2 position,int order=0){var go=new GameObject("Art layer");go.transform.SetParent(root,false);go.transform.localPosition=position;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;return sr;}
        static void Render(string path,int width,int height,Vector2 size,Action<Transform> build)
        {
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("TNT art bake");SceneManager.MoveGameObjectToScene(root,scene);
            RenderTexture rt=null;Texture2D output=null;Camera camera=null;
            try{
                build(root.transform);foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var cg=new GameObject("Art camera");SceneManager.MoveGameObjectToScene(cg,scene);camera=cg.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=size.y*.5f;camera.aspect=size.x/size.y;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;camera.allowHDR=false;camera.allowMSAA=true;
                rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=4};rt.Create();camera.targetTexture=rt;camera.Render();
                var previous=RenderTexture.active;try{RenderTexture.active=rt;output=new Texture2D(width,height,TextureFormat.RGBA32,false);output.ReadPixels(new Rect(0,0,width,height),0,0);output.Apply();}finally{RenderTexture.active=previous;}
                // Sprite blending in the transparent render target is premultiplied; PNG uses straight alpha.
                var pixels=output.GetPixels();for(int i=0;i<pixels.Length;i++){var c=pixels[i];if(c.a>.001f){c.r=Mathf.Clamp01(c.r/c.a);c.g=Mathf.Clamp01(c.g/c.a);c.b=Mathf.Clamp01(c.b/c.a);}else c=Color.clear;pixels[i]=c;}output.SetPixels(pixels);output.Apply();File.WriteAllBytes(path,output.EncodeToPNG());
            }finally{if(camera)camera.targetTexture=null;if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(output)UnityEngine.Object.DestroyImmediate(output);EditorSceneManager.ClosePreviewScene(scene);}
        }
        static void Import(string path,float ppu)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=ppu;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.maxTextureSize=1024;ti.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();ti.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;ti.SetTextureSettings(settings);
            foreach(var platform in new[]{"Standalone","Android","WebGL"}){var ps=ti.GetPlatformTextureSettings(platform);ps.overridden=false;ps.maxTextureSize=1024;ti.SetPlatformTextureSettings(ps);}ti.userData="{\"pixelsPerUnit\":"+ppu.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"pivotX\":0.5,\"pivotY\":0.5,\"borderX\":0,\"borderY\":0,\"borderZ\":0,\"borderW\":0}";ti.SaveAndReimport();
        }
        public static void Bake()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Bake artwork outside Play Mode.");
            Directory.CreateDirectory(Folder);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(Folder+"/Supplied-Sun-TNT.png"));
            // Native sprite slicing of the supplied medallion: no generated replacement artwork.
            var mesh=new Mesh();const int segments=96;var vertices=new Vector3[segments+1];var uv=new Vector2[segments+1];var indices=new int[segments*3];
            Vector2 center=new Vector2(610f/texture.width,504f/texture.height);uv[0]=center;
            for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments;var unit=new Vector2(Mathf.Cos(a),Mathf.Sin(a));vertices[i+1]=unit*.225f;uv[i+1]=center+new Vector2(unit.x*225/texture.width,unit.y*225/texture.height);indices[i*3]=0;indices[i*3+1]=(i+1)%segments+1;indices[i*3+2]=i+1;}mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;mesh.RecalculateBounds();
            var material=new Material(Shader.Find("Unlit/Transparent"));material.mainTexture=texture;
            try{Render(Letter,256,256,new Vector2(.52f,.52f),root=>{var go=new GameObject("Supplied emblem slice",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;});}finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(texture);}Import(Letter,256/.52f);
            Render(Combo,792,396,new Vector2(198/140f,99/140f),root=>{
                var band=Piece(root,Load(Art+"Animation-tnt_yazi_arka_on__21580.png"),Vector2.zero);band.transform.localScale=new Vector3((198/140f)/band.sprite.bounds.size.x,(99/140f)/band.sprite.bounds.size.y,1);
                Piece(root,Load(Letter),new Vector2(0,-.025f),1).transform.localScale=Vector3.one*1.20f;
            });Import(Combo,560);
            Render(Icon,536,508,new Vector2(134/140f,127/140f),root=>{
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ReferenceGameplay/Animation/Resources/ReferenceGameplay/Visuals/TNT.prefab");var view=UnityEngine.Object.Instantiate(prefab,root,false);
                foreach(var animator in view.GetComponentsInChildren<Animator>(true))animator.enabled=false;
                foreach(var sr in view.GetComponentsInChildren<SpriteRenderer>(true))if(sr.name=="Main"||sr.name=="Base"||sr.name.IndexOf("Shadow",StringComparison.OrdinalIgnoreCase)>=0)sr.gameObject.SetActive(false);
            });Import(Icon,560);
            File.Copy(Icon,Folder+"/TNT-Sun.png",true);File.Copy(Letter,Folder+"/Sun-Badge.png",true);File.Copy(Combo,Folder+"/Combo-Band.png",true);
            File.WriteAllText(Folder+"/bake.txt","Updated three TNT texture assets in place. Original GUIDs retained. Animation and VFX assets untouched.\n");
        }
#if !ROYAL_DISABLE_QA
        static void Check(bool ok,string text){File.AppendAllText(Folder+"/checks.txt",(ok?"PASS ":"FAIL ")+text+"\n");if(!ok)throw new InvalidOperationException(text);}
        static IEnumerator Shot(string name){yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"/"+name+".png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}
        public static IEnumerator Run()
        {
            File.WriteAllText(Folder+"/checks.txt","TNT sun artwork checks\n");var qa=RoyalQaService.Instance;int gems=InitScript.Gems;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            try{
                qa.StartSession();yield return qa.Practice();qa.Visible=false;
                Check(RoyalPieceSkin.Art("tnt")==ReferenceGameplayCatalog.Art("tnt")&&RoyalGameplayArt.Art("tnt")==ReferenceGameplayCatalog.Art("tnt"),"Menus, shop and board share the sun TNT artwork");
                var lm=LevelManager.THIS;var item=lm.GetSquare(4,4).item;item.NextType=ItemsTypes.PACKAGE;item.ChangeType();yield return new WaitForSeconds(.18f);yield return Shot("TNT-reveal");yield return new WaitForSeconds(1);
                var label=item.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(x=>x.name=="TntYazi");Check(label&&label.sprite==Load(Letter),"Existing animated lettering layer now renders the sun badge");yield return Shot("TNT-idle");
                Check(RoyalPowerupDirector.Tap(item),"Existing TNT activation accepts input");yield return new WaitForSeconds(.12f);yield return Shot("TNT-activation");yield return new WaitForSeconds(.20f);yield return Shot("TNT-explosion");yield return new WaitForSeconds(2);
                yield return qa.Practice();qa.Visible=false;item=lm.GetSquare(4,4).item;item.NextType=ItemsTypes.PACKAGE;item.ChangeType();yield return new WaitForSeconds(1);label=item.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(x=>x.name=="TntYazi");Check(label&&label.sprite==Load(Letter),"Pooled TNT reuse retains the sun badge");
                yield return qa.EndSession();Check(!RoyalSave.Testing&&InitScript.Gems==gems,"Normal player data restored");File.AppendAllText(Folder+"/checks.txt","ALL CHECKS PASSED\n");
            }finally{if(RoyalSave.Testing)RoyalThrone.Editor.RoyalRegressionRunner.Run(qa.EndSession());}
        }
#endif
    }
}
#endif
