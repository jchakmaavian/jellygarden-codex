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

using System;
using UnityEngine;

namespace JellyGarden.Scripts.GUI
{
    public class KeepUIInScreen : MonoBehaviour
    {
        private void OnEnable()
        {
            RectTransform wordRT = GetComponent<RectTransform>();
 
            Vector2 wordScreenPosition = Camera.main.WorldToScreenPoint(wordRT.position);
            float wordWidth = wordRT.rect.width;
            float wordLeftPos = wordScreenPosition.x - (wordWidth / 2f);
            float wordRightPos = wordScreenPosition.x + (wordWidth / 2f);
 
            if (wordLeftPos < 0)
            {
                wordScreenPosition.x = wordScreenPosition.x + ((wordWidth / 2f) - wordScreenPosition.x);
 
                Vector2 wordWorldPos = Camera.main.ScreenToWorldPoint(wordScreenPosition);
                wordRT.position = wordWorldPos;
            }
            else if (wordRightPos > Camera.main.pixelWidth)
            {
                float deltaPosX = Camera.main.pixelWidth - wordScreenPosition.x;
                float diff = (wordWidth / 3f) - deltaPosX;
                wordScreenPosition.x = wordScreenPosition.x - diff;
               
                Vector2 wordWorldPos = Camera.main.ScreenToWorldPoint(wordScreenPosition);
                wordRT.position = wordWorldPos;
            }
        }
    }
}