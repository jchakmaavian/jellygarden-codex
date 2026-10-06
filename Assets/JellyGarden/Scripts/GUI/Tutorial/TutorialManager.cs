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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.GUI.TutorialStuff
{
	public class TutorialManager : MonoBehaviour
	{
		public List<Item> items = new List<Item> ();
		public GameObject tutorial;
		public GameObject text;
		public GameObject canvas;
		bool showed;

		void OnEnable ()
		{
			LevelManager.OnLevelLoaded += CheckNewTarget;
			LevelManager.OnStartPlay += DisableTutorial;
		}

		void OnDisable ()
		{
			LevelManager.OnLevelLoaded -= CheckNewTarget;
			LevelManager.OnStartPlay -= DisableTutorial;
			// Leaving the level (the Level object is deactivated) before the first touch left the hand active and
			// bound to pieces that are then destroyed; re-entering level 1 re-enabled it with them and threw.
			// Start clean: the tutorial is shown again for the new board, since the player never dismissed it.
			if (tutorial)
				tutorial.SetActive (false);
			items.Clear ();
		}

		void DisableTutorial ()
		{
			if (!showed && LevelManager.THIS.currentLevel == 1) {
				ChangeLayerNum (0);
				tutorial.SetActive (false);
				showed = true;
			}
		}


		void CheckNewTarget ()
		{
			if (LevelManager.THIS.currentLevel == 1 && !showed)
				StartCoroutine (WaitForCombine ());

		}

		void ShowStarsTutorial ()
		{
			//canvas.transform.position = Vector3.down * FindMaxY(items);
			tutorial.SetActive (true);
			ChangeLayerNum (4);
			// The verified first level's goal is crates, and its first lesson says so (Design/RoyalMatchReference).
			var words = text ? text.GetComponent<UnityEngine.UI.Text> () : null;
			if (words && RoyalThrone.Campaign.RoyalReferenceCampaign.Opening)
				words.text = "Match next to a crate to smash it!";
			KeepCaptionClearOfBoard ();
		}

		// The caption keeps its authored height, which suited the old 9x5 first board; on a taller board its first line
		// covered the bottom row. When they overlap it moves down to just below the board (the canvas is in world space,
		// like the pieces, so heights compare directly).
		void KeepCaptionClearOfBoard ()
		{
			var lm = LevelManager.THIS;
			var rt = text ? text.transform as RectTransform : null;
			if (!lm || !rt || lm.maxRows < 2) return;
			var canvasOfText = rt.GetComponentInParent<Canvas> ();
			if (canvasOfText && canvasOfText.renderMode == RenderMode.ScreenSpaceOverlay) return;
			var last = lm.GetSquare (0, lm.maxRows - 1); var previous = lm.GetSquare (0, lm.maxRows - 2);
			if (!last || !previous) return;
			float cell = Mathf.Abs (previous.transform.position.y - last.transform.position.y);
			float below = last.transform.position.y - cell * .62f;
			float top = rt.TransformPoint (new Vector3 (0, rt.rect.yMax, 0)).y;
			if (top > below) rt.position += Vector3.down * Mathf.Min (top - below, cell * 1.5f);
		}

		IEnumerator WaitForCombine ()
		{
			yield return new WaitUntil (() => AI.THIS.GetCombine () != null);
//		bool keepWaiting = true;
//		while (keepWaiting) {
//			yield return new WaitForFixedUpdate ();
//			if (AI.THIS.GetCombine () != null)
//				keepWaiting = false;
//		}
			items = AI.THIS.GetCombine ();


			if (items.Count == 0)
				yield break;
			items.Sort (SortByDistance);
			if (LevelManager.THIS.currentLevel == 1 && !showed) {
				ShowStarsTutorial ();
			}
		}

		public Vector3[] GetItemsPositions ()
		{
			// A move made without a touch (the bot, QA tools) destroys pieces under the hand before it is dismissed.
			items.RemoveAll (item => !item);
			Vector3[] positions = new Vector3[items.Count];
			for (int i = 0; i < items.Count; i++) {
				positions [i] = items [i].transform.position + new Vector3 (1, -1, 0);
			}
			return positions;
		}

		private int SortByDistance (Item item1, Item item2)
		{
			Item itemFirst = items [0];
			float x = Vector3.Distance (itemFirst.transform.position, item1.transform.position);
			float y = Vector3.Distance (itemFirst.transform.position, item2.transform.position);
			int retval = y.CompareTo (x);

			if (retval != 0) {
				return retval;
			} else {
				return y.CompareTo (x);
			}
		}

		public int FindMaxY (List<Item> list)
		{
			int max = int.MinValue;
			foreach (Item type in list) {
				if (type.transform.position.y > max) {
					max = (int)type.transform.position.y + 2;
				}
			}
			return max;
		}

		void ChangeLayerNum (int num)
		{
			foreach (var item in items) {
				if (item) {

					item.square.GetComponent<SpriteRenderer> ().sortingOrder = num;
					item.sprRenderer.sortingOrder = num + 2;
				}
			}
		}

	}
}
