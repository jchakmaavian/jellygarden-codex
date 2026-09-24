#if UNITY_EDITOR && !ROYAL_DISABLE_QA
using System;
using System.Collections;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.GUI;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Testing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RoyalThrone.ReferenceGameplay.Editor
{
    public static class ResponsiveInputChecks
    {
        const string Report = "Design/ReferenceGameplayImport/Validation/Responsive-Input.txt";
        static LevelManager LM => LevelManager.THIS;
        static void Check(bool ok, string text)
        {
            File.AppendAllText(Report, (ok ? "PASS " : "FAIL ") + text + "\n");
            if (!ok) throw new InvalidOperationException(text);
        }
        static Item Piece(int x, int y) => LM.GetSquare(x,y).item;
        static Item Power(int x, int y, ItemsTypes type)
        {
            var item = Piece(x,y);
            item.currentType = type; item.NextType = ItemsTypes.NONE;
            item.SetColor(type == ItemsTypes.BOMB ? 555 : (x + y*2)%5);
            item.GetComponent<RoyalPieceSkin>()?.ApplyNow();
            return item;
        }
        static void Swap(Item a, Item b) => a.SwitchDirection(a.transform.position-b.transform.position);
        static void Ownership()
        {
            var pieces = LM.GetItems().Where(i=>i).ToArray();
            Check(LM.InputSwaps == 0 && pieces.All(i=>i.square && i.square.item == i && !i.inputSwapLocked && !i.inputMatchPending && i.inputConversionLocks==0),
                "Settled pieces have unique cell ownership and no input leases");
            Check(pieces.Select(i=>i.square).Distinct().Count()==pieces.Length, "No duplicate cell occupants");
            Check(!RoyalPowerupDirector.Busy && !LM.IsResolvingMatches && !LM.DragBlocked, "Effects and resolver finish without a stuck board lock");
        }
        static IEnumerator Pointer(Mouse device, Vector3 world, bool pressed)
        {
            Vector2 screen=Camera.main.WorldToScreenPoint(world);
            InputSystem.QueueStateEvent(device, new MouseState {position=screen, buttons=(ushort)(pressed?1:0)});
            float deadline=Time.realtimeSinceStartup+2;
            // Input System can process in FixedUpdate; two render frames are not an event acknowledgement.
            do {yield return null;} while(Time.realtimeSinceStartup<deadline &&
                (Vector2.Distance(InputManager.Instance.TouchPosition,screen)>.1f || InputManager.Instance.IsTouching!=pressed));
            Check(Vector2.Distance(InputManager.Instance.TouchPosition,screen)<.1f && InputManager.Instance.IsTouching==pressed,
                "Input System receives the queued pointer event");
            yield return null;
        }
        public static IEnumerator InputEvents()
        {
            File.AppendAllText(Report,"RUNNING POINTER EVENT AND CANCELLATION CHECKS\n");
            var qa=RoyalQaService.Instance; Mouse device=null;
            var actions=InputManager.Instance.inputActionAsset;var previousDevices=actions.devices;
            try
            {
                qa.StartSession(); qa.Speed=1;
                yield return qa.Practice();
                device=InputSystem.AddDevice<Mouse>("Royal isolated input test mouse");
                actions.devices=new InputDevice[]{device};
                qa.Visible=false;
                var rocket=Power(0,0,ItemsTypes.HORIZONTAL_STRIPPED);
                var tnt=Power(7,7,ItemsTypes.PACKAGE);
                var rocketPosition=rocket.transform.position;var tntPosition=tnt.transform.position;
                int moves=LM.Limit;
                yield return Pointer(device,rocketPosition,true);
                Check(rocket.dragThis,"Input System press selects the Rocket through the actual hit-test path");
                yield return Pointer(device,rocketPosition,false);
                Check((!rocket||rocket.destroying)&&LM.Limit==moves-1,"Input System release activates Rocket exactly once");
                yield return Pointer(device,tntPosition,true);
                yield return Pointer(device,tntPosition,false);
                Check((!tnt||tnt.destroying)&&LM.Limit==moves-2,"A second real input press/release activates TNT during Rocket effects");
                yield return qa.Settled(); Ownership();
                yield return qa.Practice(); qa.Visible=false;
                var a=Piece(3,2);var b=Piece(3,3);var destination=b.square;
                tnt=Power(7,7,ItemsTypes.PACKAGE);RoyalPowerupDirector.Tap(tnt);moves=LM.Limit;
                yield return Pointer(device,a.transform.position,true);
                File.AppendAllText(Report,$"TRACE press: drag={a.dragThis}, allowed={a.CanAcceptInput}, input={InputManager.Instance.IsTouching}, cell={a.square.col},{a.square.row}, pointer={InputManager.Instance.TouchPosition}, moves={LM.Limit}, busy={RoyalPowerupDirector.Busy}\n");
                yield return Pointer(device,b.transform.position,true);
                Check(LM.Limit==moves-1,$"Input System swipe starts a valid normal match while TNT is active (before={moves}, after={LM.Limit}, drag={a&&a.dragThis}, swapping={LM.InputSwaps}, direction={(a?a.switchDirection:Vector3.zero)}, delta={(a?a.deltaPos:Vector3.zero)}, pointer={InputManager.Instance.TouchPosition})");
                yield return Pointer(device,destination.transform.position,false);
                yield return qa.Settled(); Ownership();
                Check(!a||a.destroying,"Normal swipe match resolves and refills during overlapping effects");
                qa.Visible=true;

                yield return qa.Practice();
                RoyalPowerupDirector.Tap(Power(0,0,ItemsTypes.BOMB));
                yield return null;
                Check(LM.GetItems().Any(i=>i&&i.inputConversionLocks>0),"Cancellation fixture has active Color Ball target leases");
                RoyalPowerupDirector.Get.Cancel();
                yield return null;
                Check(LM.GetItems().All(i=>!i||i.inputConversionLocks==0),"Cancelling effects releases conversion target input leases");
                yield return qa.Settled(); Ownership();
                yield return qa.EndSession();
                File.AppendAllText(Report,"ALL POINTER EVENT AND CANCELLATION CHECKS PASSED\n");
            }
            finally
            {
                if(device!=null&&device.added)InputSystem.RemoveDevice(device);
                actions.devices=previousDevices;
                if(RoyalSave.Testing&&qa)qa.StartCoroutine(qa.EndSession());
            }
        }
        public static IEnumerator Run()
        {
            var qa = RoyalQaService.Instance;
            int money = UnityEngine.PlayerPrefs.GetInt("Gems"), level = UnityEngine.PlayerPrefs.GetInt("OpenLevel");
            File.WriteAllText(Report, "RUNNING responsive board input\n");
            try
            {
                qa.StartSession(); qa.Speed=1;
                yield return qa.Practice();
                var rocket1=Power(0,0,ItemsTypes.HORIZONTAL_STRIPPED);
                var rocket2=Power(0,4,ItemsTypes.HORIZONTAL_STRIPPED);
                var tnt=Power(7,7,ItemsTypes.PACKAGE);
                int moves=LM.Limit, serial=LM.moveID;
                Check(RoyalPowerupDirector.Tap(rocket1), "First Rocket accepts tap");
                Check(RoyalPowerupDirector.Busy && LM.BoardInputAllowed, "Board input remains open during Rocket travel");
                Check(RoyalPowerupDirector.Tap(rocket2) && RoyalPowerupDirector.Tap(tnt), "Second Rocket and TNT accept taps in the same frame");
                Check(!RoyalPowerupDirector.Tap(rocket1), "Repeated tap cannot fire or charge the same booster twice");
                Check(LM.Limit==moves-3 && LM.moveID==serial+3, "Three activations spend exactly three moves");
                yield return qa.Settled(); Ownership();

                yield return qa.Practice();
                tnt=Power(7,7,ItemsTypes.PACKAGE);
                var a=Piece(3,2); var b=Piece(3,3);
                moves=LM.Limit;
                RoyalPowerupDirector.Tap(tnt);
                Swap(a,b);
                Check(a.inputSwapLocked && b.inputSwapLocked && LM.InputSwaps==1, "Normal matching swipe starts during TNT anticipation");
                Check(a.square.item==a && b.square.item==b, "Swap animation preserves committed cell ownership until arrival");
                Check(LM.Limit==moves-2, "Tap and valid swipe each cost one move");
                a.DestroyItem(true); a.DestroyItem(true);
                Check(!a.destroying, "An overlapping hit waits for the moving pair, not the whole board");
                yield return qa.Settled(); Ownership();
                Check(!a || a.destroying, "Deferred hit completes after the swap without duplicate ownership");

                yield return qa.Practice();
                a=Power(0,0,ItemsTypes.VERTICAL_STRIPPED); b=Piece(1,0);
                var c=Power(6,7,ItemsTypes.PACKAGE); var d=Piece(7,7);
                moves=LM.Limit;
                Swap(a,b); Swap(c,d);
                Check(LM.InputSwaps==2 && LM.Limit==moves-2, "Two disjoint booster swipes animate concurrently");
                Swap(a,b);
                Check(LM.InputSwaps==2 && LM.Limit==moves-2, "Moving pieces cannot start duplicate swaps");
                yield return qa.Settled(); Ownership();

                yield return qa.Practice();
                // A no-match swap on the deterministic top row must still return freely.
                a=Piece(5,0); b=Piece(6,0); var home=a.square; var other=b.square;
                moves=LM.Limit;
                Swap(a,b);
                Check(a.inputSwapLocked, "Invalid swipe gives its existing return feedback");
                yield return qa.Settled();
                Check(a.square==home && b.square==other && LM.Limit==moves, "Invalid swap returns without charging a move"); Ownership();

                yield return qa.Practice();
                var ball=Power(0,0,ItemsTypes.BOMB);
                Check(RoyalPowerupDirector.Tap(ball), "Color Ball accepts the same tap path");
                yield return null;
                var reserved=LM.GetItems().FirstOrDefault(i=>i && i.inputConversionLocks>0);
                var free=LM.GetItems().FirstOrDefault(i=>i && i.CanAcceptInput);
                Check(reserved && !reserved.CanAcceptInput && free, "Color Ball reserves selected targets while unrelated pieces stay interactive");
                var freeTnt=Power(free.square.col,free.square.row,ItemsTypes.PACKAGE);
                Check(RoyalPowerupDirector.Tap(freeTnt), "TNT can activate during Color Ball processing");
                yield return qa.Settled(); Ownership();

                yield return qa.Practice();
                a=Power(3,3,ItemsTypes.BOMB); b=Power(4,3,ItemsTypes.HORIZONTAL_STRIPPED);
                moves=LM.Limit; serial=RoyalPowerupDirector.Get.ColorBallHandoffs;
                Swap(a,b);
                yield return qa.Settled();
                Check(LM.Limit==moves-1 && RoyalPowerupDirector.Get.ColorBallHandoffs==serial+1,
                    "Color Ball + Rocket swipe preserves one move and existing combo handoff"); Ownership();

                yield return qa.Practice();
                a=Power(0,0,ItemsTypes.HORIZONTAL_STRIPPED); b=Power(7,7,ItemsTypes.PACKAGE);
                LM.gameStatus=GameState.Pause;
                Check(!RoyalPowerupDirector.Tap(a) && !a.CanAcceptInput, "Pause still blocks board actions");
                LM.gameStatus=GameState.Playing;
                MenuManager.Instance.ShowMenuPause();
                Check(!RoyalPowerupDirector.Tap(a), "Popup still owns input above the board");
                MenuManager.Instance.HideAllCanvasElements(); LM.gameStatus=GameState.Playing;
                a.square.type=SquareTypes.WIREBLOCK;
                Check(!a.CanAcceptInput && !RoyalPowerupDirector.Tap(a), "Locked cells retain their blocker rule");
                a.square.type=SquareTypes.EMPTY;
                a.falling=true; Check(!a.CanAcceptInput, "A falling piece cannot be moved twice"); a.falling=false;
                LM.Limit=1; LM.targetObject[0].targetCount=0; // Finish cleanly without a fail popup.
                Check(RoyalPowerupDirector.Tap(a) && !RoyalPowerupDirector.Tap(b) && LM.Limit==0, "Last move cannot be spent twice by rapid taps");
                yield return qa.EndSession();
                Check(!RoyalSave.Testing && UnityEngine.PlayerPrefs.GetInt("Gems")==money && UnityEngine.PlayerPrefs.GetInt("OpenLevel")==level,
                    "Normal currency and progress preserved by isolated tests");
                File.AppendAllText(Report,"ALL RESPONSIVE INPUT CHECKS PASSED\n");
            }
            finally
            {
                if(RoyalSave.Testing && qa)qa.StartCoroutine(qa.EndSession());
            }
        }
    }
}
#endif
