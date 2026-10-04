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
			Vector3[] positions = tutorialManager.GetItemsPositions ();
			StartCoroutine (AnimateHand (positions));
		}

		IEnumerator AnimateHand (Vector3[] positions)
		{
			float speed = 1;
			int posNum = 0;
			if (!AI.THIS || !AI.THIS.tipItem) yield break;   // its piece is gone (see TutorialManager.GetItemsPositions)

//		for (int i = 0; i < 2; i++) {
			transform.position = AI.THIS.tipItem.transform.position;
			posNum++;
			Vector3 offset = new Vector3 (0.5f, -1f);
			Vector2 startPos = transform.position + offset;
			Vector2 endPos = transform.position + AI.THIS.vDirection + offset;
			float distance = Vector3.Distance (startPos, endPos);
			// A hint without a swipe direction gives zero distance: the lerp below would divide by it, set a NaN position
			// (an error each physics step) and re-arm at once. Rest the hand on the piece instead.
			if (distance < .001f) { transform.position = startPos; yield return new WaitForSeconds (1f); PrepareAnimateHand (); yield break; }
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
