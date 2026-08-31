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

namespace JellyGarden.Scripts.GUI
{
	public class BoostAnimation : MonoBehaviour
	{
		public Square square;

		public void ShowEffect ()
		{
			GameObject partcl = Instantiate (Resources.Load ("Prefabs/Effects/Firework"), transform.position, Quaternion.identity) as GameObject;
			partcl.GetComponent<ParticleSystem> ().startColor = LevelManager.THIS.scoresColors [square.item.color];
			if (name.Contains ("random_color_item"))//1.6.1 new lollipop
				partcl.GetComponent<ParticleSystem> ().startColor = Color.white;
			Destroy (partcl, 1f);

			if (name.Contains ("random_color_item")) {//1.6.1 new lollipop
				GameObject p = Instantiate (Resources.Load ("Prefabs/Effects/CircleExpl"), transform.position, Quaternion.identity) as GameObject;
				Destroy (p, 0.4f);

			}
		}

		public void OnFinished (BoostType boostType)
		{
			if (boostType == BoostType.Random_color) {

				List<Item> itemsList = LevelManager.THIS.GetItemsAround (square);
				foreach (Item item in itemsList) {
					if (item != null) {
						if (item.currentType == ItemsTypes.NONE)
							item.GenColor (-1, true);
					}
				}

			}
			if (boostType == BoostType.Bomb) {
				square.item.DestroyItem ();
			}
			LevelManager.THIS.StartCoroutine (LevelManager.THIS.FindMatchDelay ());
			SoundBase.Instance.PlaySound(SoundBase.Instance.explosion);

			Destroy (gameObject);
		}
	}
}
