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

namespace JellyGarden.Scripts.Leadboard
{
	public class LeadboardObject : MonoBehaviour {
		public Image icon;
		public Text place;
		public Text playerName;
		public Text score;
		#if PLAYFAB || GAMESPARKS
	private LeadboardPlayerData playerData;

	public LeadboardPlayerData PlayerData {
		get {
			return playerData;
		}

		set {
			playerData = value;
			SetupIcon ();
		}
	}

	void SetupIcon () {
		StartCoroutine (WaitForPicture ());
	}

	IEnumerator WaitForPicture () {
		print ("wait for picture");
		yield return new WaitUntil (() => PlayerData != null);
		yield return new WaitUntil (() => PlayerData.friendData != null);
		if (PlayerData.friendData.picture == null) {//2.1.2
			FacebookManager.THIS.LoggedSuccefull ();
			FacebookManager.THIS.GetFriendsPicture ();

	}
		yield return new WaitUntil (() => PlayerData.friendData.picture != null);
		PlayerData.picture = PlayerData.friendData.picture;
		icon.sprite = PlayerData.picture;
		place.text = "" + PlayerData.position;
		playerName.text = PlayerData.Name;
		score.text = "" + PlayerData.score;
		if (NetworkManager.THIS.IsYou (PlayerData.userID)) {
			playerName.text = "YOU";
			playerName.color = Color.red;
			//if (LevelManager.THIS.gameStatus == GameState.Win) {
			//    score.text = "" + PlayerPrefs.GetInt("Score" + LevelManager.THIS.currentLevel);
			//   }
		}

	}

		#endif

	}
}
