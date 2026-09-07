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

namespace JellyGarden.Scripts.GUI
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance { get; private set; }

        [SerializeField] private Transform canvas;
        [SerializeField] private Transform canvasGlobal;
        [SerializeField] private GameObject completeLabel;
        [SerializeField] private GameObject preCompleteBanner;
        [SerializeField] private GameObject prePlay;
        [SerializeField] private GameObject menuPause;
        [SerializeField]
        public GameObject gemsShop;
        [SerializeField]
        public GameObject liveShop;
        [SerializeField]
        public GameObject settings;
        [SerializeField] private GameObject tutorial;
        [SerializeField] private GameObject boostInfo;
        [SerializeField] private GameObject menuFailed;
        [SerializeField]
        public GameObject menuPlay;
        [SerializeField] private GameObject noMoreMatches;
        [SerializeField] private GameObject rate;
        [SerializeField] private GameObject menuComplete;
        [SerializeField] private GameObject preFailed;
        [SerializeField] private GameObject reward;
        [SerializeField] private GameObject progressBar;
        [SerializeField] private GameObject[] stars;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            HideAllCanvasElements();
        }

        private void Close(GameObject obj)
        {
            if (obj != null)
                obj.GetComponent<AnimationManager>().CloseMenu();
        }

        public void ShowCompleteLabel(bool show = true)
        {
            if (!show)
            {
                Close(completeLabel);
                return;
            }
            if (completeLabel != null)
                completeLabel.SetActive(true);
        }

        public void ShowPreCompleteBanner(bool show = true)
        {
            if (!show)
            {
                Close(preCompleteBanner);
                return;
            }
            if (preCompleteBanner != null)
                preCompleteBanner.SetActive(true);
        }

        public void ShowPrePlay(bool show = true)
        {
            if (!show)
            {
                Close(prePlay);
                return;
            }
            if (prePlay != null)
                prePlay.SetActive(true);
        }

        public void ShowMenuPause(bool show = true)
        {
            if (!show)
            {
                Close(menuPause);
                return;
            }
            if (menuPause != null)
                menuPause.SetActive(true);
        }

        public void ShowGemsShop(bool show = true)
        {
            // --- King Leo story layer -------------------------------------
            // Nothing is for sale (TECH.md). Additive: delete this and the two
            // lines in ShowLiveShop below, and the shops open again.
            if (show && !JellyGarden.Scripts.Story.StoryFreeGame.ShopsAllowed)
                return;
            // --------------------------------------------------------------

            if (!show)
            {
                Close(gemsShop);
                return;
            }
            if (gemsShop != null)
            {
                gemsShop.SetActive(true);
            }
        }

        public void ShowLiveShop(bool show = true)
        {
            if (show && !JellyGarden.Scripts.Story.StoryFreeGame.ShopsAllowed)
                return;

            if (!show)
            {
                Close(liveShop);
                return;
            }
            if (liveShop != null)
                liveShop.SetActive(true);
        }

        public void ShowSettings(bool show = true)
        {
            if (!show)
            {
                Close(settings);
                return;
            }
            if (settings != null)
                settings.SetActive(true);
        }

        public void ShowTutorial(bool show = true)
        {
            if (!show)
            {
                Close(tutorial);
                return;
            }
            if (tutorial != null)
                tutorial.SetActive(true);
        }

        public void ShowBoostInfo(bool show = true)
        {
            if (!show)
            {
                Close(boostInfo);
                return;
            }
            if (boostInfo != null)
                boostInfo.SetActive(true);
        }

        public void ShowMenuFailed(bool show = true)
        {
            if (!show)
            {
                Close(menuFailed);
                return;
            }
            if (menuFailed != null)
                menuFailed.SetActive(true);
        }

        public void ShowMenuPlay(bool show = true)
        {
            if (!show)
            {
                Close(menuPlay);
                return;
            }
            if (menuPlay != null)
                menuPlay.SetActive(true);
        }

        public void ShowNoMoreMatches(bool show = true)
        {
            if (!show)
            {
                Close(noMoreMatches);
                return;
            }
            if (noMoreMatches != null)
                noMoreMatches.SetActive(true);
        }

        public void ShowRate(bool show = true)
        {
            // --- King Leo story layer -------------------------------------
            // The rate prompt is the kit's, and it is asked for from
            // GameState.Map — a screen this game never shows. Its panel lays a
            // black scrim at 87% over the whole screen and then has nothing on
            // it the player can press, so every level after the sixth is played
            // in the dark. Additive: delete these two lines and it is back.
            if (show && !JellyGarden.Scripts.Story.StoryFreeGame.RatePromptAllowed)
                return;
            // --------------------------------------------------------------

            if (!show)
            {
                Close(rate);
                return;
            }
            if (rate != null)
                rate.SetActive(true);
        }

        public void ShowMenuComplete(bool show = true)
        {
            if (!show)
            {
                Close(menuComplete);
                return;
            }
            if (menuComplete != null)
                menuComplete.SetActive(true);
        }

        public void ShowPreFailed(bool show = true)
        {
            // --- King Leo story layer -------------------------------------
            // The PreFailed banner is the kit's last sales pitch: five more
            // moves for twelve gems, or "Give Up". Nothing is for sale here, so
            // the offer is one button the player cannot take and one they must
            // press to see the result of a level they have already lost.
            //
            // It cannot simply be skipped: this banner's own animation event is
            // what sets GameOver, and GameOver is what raises OnLose and puts the
            // story's fail panel up. So the state moves on instead, out of the
            // gameStatus setter that called this — the same place and the same
            // one-frame delay the banner's animation event would have used.
            //
            // Additive: delete this block and the kit's banner is back.
            if (show && !JellyGarden.Scripts.Story.StoryFreeGame.PreFailOfferAllowed)
            {
                StartCoroutine(EndTheLostLevel());
                return;
            }
            // --------------------------------------------------------------

            if (!show)
            {
                preFailed.GetComponent<AnimationManager>().GoOnFailed();
                return;
            }
            if (preFailed != null)
                preFailed.SetActive(true);
        }

        /// <summary>
        /// Part of the block above: what the PreFailed banner's animation event
        /// does at the end of its slide, minus the banner.
        /// </summary>
        private global::System.Collections.IEnumerator EndTheLostLevel()
        {
            yield return null;
            if (LevelManager.THIS != null && LevelManager.THIS.Limit <= 0)
                LevelManager.THIS.gameStatus = GameState.GameOver;
        }

        public GameObject ShowReward(bool show = true)
        {
            if (!show)
            {
                Close(reward);
                return reward;
            }
            if (reward != null)
                reward.SetActive(true);
            return reward;
        }

        public void ShowProgressBar(bool show = true)
        {
            if (!show)
            {
                Close(progressBar);
                return;
            }
            if (progressBar != null)
                progressBar.SetActive(true);
        }

        public Transform GetCanvasGlobal()
        {
            return canvasGlobal;
        }

        public Transform GetCanvas()
        {
            return canvas;
        }

        public void HideAllCanvasElements()
        {
            if (canvasGlobal != null)
            {
                foreach (Transform item in canvasGlobal)
                {
                    item.gameObject.SetActive(false);
                }
            }
        }
    }
}