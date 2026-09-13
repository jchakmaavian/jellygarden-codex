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
    [RequireComponent(typeof(AudioSource))]
    public class MusicBase : MonoBehaviour {
        public static MusicBase Instance;
        public AudioClip[] music;
        private AudioSource _audioSource;

        void Awake() {
            if (transform.parent == null) {
                transform.parent = Camera.main.transform;
                transform.localPosition = Vector3.zero;
            }

            DontDestroyOnLoad(gameObject);
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);

            _audioSource = GetComponent<AudioSource>();
        }

        public void PlayMusic(int index, bool loop = true) {
            if (Juice.RoyalAudioDirector.Get) { if (_audioSource) _audioSource.Stop(); return; }
            if (_audioSource && index < music.Length) {
                _audioSource.clip = music[index];
                _audioSource.loop = loop;
                _audioSource.Play();
            }
        }

        public void Stop() {
            if (_audioSource)
                _audioSource.Stop();
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
