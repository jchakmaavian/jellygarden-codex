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
using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Targets
{
    public class TargetsGroup : MonoBehaviour
    {
        public TargetIcon prefab;
        private int levelNum;
        private List<TargetIcon> TargetIcons = new List<TargetIcon>();
        private void OnEnable()
        {
            levelNum = PlayerPrefs.GetInt("OpenLevel");
            if(RoyalThrone.Campaign.RoyalReferenceCampaign.Active)
            {
                foreach(var target in LevelManager.THIS.targetObject)
                {
                    var obj=Instantiate<TargetIcon>(prefab,transform);obj.SetTarget(target);TargetIcons.Add(obj);target.guiObj=obj;
                }
                return;
            }
            var targetObj = Resources.Load<TargetLevel>("Targets/Level" + levelNum);
            foreach (var target in targetObj.targets)
            {
                var obj = Instantiate<TargetIcon>(prefab, transform);
                obj.SetTarget(target);
                TargetIcons.Add(obj);
                target.guiObj = obj;
            }
        }

        private void OnDisable()
        {
            foreach (var child in TargetIcons)
            {
                Destroy(child.gameObject);
            }
            TargetIcons.Clear();
        }
    }
}
