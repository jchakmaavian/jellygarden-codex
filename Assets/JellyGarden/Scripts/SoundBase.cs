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

namespace JellyGarden.Scripts
{
	[RequireComponent (typeof(AudioSource))]
	public class SoundBase : MonoBehaviour {
		public static SoundBase Instance;
		public AudioClip click;
		public AudioClip[] swish;
		public AudioClip[] drop;
		public AudioClip alert;
		public AudioClip timeOut;
		public AudioClip[] star;
		public AudioClip[] gameOver;
		public AudioClip cash;

		public AudioClip[] destroy;
		public AudioClip boostBomb;
		public AudioClip boostColorReplace;
		public AudioClip explosion;
		public AudioClip getStarIngr;
		public AudioClip strippedExplosion;
		public AudioClip[] complete;
		public AudioClip block_destroy;
		public AudioClip wrongMatch;
		public AudioClip noMatch;
		public AudioClip appearStipedColorBomb;
		public AudioClip appearPackage;
		public AudioClip destroyPackage;
		public AudioClip colorBombExpl;

		private AudioSource _audioSource;

		///SoundBase.Instance.audio.PlayOneShot( SoundBase.Instance.kreakWheel );

		// Use this for initialization
		void Awake () {
			if (Instance && Instance != this) { Destroy(gameObject); return; }
			Instance = this;
			transform.SetParent(null);
			DontDestroyOnLoad(gameObject);
			_audioSource = GetComponent<AudioSource>();
		}
	
		// Update is called once per frame
		void Update () {
	
		}

		public void PlaySound(AudioClip clip) {
            if (!clip) return;
            var royal = Juice.RoyalAudioDirector.Get;
            if (royal)
            {
                if (Contains(destroy, clip)) royal.Play(Juice.RoyalCue.Match);
                else if (Contains(drop, clip)) royal.Play(Juice.RoyalCue.Land);
                else if (clip == click || clip == wrongMatch || clip == noMatch || clip == alert || clip == timeOut || Contains(gameOver,clip)) royal.Play(Juice.RoyalCue.Click);
                else if (clip == block_destroy) royal.Play(Juice.RoyalCue.Match,.85f);
                else if (clip == strippedExplosion) royal.Play(Juice.RoyalCue.Rocket);
                else if (clip == boostBomb || clip == explosion || clip == destroyPackage || clip == colorBombExpl) royal.Play(Juice.RoyalCue.Tnt);
                else if (clip == appearStipedColorBomb || clip == appearPackage || clip == boostColorReplace || clip == cash || clip == getStarIngr) royal.Play(Juice.RoyalCue.Create);
                else if (Contains(star,clip) || Contains(complete,clip)) royal.Play(Juice.RoyalCue.Celebrate);
                else if (Contains(swish,clip)) royal.Play(Juice.RoyalCue.Click,.65f);
                else royal.PlayClip(clip, .7f);
                return;
            }
			if (_audioSource && clip)
				_audioSource.PlayOneShot(clip);
		}

        static bool Contains(AudioClip[] clips, AudioClip clip) => clips != null && global::System.Array.IndexOf(clips,clip) >= 0;

		public void SetVolume(float volume) {
			if (_audioSource)
				_audioSource.volume = volume;
		}

		public float GetVolume() {
			return _audioSource ? _audioSource.volume : 0f;
		}
	}
}
