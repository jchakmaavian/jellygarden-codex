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
using UnityEngine;

namespace JellyGarden.Scripts.GUI.TutorialStuff
{
	public class HandTutorial : MonoBehaviour
	{
		public TutorialManager tutorialManager;

		void OnEnable ()
		{
			PrepareAnimateHand ();
		}

		void PrepareAnimateHand ()
		{
			// STORY: the call to tutorialManager.GetItemsPositions() was here and
			// threw a NullReferenceException on every loop of the tutorial hand.
			// Its list holds Items that have already been matched and destroyed,
			// and it reads .transform off each one.
			//
			// Nothing was lost by dropping it: AnimateHand never read the array.
			// It takes every position it uses from AI.THIS.tipItem and
			// AI.THIS.vDirection, so the result was computed, thrown on, and
			// discarded. Restoring the original behaviour is restoring the line.
			StartCoroutine (AnimateHand ());
		}

		IEnumerator AnimateHand ()
		{
			// STORY: the hand restarts itself at the end of every pass, so it
			// comes back around after the player has made the move it was
			// pointing at — and by then AI.THIS.tipItem is the destroyed Item
			// that used to be there. Every line below reads it. Stopping is what
			// "there is no move to point at" means; throwing was not.
			if (AI.THIS == null || AI.THIS.tipItem == null)
				yield break;

			float speed = 1;
			int posNum = 0;

//		for (int i = 0; i < 2; i++) {
			int i = 0;
			if (AI.THIS.combineType == CombineType.VShape)
				i = 1;
			transform.position = AI.THIS.tipItem.transform.position;
			posNum++;
			Vector3 offset = new Vector3 (0.5f, -1f);
			Vector2 startPos = transform.position + offset;
			Vector2 endPos = transform.position + AI.THIS.vDirection + offset;
			float distance = Vector3.Distance (startPos, endPos);
			float fracJourney = 0;
			float startTime = Time.time;

			while (fracJourney < 1) {
				float distCovered = (Time.time - startTime) * speed;
				fracJourney = distCovered / distance;
				transform.position = Vector2.Lerp (startPos, endPos, fracJourney);
				yield return new WaitForFixedUpdate ();
			}
//		}
			yield return new WaitForFixedUpdate ();
			PrepareAnimateHand ();
		}
	}
}
