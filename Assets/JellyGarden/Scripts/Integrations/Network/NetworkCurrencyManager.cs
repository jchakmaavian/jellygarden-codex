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

#if PLAYFAB || GAMESPARKS
using UnityEngine;
using System.Collections;

#if PLAYFAB
using PlayFab.ClientModels;
using PlayFab;
#endif
using System.Collections.Generic;
using System;

namespace JellyGarden.Network
{
    public class NetworkCurrencyManager : ICurrencyManager
    {
        public static int currentBalance;
        ICurrencyManager currencyMananager;

        public NetworkCurrencyManager()
        {
            NetworkManager.OnLoginEvent += GetBalance;
            NetworkManager.OnLogoutEvent += Logout;
#if PLAYFAB
            currencyMananager = new PlayFabCurrencyManager();
#elif GAMESPARKS
            currencyMananager = new GamesparksCurrencyManager();
#endif
        }

        void Logout()
        {
            NetworkManager.OnLoginEvent -= GetBalance;
            NetworkManager.OnLogoutEvent -= Logout;
        }

        public void IncBalance(int amount)
        {
            if (!NetworkManager.THIS.IsLoggedIn)
                return;

            if (currencyMananager != null)
                currencyMananager.IncBalance(amount);
        }

        public void DecBalance(int amount)
        {
            if (!NetworkManager.THIS.IsLoggedIn)
                return;

            if (currencyMananager != null)
                currencyMananager.DecBalance(amount);
        }

        public void SetBalance(int newbalance)
        {
            if (!NetworkManager.THIS.IsLoggedIn)
                return;

            //		GetBalance ();

            if (currencyMananager != null)
                currencyMananager.SetBalance(newbalance);
        }

        public void GetBalance()
        {
            if (!NetworkManager.THIS.IsLoggedIn)
                return;

            if (currencyMananager != null)
            {
                currencyMananager.GetBalance((balance) =>
                {
                    Debug.Log(balance);
                    currentBalance = balance;
                    if (currentBalance >= InitScript.Gems)
                        InitScript.Instance.SetGems(balance);
                    else
                        SetBalance(InitScript.Gems);

                });

            }
        }
    }
}
#endif