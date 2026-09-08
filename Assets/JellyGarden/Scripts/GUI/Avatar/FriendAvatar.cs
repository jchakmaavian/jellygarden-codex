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
using JellyGarden.Scripts.Integrations;
using JellyGarden.Scripts.MapScripts;
using UnityEngine;
using UnityEngine.UI;

namespace JellyGarden.Scripts.GUI.Avatar
{
	public class FriendAvatar : MonoBehaviour, IAvatarLoader {
		public Image image;
		private FriendData friendData;

		public FriendData FriendData {
			get {
				return friendData;
			}

			set {
				friendData = value;
				if (friendData != null)
					ShowPicture ();
			}
		}

		void OnEnable () {
			Hide ();
		}

		public void ShowPicture () {
			StartCoroutine (WaitForPicture ());
		}

		IEnumerator WaitForPicture () {
			yield return new WaitUntil (() => FriendData.picture != null);
//		print ("picture on map");//TODO check player's picture
			GetComponent<SpriteRenderer> ().enabled = true;
			image.sprite = FriendData.picture;
			image.enabled = true;
			SetPosition ();
		}

		void SetPosition () {
			MapLevel level = LevelsMap._instance.GetLevel (FriendData.level);
			if (level != null)
				transform.position = level.transform.position - new Vector3 (0.5f, 0.5f, 0);

		}

		void Hide () {
			GetComponent<SpriteRenderer> ().enabled = false;
			image.enabled = false;
		}

		void OnDisable () {
			Hide ();
		}

	}
}
