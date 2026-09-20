using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
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
	public class BoostIcon : MonoBehaviour
	{
		public Text boostCount;
		public BoostType type;
		bool check;

		void OnEnable ()
		{
			if (name != "Main Camera") {
				if (LevelManager.THIS != null) {
					if (LevelManager.THIS.gameStatus == GameState.Map) {
                        var mark=transform.Find("Indicator/Image/Check");
                        if(mark)mark.gameObject.SetActive(false);
                    }
//				if (!LevelManager.THIS.enableInApps)
//					gameObject.SetActive (false);
				}

			}
		}

		public void ActivateBoost ()
		{
			if (LevelManager.THIS.ActivatedBoost == this) {
				UnCheckBoost ();
				return;
			}

			if (check )//2.2.1
			{
				if (type == BoostType.Colorful_bomb) {
					LevelManager.THIS.BoostColorfullBomb = 0;
					UnCheck();
					return;
				}
				if (type == BoostType.Packages) {
					LevelManager.THIS.BoostPackage = 0;
					UnCheck();
					return;
				}
				if (type == BoostType.Stripes) {
					LevelManager.THIS.BoostStriped = 0;
					UnCheck();
					return;
				}
			}

			if (IsLocked () || check || (LevelManager.THIS.gameStatus != GameState.Playing && LevelManager.THIS.gameStatus != GameState.Map))
				return;
			if (!check && BoostCount () > 0) {
				if (type != BoostType.Colorful_bomb && type != BoostType.Packages && type != BoostType.Stripes && !LevelManager.THIS.DragBlocked)
					LevelManager.THIS.ActivatedBoost = this;
				if (type == BoostType.Colorful_bomb) {
					LevelManager.THIS.BoostColorfullBomb = 1;
					Check ();
				}
				if (type == BoostType.Packages) {
					LevelManager.THIS.BoostPackage = 5;
					Check ();
				}
				if (type == BoostType.Stripes) {
					LevelManager.THIS.BoostStriped = 5;
					Check ();
				}

			} else {
				OpenBoostShop (type);
			}
		}

		void UnCheckBoost ()
		{
			LevelManager.THIS.activatedBoost = null;
			LevelManager.THIS.UnLockBoosts ();
		}

		public void InitBoost ()
		{
			transform.Find ("Indicator/Image/Check").gameObject.SetActive (false);
			transform.Find ("Indicator/Image/Count").gameObject.SetActive (true);
			LevelManager.THIS.BoostColorfullBomb = 0;
			LevelManager.THIS.BoostPackage = 0;
			LevelManager.THIS.BoostStriped = 0;
			check = false;
		}

		void Check ()
		{
			check = true;
			transform.Find ("Indicator/Image/Check").gameObject.SetActive (true);
			transform.Find ("Indicator/Image/Count").gameObject.SetActive (false);
			//InitScript.Instance.SpendBoost(type);
		}	
	
		void UnCheck ()//2.2.1
		{
			check = false;
			transform.Find ("Indicator/Image/Check").gameObject.SetActive (false);
			transform.Find ("Indicator/Image/Count").gameObject.SetActive (true);
		}

		public void LockBoost ()
		{
			transform.Find ("Lock").gameObject.SetActive (true);
			transform.Find ("Indicator").gameObject.SetActive (false);
		}

		public void UnLockBoost ()
		{
			transform.Find ("Lock").gameObject.SetActive (false);
			transform.Find ("Indicator").gameObject.SetActive (true);
		}

		bool IsLocked ()
		{
			return transform.Find ("Lock").gameObject.activeSelf;
		}

		int BoostCount ()
		{
			return int.Parse (boostCount.text);
		}

		public void OpenBoostShop (BoostType boosType)
		{
			SoundBase.Instance.PlaySound(SoundBase.Instance.click);
			GameObject.Find ("CanvasGlobal").transform.Find ("BoostShop").gameObject.GetComponent<BoostShop> ().SetBoost (boosType);
		}

		void ShowPlus (bool show)
		{
			transform.Find ("Indicator").Find ("Plus").gameObject.SetActive (show);
		}


		void Update ()
		{
			if (boostCount != null) {
				boostCount.text = "" + PlayerPrefs.GetInt ("" + type);
				if (!check) {
					if (BoostCount () > 0)
						ShowPlus (false);
					else
						ShowPlus (true);
				}
			}
		}
	}
}
