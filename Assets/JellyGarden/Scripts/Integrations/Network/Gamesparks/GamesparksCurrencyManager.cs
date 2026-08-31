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

#if GAMESPARKS
using GameSparks.Core;

namespace JellyGarden.Network.Gamesparks 
{
    public class GamesparksCurrencyManager : ICurrencyManager 
    {


        #region ICurrencyManager implementation

        public void IncBalance (int amount) {
            new GameSparks.Api.Requests.LogEventRequest ().SetEventKey ("AddCurrency")
                .SetEventAttribute ("Value", amount).Send ((response) => {
                if (!response.HasErrors) {
                    Debug.Log ("Currency Saved To GameSparks...");
                } else {
                    SetBalance (InitScript.Gems);//2.1.2
                }
            });
            
        }

        public void DecBalance (int amount) {
            new GameSparks.Api.Requests.LogEventRequest ().SetEventKey ("SpendCurrency")
                .SetEventAttribute ("Value", amount).Send ((response) => {
                if (!response.HasErrors) {
                    Debug.Log ("Currency Saved To GameSparks...");
                } else {
                    Debug.Log ("Error Saving Currency Data...");
                }
            });
            
        }

        public void SetBalance (int newbalance) {
            new GameSparks.Api.Requests.LogEventRequest ().SetEventKey ("SetCurrency")
                .SetEventAttribute ("Value", newbalance).Send ((response) => {
                if (!response.HasErrors) {
                    Debug.Log ("Currency Saved To GameSparks...");
                } else {
                    Debug.Log ("Error Saving Currency Data...");
                }
            });
            
        }

        public void GetBalance (System.Action<int> Callback) {
            new GameSparks.Api.Requests.LogEventRequest ().SetEventKey ("GetCurrency")
                .Send ((response) => {
                if (!response.HasErrors) {
                    GSData data = response.ScriptData.GetGSData ("currency_Data");
                    if (data != null) {
                        Callback (int.Parse (data.GetInt ("Value").ToString ()));
                        Debug.Log ("Currency Received From GameSparks...");
                    }
                } else {
                    Debug.Log ("Error Saving Currency Data...");
                }
            });
            
        }



        #endregion



    }
}
#endif