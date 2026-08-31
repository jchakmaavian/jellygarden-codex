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

#if PLAYFAB
using UnityEngine;
using System.Collections;
using PlayFab.ClientModels;
using PlayFab;
using System.Collections.Generic;
using System;

namespace JellyGarden.Network.PlayFab
{
    public class PlayFabCurrencyManager : ICurrencyManager 
    {
        public PlayFabCurrencyManager () {
        }


        public  void IncBalance (int amount) {
            PlayFab.ClientModels.AddUserVirtualCurrencyRequest request = new PlayFab.ClientModels.AddUserVirtualCurrencyRequest () {
                VirtualCurrency = "GC",
                Amount = amount
            };

            PlayFabClientAPI.AddUserVirtualCurrency (request, (result) => {
                Debug.Log (result.Balance);
            },
                (error) => {
                    Debug.Log (error.ErrorMessage);
                });
        }

        public  void DecBalance (int amount) {
            PlayFab.ClientModels.SubtractUserVirtualCurrencyRequest request = new PlayFab.ClientModels.SubtractUserVirtualCurrencyRequest () {
                VirtualCurrency = "GC",
                Amount = amount
            };

            PlayFabClientAPI.SubtractUserVirtualCurrency (request, (result) => {
                Debug.Log (result.Balance);
            },
                (error) => {
                    Debug.Log (error.ErrorMessage);
                });
        }

        public  void SetBalance (int newbalance) {
            PlayFab.ClientModels.AddUserVirtualCurrencyRequest request = new PlayFab.ClientModels.AddUserVirtualCurrencyRequest () {
                VirtualCurrency = "GC",
                Amount = newbalance - NetworkCurrencyManager.currentBalance
            };

            PlayFabClientAPI.AddUserVirtualCurrency (request, (result) => {
                Debug.Log (result.Balance);
            },
                (error) => {
                    Debug.Log (error.ErrorMessage);
                });

        }

        public  void GetBalance (Action<int> Callback) {
            PlayFab.ClientModels.AddUserVirtualCurrencyRequest request = new PlayFab.ClientModels.AddUserVirtualCurrencyRequest () {
                VirtualCurrency = "GC"
            };

            PlayFabClientAPI.AddUserVirtualCurrency (request, (result) => {
                Callback (result.Balance);
            },
                (error) => {
                    Debug.Log (error.ErrorMessage);
                    //GetCurrencyList();
                });

        }
    }
}
#endif