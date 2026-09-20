using System;
using System.Collections;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace RoyalThrone.Editor
{
    // Only called inside RoyalEffectsChecks' disposable board. Events go through the
    // production InputAction bindings, InputManager, collider hit test and Item.Update.
    // No real mouse injection, player level, save, reward or life is changed here.
    public static class RoyalInputRegression
    {
        static LevelManager level;
        static Mouse mouse;
        static Touchscreen touch;
        static Action<bool,string> routeCheck;
        static Item gestureSource;
        static Item At(int x,int y) => level.GetSquare(x,y).item;
        static void Paint(int x,int y,int color) => At(x,y).SetColor(color);

        static IEnumerator Seed()
        {
            // Settled gameplay schedules its next idle scan on LevelManager,
            // not AI. Remove that old fixture-board work before replacing items.
            // The suite itself runs on RoyalRegressionRunner, so remains active.
            level.StopAllCoroutines();
            AI.THIS.StopAllCoroutines(); AI.THIS.CancelVisibleHint(); AI.THIS.allowShowTip=false;
            foreach(var item in level.GameField.GetComponentsInChildren<Item>()) Object.DestroyImmediate(item.gameObject);
            foreach(var sq in level.squaresArray)
            {
                foreach(var block in sq.block) if(block) Object.DestroyImmediate(block);
                sq.block.Clear(); sq.type=SquareTypes.EMPTY; sq.item=null; sq.GenItem(false);
            }
            yield return null;
            foreach(var sq in level.squaresArray)
            {
                var item=sq.item;
                item.StopAllCoroutines(); item.SetColor((sq.col+sq.row*2)%5);
                item.currentType=item.NextType=ItemsTypes.NONE;
                item.falling=item.destroying=item.justCreatedItem=false;
                item.transform.position=sq.transform.position+Vector3.back*.2f;
            }
            level.lastDraggedItem=level.lastSwitchedItem=null; level.destroyAnyway.Clear();
            level.Limit=99; level.DragBlocked=false;
            Physics2D.SyncTransforms();
            yield return null;
        }

        static IEnumerator Gesture(Vector2 from,Vector2 to,bool touchscreen=false,Item expectedSource=null)
        {
            // Value-type position actions choose among all matching controls. Isolate
            // the virtual pointer so a real mouse or a stale touch position cannot win
            // disambiguation while this device supplies the button press.
            gestureSource=expectedSource;
            InputDevice pointer=touchscreen?(InputDevice)touch:mouse;
            InputManager.Instance.inputActionAsset.devices=new[]{pointer};
            yield return null;
            if(touchscreen)
                InputSystem.QueueStateEvent(touch,new TouchState{touchId=71,phase=UnityEngine.InputSystem.TouchPhase.Began,position=from});
            else InputSystem.QueueStateEvent(mouse,new MouseState{position=from,buttons=1});
            yield return new WaitForSeconds(.06f);
            VerifyRoute(from,true,"press",pointer);
            if(expectedSource) routeCheck(expectedSource.dragThis,"Pointer press acquires the expected source Item before movement: "+SourceState(expectedSource));
            if(touchscreen)
                InputSystem.QueueStateEvent(touch,new TouchState{touchId=71,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=to});
            else InputSystem.QueueStateEvent(mouse,new MouseState{position=to,buttons=1});
            yield return new WaitForSeconds(.06f);
            VerifyRoute(to,true,"move",pointer);
            if(touchscreen)
                InputSystem.QueueStateEvent(touch,new TouchState{touchId=71,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=to});
            else InputSystem.QueueStateEvent(mouse,new MouseState{position=to,buttons=0});
            yield return null;
            VerifyRoute(to,false,"release",pointer);
        }
        static string SourceState(Item source)
        {
            if(!source)return "source missing";
            var hit=source.GetComponent<BoxCollider2D>();
            return source.name+" cell="+source.square.col+","+source.square.row+" acquired="+source.dragThis+
                " falling="+source.falling+" destroying="+source.destroying+" colour="+source.color+
                " direction="+source.switchDirection+" pressWorld="+source.mousePos+" delta="+source.deltaPos+
                " root="+source.transform.position+" collider="+hit.bounds+" enabled="+hit.enabled;
        }
        static void VerifyRoute(Vector2 expected,bool held,string phase,InputDevice pointer)
        {
            var input=InputManager.Instance;
            string positionSource=input.Gameplay.Position.activeControl?.device.name??"none";
            string pressSource=input.Gameplay.PrimaryTouch.activeControl?.device.name??"none";
            var hit=Physics2D.OverlapPoint(Camera.main.ScreenToWorldPoint(input.TouchPosition));
            string detail=phase+" expected="+expected+" routed="+input.TouchPosition+" held="+input.IsTouching+
                " positionDevice="+positionSource+" pressDevice="+pressSource+" hit="+(hit?hit.name:"none")+
                " blocked="+level.DragBlocked+" status="+level.gameStatus+" onlyFalling="+level.onlyFalling+
                " source="+SourceState(gestureSource);
            Debug.Log("[Royal Input Route] "+detail);
            routeCheck(Vector2.Distance(input.TouchPosition,expected)<1.5f&&input.IsTouching==held&&
                (!held||input.Gameplay.PrimaryTouch.activeControl?.device==pointer),"Virtual pointer routes through production InputManager: "+detail);
        }
        static Vector2 Screen(Item item)=>Camera.main.WorldToScreenPoint(item.transform.position);
        static IEnumerator Settle(Action<bool,string> check)
        {
            float end=Time.realtimeSinceStartup+12;
            do{yield return null;}while((level.DragBlocked||RoyalPowerupDirector.Busy||level.GetItems().Any(i=>i&&(i.falling||i.destroying)))&&Time.realtimeSinceStartup<end);
            check(!level.DragBlocked&&!RoyalPowerupDirector.Busy,"Input event swap settles and returns control");
        }
        public static IEnumerator Run(LevelManager lm,Action<bool,string> check)
        {
            if(!lm.GameField.name.StartsWith("Royal workshop test board")) throw new InvalidOperationException("Input regression requires the disposable workshop board.");
            level=lm;var previousMouse=Mouse.current;var previousTouch=Touchscreen.current;
            var actionAsset=InputManager.Instance.inputActionAsset;var previousDeviceFilter=actionAsset.devices;routeCheck=check;
            var eventCamera=Camera.main;int previousEventMask=eventCamera?eventCamera.eventMask:0;
            int savedMoves=lm.Limit,savedScore=LevelManager.Score,savedMoveID=lm.moveID;
            int savedGems=InitScript.Gems,savedLives=InitScript.lifes;bool savedDragBlocked=lm.DragBlocked;
            mouse=InputSystem.AddDevice<Mouse>("Royal QA virtual mouse");
            touch=InputSystem.AddDevice<Touchscreen>("Royal QA virtual touch");
            try
            {
                // The InputAction filter does not control Unity's legacy
                // OnMouseDown/Drag messages. Keep a physical pointer from driving
                // those parallel callbacks during this virtual-device fixture.
                if(eventCamera)eventCamera.eventMask=0;
                check(eventCamera&&eventCamera.eventMask==0,"Virtual pointer fixture isolates legacy camera mouse callbacks");
                yield return Seed(); var first=At(1,1);var second=At(2,1);int moves=lm.Limit;
                yield return Gesture(Screen(first),Screen(second),false,first); yield return Settle(check);
                check(lm.Limit==moves&&At(1,1)==first&&At(2,1)==second,"Mouse invalid drag returns both pieces and spends no move");

                yield return Seed(); Paint(2,2,0);Paint(3,1,0);Paint(3,3,0);Paint(3,2,1);
                moves=lm.Limit;int score=LevelManager.Score;
                yield return Gesture(Screen(At(2,2)),Screen(At(3,2)),false,At(2,2));yield return Settle(check);
                check(lm.Limit==moves-1&&LevelManager.Score>score,"Mouse horizontal drag matches through production input and spends one move (moves "+moves+" -> "+lm.Limit+", score "+score+" -> "+LevelManager.Score+")");

                // This starts beyond the inherited 73% hit area, on the enlarged
                // ruby's visible right edge. The full production pointer path must work.
                yield return Seed(); Paint(2,2,0);Paint(3,1,0);Paint(3,3,0);Paint(3,2,1);
                first=At(2,2);second=At(3,2);moves=lm.Limit;score=LevelManager.Score;
                Vector3 edgeWorld=first.transform.position+Vector3.right*(lm.squareWidth*.43f);
                yield return Gesture(Camera.main.WorldToScreenPoint(edgeWorld),Screen(second),false,first);yield return Settle(check);
                check(lm.Limit==moves-1&&LevelManager.Score>score,"Mouse drag starting on visible gem edge matches and spends exactly one move (moves "+moves+" -> "+lm.Limit+", score "+score+" -> "+LevelManager.Score+")");
                check(!InputManager.Instance.IsTouching,"Edge-start drag releases without a stuck pointer");

                yield return Seed(); Paint(3,3,1);Paint(2,2,1);Paint(4,2,1);Paint(3,2,4);
                moves=lm.Limit;score=LevelManager.Score;
                yield return Gesture(Screen(At(3,3)),Screen(At(3,2)),true,At(3,3));yield return Settle(check);
                check(lm.Limit==moves-1&&LevelManager.Score>score,"Touch vertical drag matches through production input and spends one move (moves "+moves+" -> "+lm.Limit+", score "+score+" -> "+LevelManager.Score+")");

                yield return Seed(); first=At(2,2);second=At(3,2);moves=lm.Limit;lm.DragBlocked=true;
                yield return Gesture(Screen(first),Screen(second));
                check(lm.Limit==moves&&At(2,2)==first&&At(3,2)==second,"Drag during input lock does not exchange pieces");lm.DragBlocked=false;
                yield return Gesture(new Vector2(-20,-20),new Vector2(-80,-20));
                check(lm.Limit==moves,"Press outside board does not spend a move");
                check(!InputManager.Instance.IsTouching,"Releasing virtual inputs leaves no stuck drag state");
                check(lm.GetItems().Where(i=>i).All(i=>i.square.item==i),"Input tests preserve one piece per owned square");
            }
            finally
            {
                if(eventCamera)eventCamera.eventMask=previousEventMask;
                actionAsset.devices=previousDeviceFilter;
                if(mouse!=null&&mouse.added) InputSystem.RemoveDevice(mouse);
                if(touch!=null&&touch.added) InputSystem.RemoveDevice(touch);
                if(previousMouse!=null&&previousMouse.added) previousMouse.MakeCurrent();
                if(previousTouch!=null&&previousTouch.added) previousTouch.MakeCurrent();
                lm.Limit=savedMoves;LevelManager.Score=savedScore;lm.moveID=savedMoveID;lm.DragBlocked=savedDragBlocked;
                lm.lastDraggedItem=lm.lastSwitchedItem=null;mouse=null;touch=null;routeCheck=null;gestureSource=null;
            }
            check(lm.Limit==savedMoves&&LevelManager.Score==savedScore&&lm.moveID==savedMoveID&&lm.DragBlocked==savedDragBlocked,
                "Input regression restores caller moves, score, move ID and input-lock state");
            check(InitScript.Gems==savedGems&&InitScript.lifes==savedLives,"Pointer regression preserves gems and lives");
        }
    }
}
