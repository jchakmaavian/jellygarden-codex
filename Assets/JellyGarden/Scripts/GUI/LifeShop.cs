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
	public class LifeShop : MonoBehaviour
	{
		public int CostIfRefill = 12;
		Text nextLife;
		// Use this for initialization
		void OnEnable ()
		{
			CostIfRefill = Juice.RoyalEconomy.LivesRefill;   // the coin rules, not the inspector value
			transform.Find ("Image/BuyLife/Price").GetComponent<Text> ().text = "" + CostIfRefill;
			// A refill costs gems, not real money. Hiding it with the real-money packs left the
			// out-of-lives popup with a heart, one line of text and no action at all.
			transform.Find ("Image/BuyLife").gameObject.SetActive (true);
		}

		void Update ()
		{
			// The popup is the one place a player waits on the refill, so it shows the countdown.
			if (nextLife == null)
			{
				Transform found = transform.Find ("Image/Royal next life");
				if (found == null) return;
				nextLife = found.GetComponent<Text> ();
				if (nextLife == null) return;
			}
			// The countdown comes from the one life timer (UpdateLives sets RestLifeTimer) and reads m:ss, as on Home ("4:59"),
			// rather than the hidden legacy counter's "04:59" (UI polish, 2026-09-30).
			InitScript.Instance.UpdateLives ();
			int seconds = Mathf.Max (0, Mathf.CeilToInt (InitScript.RestLifeTimer));
			nextLife.text = InitScript.lifes >= InitScript.Instance.CapOfLife
				? "Your lives are full"
				: "Next life in " + (seconds / 60) + ":" + (seconds % 60).ToString ("00");
		}
	}
}
