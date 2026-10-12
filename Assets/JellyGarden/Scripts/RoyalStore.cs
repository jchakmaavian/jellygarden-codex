using PlayerPrefs = RoyalThrone.Testing.RoyalSave;
using System;
using System.Collections.Generic;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.Purchasing;

namespace RoyalThrone
{
    // Real-money purchases through Google Play (2026-10-05, the user: real money buys stars and coins through Google Play).
    // Unity IAP 5 (com.unity.purchasing; BillingMode.json chooses Google Play) talks to Google Play Billing on Android and to
    // its own fake store in the editor, which approves every purchase. The products are RoyalEconomy.StoreItems, each a
    // consumable that the Play Console must list under the same id and price.
    // A purchase pays out when the store reports it (OnPurchasePending: coins to InitScript, stars to RoyalAreas) and is
    // only then confirmed, so an order the app did not finish (closed mid-payment) is delivered again at the next start.
    // Each order's transaction id is recorded once paid, so a delivered-again order is confirmed without paying twice.
    public static class RoyalStore
    {
        public enum State { Off, Connecting, Ready, Unavailable }
        public static State Status { get; private set; }
        /// <summary>The store's state or a purchase's outcome changed (the shop redraws).</summary>
        public static event Action Changed;
        /// <summary>The last outcome to show the player ("+500 Coins", "Purchase cancelled", ...).</summary>
        public static string Message { get; private set; } = "";
        /// <summary>When Message was last set (unscaled time), so a reopened shop shows only what happened since.</summary>
        public static float MessageAt { get; private set; } = -1;
        /// <summary>A purchase is waiting for Google Play's answer.</summary>
        public static bool Busy => waiting != null;

        static StoreController store;
        // Whether the last product fetch returned any product. Unity IAP reports a partial fetch twice: OnProductsFetched
        // with what it found, then OnProductsFetchFailed for the rest. One id missing on Google Play must not shut the shop.
        static bool fetchedAny;
        static Action<bool, string> waiting;
        static string waitingFor;   // the product the waiting purchase is for: another order (a slow one) finishing does not end it
        // In the editor a test purchase belongs to the save that started it (the QA profile or the real one): the test store's
        // dialog can be answered after a QA session has ended, which paid test coins into the real save (2026-10-05).
        static bool startedInQa;
        const string PaidKey = "RoyalThrone.Store.Paid";
        const int PaidKept = 64;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { store = null; Status = State.Off; Changed = null; waiting = null; waitingFor = null; Message = ""; MessageAt = -1; fetchedAny = false; }

        // Orders the app did not finish (closed mid-payment, or a slow payment completed since) are delivered as soon as the
        // game scene is up, not only once the shop opens: Google refunds what is not acknowledged within three days.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void ConnectWithTheGame()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= ConnectOnGameScene;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += ConnectOnGameScene;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "game") Connect();
        }
        static void ConnectOnGameScene(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) { if (scene.name == "game") Connect(); }

        /// <summary>Connects once (again after a failure). Safe to call every time the shop opens.</summary>
        public static void Connect()
        {
            if (Status == State.Connecting || Status == State.Ready) return;
            ConnectAsync();
        }

