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
using System.Collections;

namespace JellyGarden.Scripts
{
    public class ItemAnimEvents : MonoBehaviour {


        public Item item;
        private SpriteRenderer _spriteRenderer;
        private Animator _animator;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
        }

        public void SetAnimationDestroyingFinished()
        {
            item.SetAnimationDestroyingFinished();
        }

        public bool IsActive()
        {
            return _spriteRenderer.enabled;
        }

        public void SetActive(bool b)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = b;
                if (b)
                {
                    StartCoroutine(HideDelayed());
                }
            }
            else
            {
                Debug.LogError("SpriteRenderer is null");
            }
        }

        private IEnumerator HideDelayed()
        {
            yield return new WaitForSeconds(1f);
            _animator.SetTrigger("stop");
            _animator.SetInteger("color", 10);
            _spriteRenderer.enabled = false;
        }
    }
}
