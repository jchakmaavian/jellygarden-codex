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
using System.Collections.Generic;

#if GAMESPARKS
using GameSparks.Api.Requests;
using GameSparks.Core;

namespace JellyGarden.Network.Gamesparks 
{
    public class GamesparksLogin : ILoginManager 
    {
        #region ILoginManager implementation

        string userID;

        public GamesparksLogin () {

        }

        public void LoginWithFB (string accessToken, string titleId) {
            new FacebookConnectRequest ().SetSwitchIfPossible (true).SetAccessToken (accessToken).Send ((response) => {
                if (!response.HasErrors) {
                    Debug.Log ("Player id : " + response.UserId);
                    userID = response.UserId;
                    NetworkManager.UserID = userID;
                    NetworkManager.THIS.IsLoggedIn = true;
                } else {
                    IDictionary<string, object> errors = response.Errors.BaseData;
                    Debug.Log ("Authentification error:");
                    foreach (var item in errors) {
                        Debug.Log (item.Key + " : " + item.Value);
                    }
                }
            });

        }

        public void UpdateName (string userName) {
    //        new AccountDetailsRequest ()
    //            .Send ((response) => {
    //            if (!response.HasErrors) {
    //
    //                Debug.Log ("Player id :" + response.UserId);
    //                Debug.Log ("Player name :" + response.DisplayName);
    //            } else {
    //                Debug.Log ("errors " + response.Errors);
    //            }
    //
    //        });
        }

        public bool IsYou (string id) {
            if (id == userID)
                return true;
            return false;
        }

        #endregion
    }
}
#endif