        static async void ConnectAsync()
        {
            Set(State.Connecting, null);
            try
            {
                if (store == null)
                {
                    store = UnityIAPServices.StoreController();
                    store.OnProductsFetched += products => { fetchedAny |= products != null && products.Count > 0; Set(State.Ready, null); store.FetchPurchases(); };
                    store.OnProductsFetchFailed += failure =>
                    {
                        // Only when nothing at all came back is the store unavailable; a missing pack is refused by Buy alone.
                        var missing = failure != null && failure.FailedFetchProducts != null ? string.Join(", ", failure.FailedFetchProducts.ConvertAll(d => d.id)) : "?";
                        Debug.LogWarning("[Royal store] Products not found on the store: " + missing + " (" + (failure != null ? failure.FailureReason : "") + ")");
                        if (!fetchedAny) Set(State.Unavailable, null);
                    };
                    store.OnPurchasePending += Deliver;
                    store.OnPurchaseFailed += Failed;
                    store.OnPurchaseDeferred += order => Finish(false, "Payment pending: it is added once Google Play completes it", order);
                    store.OnStoreDisconnected += failure => { Set(State.Unavailable, null); Finish(false, "Store unavailable"); };
                    // Listened to so the store does not warn about missing callbacks; the pending orders do the work.
                    store.OnPurchaseConfirmed += order => { };
                    store.OnPurchasesFetched += orders => { };
                    store.OnPurchasesFetchFailed += failure => { };
                }
                await store.Connect();
                var products = new List<ProductDefinition>();
                foreach (var item in RoyalEconomy.StoreItems) products.Add(new ProductDefinition(item.Id, ProductType.Consumable));
                fetchedAny = false;
                store.FetchProducts(products);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Royal store] Could not connect: " + e.Message);
                Set(State.Unavailable, null);
            }
        }

        /// <summary>The price to show: Google Play's own (in the player's currency) on a device, the US price otherwise.</summary>
        public static string Price(RoyalEconomy.StoreItem item)
        {
            if (Application.isEditor || Status != State.Ready || store == null) return item.PriceUsd;
            var product = store.GetProductById(item.Id);
            var price = product != null ? product.metadata.localizedPriceString : null;
            return string.IsNullOrEmpty(price) ? item.PriceUsd : price;
        }

        /// <summary>Starts a purchase; `done` hears the outcome (paid, message).</summary>
        public static bool Buy(RoyalEconomy.StoreItem item, Action<bool, string> done)
        {
            string refusal = Status != State.Ready || store == null ? "Store unavailable. Check your connection and try again"
                : waiting != null ? "A purchase is already in progress"
                // On a device, money is never spent from the QA test profile: its coins are put back when the session ends.
                : !Application.isEditor && RoyalThrone.Testing.RoyalSave.Testing ? "Leave the test profile to buy with real money" : null;
            var product = refusal == null ? store.GetProductById(item.Id) : null;
            if (refusal == null && (product == null || !product.availableToPurchase)) refusal = "This pack is not available yet";
            if (refusal != null) { Say(refusal); done?.Invoke(false, refusal); Changed?.Invoke(); return false; }
            waiting = done ?? ((paid, message) => { }); waitingFor = item.Id;
            startedInQa = RoyalThrone.Testing.RoyalSave.Testing;
            Say("Waiting for Google Play...");
            Changed?.Invoke();
            store.PurchaseProduct(product);
            return true;
        }

        static void Deliver(PendingOrder order)
        {
            string transaction = order.Info != null ? order.Info.TransactionID : null;
            if (Application.isEditor && startedInQa != RoyalThrone.Testing.RoyalSave.Testing)
            {
                Debug.LogWarning("[Royal store] A test purchase finished in a different save than the one that started it; not paid.");
                store.ConfirmPurchase(order);
                Finish(false, "Purchase cancelled");
                return;
            }
            // A real order delivered while the QA test profile is active (a development build) waits, unconfirmed, until the
            // player's own save is back: Google Play hands it over again at the next connection.
            if (!Application.isEditor && RoyalThrone.Testing.RoyalSave.Testing) { Debug.LogWarning("[Royal store] An order arrived during a QA session; it is kept for the player's save."); return; }
            var given = new List<string>();
            foreach (var line in order.CartOrdered.Items())
            {
                var item = line.Product != null ? RoyalEconomy.FindStoreItem(line.Product.definition.id) : null;
                if (item == null) continue;
                string receipt = (transaction ?? "") + "/" + item.Id;
                if (!string.IsNullOrEmpty(transaction) && WasPaid(receipt)) continue;
                int quantity = Mathf.Max(1, line.Quantity);
                Grant(item, quantity);
                if (!string.IsNullOrEmpty(transaction)) RecordPaid(receipt);
                given.Add("+" + (item.Coins > 0 ? (item.Coins * quantity).ToString("N0") + " Coins" : (item.Stars * quantity) + " Stars"));
            }
            store.ConfirmPurchase(order);
            Finish(true, given.Count > 0 ? string.Join("  ", given) : "Purchase complete", order);
        }

        // Whether an order is the one the shop is waiting for (no product to tell: assume it is).
        static bool IsWaitedFor(Order order)
        {
            if (waitingFor == null || order == null || order.CartOrdered == null) return true;
            foreach (var line in order.CartOrdered.Items()) if (line.Product != null && line.Product.definition.id == waitingFor) return true;
            return false;
        }

        static void Failed(FailedOrder order)
        {
            bool cancelled = order.FailureReason == PurchaseFailureReason.UserCancelled;
            Finish(false, cancelled ? "Purchase cancelled" : "Purchase failed. You were not charged", order);
        }

        static void Finish(bool paid, string message, Order order = null)
        {
            Say(message);
            if (order != null && !IsWaitedFor(order)) { Changed?.Invoke(); return; }   // another order: the waited one goes on
            var done = waiting; waiting = null; waitingFor = null;
            done?.Invoke(paid, message);
            Changed?.Invoke();
        }

        static void Set(State state, string message) { Status = state; Changed?.Invoke(); }
        static void Say(string message) { Message = message; MessageAt = Time.unscaledTime; }

        /// <summary>Pays out a product: coins to the purse, stars to the area stars.</summary>
        public static void Grant(RoyalEconomy.StoreItem item, int quantity = 1)
        {
            int coins = item.Coins * quantity, stars = item.Stars * quantity;
            if (coins > 0)
            {
                if (InitScript.Instance) InitScript.Instance.AddGems(coins);
                else { InitScript.Gems += coins; PlayerPrefs.SetInt("Gems", InitScript.Gems); PlayerPrefs.Save(); }
            }
            if (stars > 0) Presentation.RoyalAreas.AddBoughtStars(stars);
            RefreshHome();
        }

        /// <summary>Buys stars with coins at RoyalEconomy.StarPriceCoins each; false when the purse is short.</summary>
        public static bool BuyStarsWithCoins(int stars)
        {
            int cost = RoyalEconomy.StarsCost(stars);
            if (stars <= 0 || !InitScript.Instance || InitScript.Gems < cost) return false;
            InitScript.Instance.SpendGems(cost);
            Presentation.RoyalAreas.AddBoughtStars(stars);
            RefreshHome();
            Say("+" + stars + (stars == 1 ? " Star" : " Stars") + " for " + cost.ToString("N0") + " coins");
            Changed?.Invoke();
            return true;
        }

        static void RefreshHome()
        {
            var home = UnityEngine.Object.FindAnyObjectByType<RoyalThrone.Presentation.RoyalHomeScreen>();
            if (home) home.RefreshProgress();
        }

        static bool WasPaid(string receipt) => Array.IndexOf(PlayerPrefs.GetString(PaidKey, "").Split('|'), receipt) >= 0;

        static void RecordPaid(string receipt)
        {
            var list = new List<string>(PlayerPrefs.GetString(PaidKey, "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)) { receipt };
            if (list.Count > PaidKept) list.RemoveRange(0, list.Count - PaidKept);
            PlayerPrefs.SetString(PaidKey, string.Join("|", list)); PlayerPrefs.Save();
        }
    }
}
