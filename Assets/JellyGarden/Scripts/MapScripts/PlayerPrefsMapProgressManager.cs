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

using UnityEngine;

namespace JellyGarden.Scripts.MapScripts
{
    public class PlayerPrefsMapProgressManager : IMapProgressManager
    {
        private string GetLevelKey(int number)
        {
            return string.Format("Level.{0:000}.StarsCount", number);
        }

        public int LoadLevelStarsCount(int level)
        {
            return PlayerPrefs.GetInt(GetLevelKey(level), 0);
        }

        public void SaveLevelStarsCount(int level, int starsCount)
        {
            PlayerPrefs.SetInt(GetLevelKey(level), starsCount);
        }

        public void ClearLevelProgress(int level)
        {
            PlayerPrefs.DeleteKey(GetLevelKey(level));
        }
    }
}
