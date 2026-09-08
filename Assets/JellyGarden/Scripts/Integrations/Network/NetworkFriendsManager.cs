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
using System;
using System.Collections.Generic;

#if PLAYFAB
using PlayFab.ClientModels;
using PlayFab;
#endif

namespace JellyGarden.Network
{
    public class NetworkFriendsManager : IFriendsManager
    {
        IFriendsManager friendsManager;

        public NetworkFriendsManager()
        {
#if PLAYFAB
            friendsManager = new PlayFabFriendsManager();
#elif GAMESPARKS
            friendsManager = new GameSparksFriendsManager();

#endif
            NetworkManager.OnLoginEvent += GetFriends;
            LevelManager.OnMapState += PlaceFriendsPositionsOnMap;
            LevelManager.OnMenuPlay += GetLeadboardOnLevel;
            LevelManager.OnMenuComplete += GetLeadboardOnLevel;
            NetworkManager.OnLogoutEvent += Logout;//1.3.3
        }

        public void Logout()
        {//1.3.3
            NetworkManager.OnLoginEvent -= GetFriends;
            LevelManager.OnMapState -= PlaceFriendsPositionsOnMap;
            LevelManager.OnMenuPlay -= GetLeadboardOnLevel;
            LevelManager.OnMenuComplete -= GetLeadboardOnLevel;

            NetworkManager.OnLogoutEvent -= Logout;
            FacebookManager.Friends.Clear();
            friendsManager.Logout();
        }


        /// <summary>
        /// Gets the friends list.
        /// </summary>
        public void GetFriends()
        {
            if (!NetworkManager.THIS.IsLoggedIn)
                return;

            if (friendsManager != null)
            {
                friendsManager.GetFriends((dic) =>
                {


                    foreach (var item in dic)
                    {
                        FriendData friend = new FriendData()
                        {
                            FacebookID = item.Key,
                            userID = item.Value
                        };
                        // Debug.Log (friend.userID);
                        FacebookManager.THIS.AddFriend(friend);//2.1.2
                                                               //					Debug.Log ("    " + item.Key + " == " + item.Value);
                    }
                    FacebookManager.THIS.GetFriendsPicture();
                    PlaceFriendsPositionsOnMap();

                });
                FacebookManager.THIS.AddFriend(FacebookManager.THIS.GetCurrentUserAsFriend());//2.2.2

            }
        }

        /// <summary>
        /// Place the friends on map.
        /// </summary>
        public void PlaceFriendsPositionsOnMap()
        {
            if (!NetworkManager.THIS.IsLoggedIn)
                return;

            if (friendsManager != null)
            {
                friendsManager.PlaceFriendsPositionsOnMap((list) =>
                {
                    foreach (var item in list)
                    {
                        FriendData friend = FacebookManager.Friends.Find(delegate (FriendData bk)
                        {
                            return bk.userID == item.Key && bk.userID != NetworkManager.UserID;
                        });
                        if (friend != null)
                        {
                            friend.level = item.Value;
                        }
                    }
                    NetworkManager.FriendsOnMapLoaded();

                });
            }
        }

        /// <summary>
        /// Gets the leadboard on level.
        /// </summary>
        public void GetLeadboardOnLevel()
        {
            LevelManager.THIS.StartCoroutine(GetLeadboardCor());
        }

        IEnumerator GetLeadboardCor()
        {
            yield return new WaitUntil(() => NetworkManager.THIS.IsLoggedIn == true);
            Debug.Log("getting leadboard");

            if (friendsManager != null)
            {
                int LevelNumber = PlayerPrefs.GetInt("OpenLevel");
                NetworkManager.leadboardList.Clear();
                yield return new WaitForSeconds(1);
                friendsManager.GetLeadboardOnLevel(LevelNumber, (list) =>
                {
                    foreach (var pl in list)
                    {
                        FriendData friend = FacebookManager.Friends.Find(delegate (FriendData bk)
                        {
                            return bk.userID == pl.userID;
                        }
                                            );
                        if (friend != null)
                        {
                            pl.friendData = friend;
                            pl.picture = friend.picture;
                        }

                        LeadboardPlayerData leadboardPlayerData = NetworkManager.leadboardList.Find(delegate (LeadboardPlayerData bk)
                        {
                            return bk.userID == pl.userID;
                        }
                                                                  );
                        if (leadboardPlayerData != null)
                            leadboardPlayerData = pl;
                        else
                            NetworkManager.leadboardList.Add(pl);
                        //					Debug.Log (pl.Name + " " + pl.userID + " " + pl.position + " " + pl.score);
                    }

                    if (NetworkManager.leadboardList.Count > 0)
                    {
                        NetworkManager.LevelLeadboardLoaded();
                    }

                });
            }

        }
    }
}
#endif