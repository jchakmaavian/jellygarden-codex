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
			if (transform.parent == null) {
				transform.parent = Camera.main.transform;
				transform.localPosition = Vector3.zero;
			}
			DontDestroyOnLoad (gameObject);
			if (Instance == null)
				Instance = this;
			else if (Instance != this)
				Destroy (gameObject);

			_audioSource = GetComponent<AudioSource>();
		}
	
		// Update is called once per frame
		void Update () {
	
		}

		public void PlaySound(AudioClip clip) {
            var royal = Juice.RoyalAudioDirector.Get;
            if (royal)
            {
                if (global::System.Array.IndexOf(destroy, clip) >= 0) royal.Play(Juice.RoyalCue.Match);
                else if (global::System.Array.IndexOf(drop, clip) >= 0) royal.Play(Juice.RoyalCue.Land);
                else if (clip == click || clip == wrongMatch) royal.Play(Juice.RoyalCue.Click);
                else if (clip == block_destroy) royal.Play(Juice.RoyalCue.Match,.85f);
                else royal.PlayClip(clip, .7f);
                return;
            }
			if (_audioSource && clip)
				_audioSource.PlayOneShot(clip);
		}

		public void SetVolume(float volume) {
			if (_audioSource)
				_audioSource.volume = volume;
		}

		public float GetVolume() {
			return _audioSource ? _audioSource.volume : 0f;
		}
	}
}
