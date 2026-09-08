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
using JellyGarden.Scripts.GUI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JellyGarden.Scripts.System
{
    public class RestartLevel : MonoBehaviour
    {

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(WaitForLoad(scene));
        }

        IEnumerator WaitForLoad(Scene scene)
        {
            yield return new WaitUntil(()=>LevelManager.THIS != null);
            if(scene.name == "game")
            {
                Debug.Log("restart");

                StartGame();
                Destroy(gameObject);
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            Debug.Log("OnDisable");
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void StartGame()
        {
            if (InitScript.lifes > 0)
            {
                InitScript.Instance.SpendLife(1);
                LevelManager.THIS.gameStatus = GameState.PrepareGame;
            }
            else
            {
                BuyLifeShop();
            }

        }

        public void BuyLifeShop()
        {

            SoundBase.Instance.PlaySound(SoundBase.Instance.click);
            if (InitScript.lifes < InitScript.Instance.CapOfLife)
                MenuManager.Instance.ShowLiveShop(true);

        }
    }
}
