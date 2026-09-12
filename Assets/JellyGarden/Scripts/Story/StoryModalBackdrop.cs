using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.Story
{
    /// <summary>A cached blurred view beneath a modal; input stays blocked during capture.</summary>
    public sealed class StoryModalBackdrop : MonoBehaviour
    {
        private RawImage _image;
        private CanvasGroup _visuals;
        private Material _material;
        private RenderTexture _texture;
        private Coroutine _capture;
        private Vector2Int _size;

        public void Initialize()
        {
            _visuals=gameObject.AddComponent<CanvasGroup>();
            _image=StoryUi.CreateRect("BlurredHome",transform).gameObject.AddComponent<RawImage>();
            StoryUi.Stretch(_image.rectTransform);
            _image.transform.SetAsFirstSibling();
            _image.raycastTarget=false;
            _image.enabled=false;
        }
        public void Capture()
        {
            if(_capture!=null) StopCoroutine(_capture);
            _capture=null;
            Release();
            _visuals.alpha=1;
            _size=new Vector2Int(Screen.width,Screen.height);
            if(!Application.isPlaying||!isActiveAndEnabled||
                SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            Shader shader=Resources.Load<Shader>("Story/UI/WinBackdropBlur");
            if(shader==null||!shader.isSupported) return;
            if(_material==null) _material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            // Alpha hides the modal for the snapshot while the scrim still raycasts.
            _visuals.alpha=0;
            _capture=StartCoroutine(CaptureFrame());
        }
        private IEnumerator CaptureFrame()
        {
            // Let underlying CanvasScalers and safe-area frames settle after a resize.
            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            RenderTexture full=null,scratch=null,previous=RenderTexture.active;
            try
            {
                int w=Mathf.Max(1,Screen.width),h=Mathf.Max(1,Screen.height);
                full=RenderTexture.GetTemporary(w,h,0);
                StoryWindow.CaptureInto(full);
                int sw=Mathf.Max(1,w/2),sh=Mathf.Max(1,h/2);
                _texture=RenderTexture.GetTemporary(sw,sh,0);
                scratch=RenderTexture.GetTemporary(sw,sh,0);
                _texture.filterMode=scratch.filterMode=FilterMode.Bilinear;
                _texture.wrapMode=scratch.wrapMode=TextureWrapMode.Clamp;
                Graphics.Blit(full,_texture);
                _material.SetVector("_Direction",new Vector4(1f/sw,0,0,0));
                Graphics.Blit(_texture,scratch,_material);
                _material.SetVector("_Direction",new Vector4(0,1f/sh,0,0));
                Graphics.Blit(scratch,_texture,_material);
                _image.texture=_texture;
                // Metal/D3D back buffers run top-down; uGUI texture UVs run bottom-up.
                _image.uvRect=SystemInfo.graphicsUVStartsAtTop?new Rect(0,1,1,-1):new Rect(0,0,1,1);
                _image.enabled=true;
            }
            catch(global::System.Exception e)
            {
                Release();
                Debug.LogWarning("[Story] Modal backdrop unavailable: "+e.Message);
            }
            finally
            {
                RenderTexture.active=previous;
                if(full!=null) RenderTexture.ReleaseTemporary(full);
                if(scratch!=null) RenderTexture.ReleaseTemporary(scratch);
                _visuals.alpha=1;
                _capture=null;
            }
        }
        private void LateUpdate()
        {
            if(_size!=new Vector2Int(Screen.width,Screen.height)) Capture();
        }
        private void Release()
        {
            if(_image!=null){_image.texture=null;_image.enabled=false;}
            if(_texture!=null){RenderTexture.ReleaseTemporary(_texture);_texture=null;}
        }
        private void OnDisable()
        {
            StopAllCoroutines();_capture=null;
            if(_visuals!=null) _visuals.alpha=1;
            Release();
        }
        private void OnDestroy()
        {
            Release();
            if(_material!=null) Destroy(_material);
        }
    }
}
