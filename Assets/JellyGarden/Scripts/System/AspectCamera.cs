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
using System.Linq;
using UnityEngine;

// [ExecuteInEditMode]
//2.2.2
namespace JellyGarden.Scripts.System
{
    public class AspectCamera : MonoBehaviour
    {
        public Sprite map;
        public RectTransform topPanel;
        private Rect fieldRect;
        float fieldHeight;
        private float fielgWidth;
        private float fraq;

        private void OnEnable()
        {
            LevelManager.OnMapState += UpdateAspect;
            LevelManager.OnEnterGame += UpdateAspect;
        }

        private void OnDisable()
        {
            LevelManager.OnMapState -= UpdateAspect;
            LevelManager.OnEnterGame -= UpdateAspect;
        }

        void UpdateAspect()
        {
            StartCoroutine(Wait());

        }

        IEnumerator Wait()
        {
            yield return new WaitWhile(() => !LevelManager.THIS);
            if (LevelManager.THIS.gameStatus != GameState.Map)
            {
                yield return new WaitWhile(() => LevelManager.THIS.GetItems().Count == 0);
                var items = LevelManager.THIS.GetItems().Where(i => i != null).Where(i => i != null);
                float topY = items.Max(i => i.transform.position.y);
                float bottomY = items.Min(i => i.transform.position.y);
                float leftX = items.Min(i => i.transform.position.x);
                float rightX = items.Max(i => i.transform.position.x);

                fieldHeight = topY - bottomY;
                fielgWidth = rightX - leftX;
                fieldRect = new Rect(leftX, topY, fielgWidth, fieldHeight);
                fraq = (fielgWidth > fieldHeight ? fielgWidth : fieldHeight);
                int width = Screen.width;
                int height = Screen.height;
                float v = fraq / width * (height - 300);
                var h = fieldRect.width * Screen.height / Screen.width / 2 + 1.5f;
                var w = (fieldRect.height + 2.5f * 2) / 2 + 2f;
                var maxLength = Mathf.Max(h, w);
                Camera.main.orthographicSize = Mathf.Clamp(maxLength, 4, maxLength);
            }
            else
                Camera.main.orthographicSize = 8f / Screen.width * Screen.height / 2f;


        }

    }
}