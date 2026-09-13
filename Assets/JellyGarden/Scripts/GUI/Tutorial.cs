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
        void OnEnable(){if(Juice.RoyalEffectsProfile.Get)SetImage();}
        public void Next()
        {
            i++;
            i = Mathf.Clamp(i, 0, Juice.RoyalEffectsProfile.Get?4:sprites.Length-1);
            SetImage();
        }
        public void Back()
        {
            i--;
            i = Mathf.Clamp(i, 0, Juice.RoyalEffectsProfile.Get?4:sprites.Length-1);
            SetImage();
        }

        void SetImage()
        {
            if(Juice.RoyalEffectsProfile.Get){
                image.enabled=false;var p=transform.Find("Image");
                for(int n=0;n<5;n++){var old=p.Find("Royal lesson "+n);if(old)old.gameObject.SetActive(n==i);}
                var page=p.Find("Royal lesson "+i);if(page)return;
                page=new GameObject("Royal lesson "+i,typeof(RectTransform)).transform;page.SetParent(p,false);
                RoyalThrone.Presentation.RoyalMenuSkin.Place(page,0,0,530,430);
                string[] titles={"MATCH THREE","MAKE A ROCKET","MAKE TNT","MAKE A HELICOPTER","MAKE A MAGIC ORB"};
                string[] copy={"Swap neighbouring pieces to match three.\nEvery valid move brings you closer.","Match four in a straight line.\nTap the rocket to clear its row or column.","Match five in a T or L shape.\nTNT clears the surrounding area.","Match a square of four.\nA helicopter flies to a useful target.","Match five in a straight line.\nThe orb clears one colour."};
                RoyalThrone.Presentation.RoyalMenuSkin.Label(page,"Title",titles[i],0,192,500,52,26);
                int count=i==0?3:i==1||i==3?4:5;
                for(int n=0;n<count;n++){
                    float x=(n-(count-1)*.5f)*78,y=90;
                    if(i==2){x=(n<3?(n-1)*78:0)-60;y=n<3?110:110-(n-2)*78;}
                    if(i==3){x=(n%2-.5f)*78;y=120-(n/2)*78;}
                    RoyalThrone.Presentation.RoyalMenuSkin.Art(page,"Piece "+n,"ruby",x,y,74,74);
                }
                if(i>0)RoyalThrone.Presentation.RoyalMenuSkin.Art(page,"Power",new[]{"","rocket","tnt","helicopter","orb"}[i],i==2?160:0,i==2?35:-65,115,115);
                RoyalThrone.Presentation.RoyalMenuSkin.Label(page,"Rule",copy[i],0,-163,510,95,23);
                return;
            }
            image.sprite = sprites[i];
            image.SetNativeSize();
        }
    }
}
