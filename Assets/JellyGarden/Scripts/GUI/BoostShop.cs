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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.GUI
{
    public enum BoostType
    {
        ExtraMoves,
        Packages,
        Stripes,
        ExtraTime,
        Bomb,
        Colorful_bomb,
        Hand,
        Random_color,
        None
    }

    public class BoostShop : MonoBehaviour {
        public Sprite[] icons;
        public string[] descriptions;
        public int[] prices;
        public Image icon;
        public Text description;

        BoostType boostType;

        public List<BoostProduct> boostProducts = new List<BoostProduct>();

        // Use this for initialization
        void Start () {
	
        }

        void Update()
        {
        }
	
        // Update is called once per frame
        public void SetBoost (BoostType _boostType ) 
        {
            boostType = _boostType;
            gameObject.SetActive( true );
            icon.sprite = boostProducts[(int)_boostType].icon;
            if(Juice.RoyalEffectsProfile.Get){
                string art=_boostType==BoostType.ExtraMoves||_boostType==BoostType.ExtraTime?"crown":_boostType==BoostType.Packages?"tnt":_boostType==BoostType.Stripes?"rocket":_boostType==BoostType.Hand?"hammer":"orb";
                icon.sprite=Juice.RoyalPieceSkin.Art(art);icon.preserveAspect=true;
            }
            description.text = boostProducts[(int)_boostType].description;
            for (int i = 0; i < 3; i++)
            {
                transform.Find("Image/BuyBoost" + (i + 1) + "/Count").GetComponent<Text>().text = "x" + boostProducts[(int)_boostType].count[i];
                transform.Find("Image/BuyBoost" + (i + 1) + "/Price").GetComponent<Text>().text = "" + boostProducts[(int)_boostType].GemPrices[i];
            }
        }

        public void BuyBoost(GameObject button)
        {
            int count = int.Parse( button.transform.Find("Count").GetComponent<Text>().text.Replace("x",""));
            int price = int.Parse( button.transform.Find("Price").GetComponent<Text>().text);
            GetComponent<AnimationManager>().BuyBoost(boostType, price, count);
        }
    }

    [global::System.Serializable]
    public class BoostProduct
    {
        public Sprite icon;
        public string description;
        public int[] count;
        public int[] GemPrices;
    }
}
