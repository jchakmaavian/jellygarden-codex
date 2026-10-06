using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
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
using UnityEngine.UI;

namespace JellyGarden.Scripts.GUI
{
    /// <summary>
    /// Displays the time until the next life. The schedule itself lives in
    /// <see cref="InitScript.UpdateLives"/>, which owns granting, persistence and the deadline,
    /// so this counter never advances a timer of its own and several instances cannot race.
    /// </summary>
    public class LIFESAddCounter : MonoBehaviour
    {
        Text text;

        void Start()
        {
            text = GetComponent<Text>();
        }

        void Update()
        {
            InitScript init = InitScript.Instance;
            if (init == null) return;
            float remaining = init.UpdateLives();
            InitScript.DateOfRestLife = DateTime.Now;

            if (text == null || !gameObject.activeSelf) return;
            text.enabled = true;
            if (InitScript.lifes >= init.CapOfLife)
            {
                text.text = "Full";
                InitScript.timeForReps = text.text;
                return;
            }

            // Round up so a pending life never reads 00:00 before it is actually granted.
            int total = Mathf.Max(0, Mathf.CeilToInt(remaining));
            int hours = total / 3600;
            int minutes = (total - hours * 3600) / 60;
            int seconds = total - hours * 3600 - minutes * 60;
            text.text = init.TotalTimeForRestLifeHours > 0
                ? string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds)
                : string.Format("{0:00}:{1:00}", minutes + hours * 60, seconds);
            InitScript.timeForReps = text.text;
        }
    }
}
