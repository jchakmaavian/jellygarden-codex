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
using UnityEngine.UI;

namespace JellyGarden.Scripts.GUI
{
	public class Background : MonoBehaviour
	{
		public Sprite[] pictures;
		[Header("How often to change background per levels")]
		public int changeBackgoundEveryLevels = 20 ;
		// Use this for initialization
		void OnEnable ()
		{
			if (LevelManager.THIS != null && pictures != null && pictures.Length > 0)
			{
				int index = (int)((float)LevelManager.Instance.currentLevel / (float)changeBackgoundEveryLevels - 0.01f);
				index = Mathf.Clamp(index, 0, pictures.Length - 1);
				GetComponent<Image>().sprite = pictures[index];
			}
		}
	}
}
