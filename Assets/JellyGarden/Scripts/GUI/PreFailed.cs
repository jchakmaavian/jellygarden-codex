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
	public class PreFailed : MonoBehaviour
	{
		public Sprite[] buyButtons;
		public Image buyButton;
		int FailedCost;
		// Use this for initialization
		void OnEnable ()
		{
			// The Continue price rises with each one bought in this attempt (RoyalEconomy: 150, 250, then 400).
			FailedCost = LevelManager.THIS.FailedCost = Juice.RoyalEconomy.ContinueCost (LevelManager.THIS.ContinuesBought);
			transform.Find ("Buy/Price").GetComponent<Text> ().text = "" + FailedCost;
			if (LevelManager.THIS.limitType == LIMIT.MOVES)
				buyButton.sprite = buyButtons [0];
			else if (LevelManager.THIS.limitType == LIMIT.TIME)
				buyButton.sprite = buyButtons [1];
			// Extra moves are bought with gems, not real money, so this offer must stay on
			// screen even when the real-money packs are off. Hiding it left the popup promising
			// "5 extra moves to finish your goals" with no way to take them.
			transform.Find ("Buy").gameObject.SetActive (true);

			SetTargets ();
		}

		void SetTargets ()
		{
			Transform TargetCheck1 = transform.Find ("Banner/Targets/TargetCheck1");
			Transform TargetCheck2 = transform.Find ("Banner/Targets/TargetCheck2");
			Transform TargetUnCheck1 = transform.Find ("Banner/Targets/TargetUnCheck1");
			Transform TargetUnCheck2 = transform.Find ("Banner/Targets/TargetUnCheck2");
			if (LevelManager.Score < LevelManager.THIS.star1) {
				TargetCheck2.gameObject.SetActive (false);
				TargetUnCheck2.gameObject.SetActive (true);
			} else {
				TargetCheck2.gameObject.SetActive (true);
				TargetUnCheck2.gameObject.SetActive (false);
			}
			if (LevelManager.THIS.target == Target.BLOCKS) {
				if (LevelManager.THIS.TargetBlocks > 0) {
					TargetCheck1.gameObject.SetActive (false);
					TargetUnCheck1.gameObject.SetActive (true);
				} else {
					TargetCheck1.gameObject.SetActive (true);
					TargetUnCheck1.gameObject.SetActive (false);
				}
			} else if (LevelManager.THIS.target == Target.INGREDIENT || LevelManager.THIS.target == Target.COLLECT) {
				if (LevelManager.THIS.ingrCountTarget [0] > 0 || LevelManager.THIS.ingrCountTarget [1] > 0) {
					TargetCheck1.gameObject.SetActive (false);
					TargetUnCheck1.gameObject.SetActive (true);
				} else {
					TargetCheck1.gameObject.SetActive (true);
					TargetUnCheck1.gameObject.SetActive (false);
				}
			} else if (LevelManager.THIS.target == Target.SCORE) {
				if (LevelManager.Score < LevelManager.THIS.star1) {
					TargetCheck1.gameObject.SetActive (false);
					TargetUnCheck1.gameObject.SetActive (true);
				} else {
					TargetCheck1.gameObject.SetActive (true);
					TargetUnCheck1.gameObject.SetActive (false);
				}
			}


		}

	}
}
