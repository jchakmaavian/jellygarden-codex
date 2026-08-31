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

#if UNITY_INAPPS
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace JellyGarden.Scripts
{
    public class UnityInAppsIntegration : MonoBehaviour
    {
        public static UnityInAppsIntegration THIS;
        public StoreController m_StoreController;
        private static string[] kProductIDConsumableArray;                                                       // General handle for the consumable product.


        void Start()
        {
            THIS = this;
            if (m_StoreController == null)
            {
                InitializePurchasing();
            }
        }

        public async void InitializePurchasing()
        {
            if (IsInitialized())
            {
                return;
            }

            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
            m_StoreController.OnProductsFetched += OnProductsFetched;
            m_StoreController.OnPurchasesFetched += OnPurchasesFetched;

            await m_StoreController.Connect();

            kProductIDConsumableArray = new string[LevelManager.THIS.InAppIDs.Length];
            var initialProductsToFetch = new List<ProductDefinition>();

            for (int i = 0; i < LevelManager.THIS.InAppIDs.Length; i++)
            {
                kProductIDConsumableArray[i] = LevelManager.THIS.InAppIDs[i];
                initialProductsToFetch.Add(new ProductDefinition(kProductIDConsumableArray[i], ProductType.Consumable));
            }

            m_StoreController.FetchProducts(initialProductsToFetch);
        }


        private bool IsInitialized()
        {
            return m_StoreController != null;
        }

        public void BuyProductID(string productId)
        {
            try
            {
                if (IsInitialized())
                {
                    Debug.Log(string.Format("Attempting to purchase product: '{0}'", productId));
                    m_StoreController.PurchaseProduct(productId);
                }
                else
                {
                    Debug.Log("BuyProductID FAIL. Not initialized.");
                }
            }
            catch (Exception e)
            {
                Debug.Log("BuyProductID: FAIL. Exception during purchase. " + e);
            }
        }
        void OnProductsFetched(List<Product> products)
        {
            Debug.Log("OnProductsFetched: Products fetched successfully");
            m_StoreController.FetchPurchases();
        }

        void OnPurchasesFetched(Orders orders)
        {
            Debug.Log("OnPurchasesFetched: Purchases fetched successfully");
        }

        void OnPurchasePending(PendingOrder pendingOrder)
        {
            Debug.Log("OnPurchasePending: Purchase pending - confirming purchase");
            InitScript.Instance.PurchaseSucceded();
            m_StoreController.ConfirmPurchase(pendingOrder);
        }

        void OnPurchaseFailed(FailedOrder failedOrder)
        {
            Debug.Log(string.Format("OnPurchaseFailed: Purchase failed. Error: {0}", failedOrder.FailureReason));
        }

    }
}
#endif

