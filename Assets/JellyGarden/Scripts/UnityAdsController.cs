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
using JellyGarden.Scripts;
using UnityEngine;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

namespace JellyGarden.Ads
{
    public class UnityAdsController : MonoBehaviour
#if UNITY_ADS
        , IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
#endif
    {
#if UNITY_ADS
        #region UnityAdsController

        public static UnityAdsController Instance;

        private void Awake()
        {
            Instance = this;
        }
        
        public void InitAds(string androidID,string iOSID)
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
#if UNITY_ANDROID
            Advertisement.Initialize(androidID, false, this);
#elif UNITY_IOS
            Advertisement.Initialize(iOSID, false, this);
#endif
        }

        public void ShowAds(string loadAdType)
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
            Advertisement.Show(loadAdType, this);
        }

        public void LoadUnityRewardedAd()
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
            string videoAdsZone = (Application.platform == RuntimePlatform.IPhonePlayer)
                ? InitScript.Instance.unityAds.unityRewardedIOS
                : InitScript.Instance.unityAds.unityRewardedAndroid;
            Advertisement.Load(videoAdsZone, this);
        }

        public void LoadUnityInterstitialAd()
        {
            if (!RoyalThrone.RoyalOfflineMode.ServicesEnabled) return;
            string interstitialAdsZone =
                (Application.platform == RuntimePlatform.IPhonePlayer)
                    ? InitScript.Instance.unityAds.unityInterstitialIOS
                    : InitScript.Instance.unityAds.unityInterstitialAndroid;
            Advertisement.Load(interstitialAdsZone, this);
        }

        public void OnInitializationComplete()
        {
            Debug.Log("OnInitializationComplete!");
            LoadUnityRewardedAd();
            LoadUnityInterstitialAd();
        }

        [HideInInspector]
        public bool isLoaded;

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            isLoaded = false;
            Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
        }

        public void OnUnityAdsAdLoaded(string placementId)
        {
            isLoaded = true;
            Debug.Log("OnUnityAdsAdLoaded!  placementId = " + placementId);
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            isLoaded = false;
            Debug.Log($"Error showing Ad Unit {placementId}: {error.ToString()} - {message}");
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            isLoaded = false;
            Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
        }

        public void OnUnityAdsShowStart(string placementId)
        {
            Debug.Log("OnUnityAdsShowStart!  placementId = " + placementId);
        }

        public void OnUnityAdsShowClick(string placementId)
        {
            Debug.Log("OnUnityAdsShowClick!  placementId = " + placementId);
        }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            isLoaded = false;
            if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                Debug.Log("OnUnityAdsShowComplete!  placementId = " + placementId);
                InitScript.Instance.CheckRewardedAds();
            }
        }

        #endregion
#endif
    }
}
