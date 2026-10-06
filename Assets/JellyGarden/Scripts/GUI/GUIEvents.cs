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
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.GUI
{
    public class GUIEvents : MonoBehaviour
    {
        void Update()
        {
            if (name == "FaceBook" || name == "Share" || name == "FaceBookLogout")
            {
                if (!LevelManager.THIS.FacebookEnable)
                    gameObject.SetActive(false);
            }
        }

        public void Settings()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.click);
            MenuManager.Instance.ShowSettings(true);
        }

        public void Play()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.click);
            transform.Find("Loading").gameObject.SetActive(true);
            SceneManager.LoadScene("game");
        }

        public void Pause()
        {
            SoundBase.Instance.PlaySound(SoundBase.Instance.click);
            if (LevelManager.THIS.gameStatus == GameState.Playing)
                MenuManager.Instance.ShowMenuPause(true);
        }

        public void FaceBookLogin()
        {
            #if FACEBOOK
        FacebookManager.THIS.CallFBLogin();
            #endif
        }

        public void FaceBookLogout()
        {
            #if FACEBOOK
        FacebookManager.THIS.CallFBLogout();
            #endif
        }

        public void Share()
        {
            #if FACEBOOK
        FacebookManager.THIS.Share();
            #endif
        }
    }
}
