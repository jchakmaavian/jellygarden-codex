// // ©2015 - 2026 Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.GUI
{
    public class Tutorial : MonoBehaviour {
        public Sprite[] sprites;
        public Image image;
        int i;
        // The Royal lessons (RoyalLessonDemo, 2026-09-30): six pages, each a small board that plays its lesson on a loop.
        int Last => Juice.RoyalEffectsProfile.Get ? RoyalThrone.Presentation.RoyalLessonDemo.Count - 1 : sprites.Length - 1;
        // How to Play always opens on its first lesson (the last one closes it with GOT IT).
        void OnEnable(){if(Juice.RoyalEffectsProfile.Get){i=0;SetImage();}}
        public void Next()
        {
            // The last lesson's button reads GOT IT: it closes How to Play the way the card's close does.
            if (Juice.RoyalEffectsProfile.Get && i >= Last)
            {
                var close = transform.Find("Image/Close");
                var button = close ? close.GetComponent<Button>() : null;
                if (button) { button.onClick.Invoke(); return; }
            }
            i++;
            i = Mathf.Clamp(i, 0, Last);
            SetImage();
        }
        public void Back()
        {
            i--;
            i = Mathf.Clamp(i, 0, Last);
            SetImage();
        }

        void SetImage()
        {
            if(Juice.RoyalEffectsProfile.Get){
                image.enabled=false;var p=transform.Find("Image");
                for(int n=0;n<RoyalThrone.Presentation.RoyalLessonDemo.Count;n++){var old=p.Find("Royal lesson "+n);if(old)old.gameObject.SetActive(n==i);}
                if(!p.Find("Royal lesson "+i))RoyalThrone.Presentation.RoyalLessonDemo.Build(p,i,RoyalThrone.Presentation.RoyalCards.LessonPageY);
                RoyalThrone.Presentation.RoyalCards.LessonProgress(p,i);
                return;
            }
            image.sprite = sprites[i];
            image.SetNativeSize();
        }
    }
}
