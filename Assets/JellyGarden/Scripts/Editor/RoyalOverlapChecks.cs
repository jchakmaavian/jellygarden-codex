using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RoyalThrone.Editor
{
    public static class RoyalOverlapChecks
    {
        const string Folder="Logs/port-checks/QaFeedback/09-OverlapVerification/";
        static int sampledFrames;
        static bool ownership, reservations;
        static readonly Dictionary<Item,int> destroyingSince=new Dictionary<Item,int>();
        static bool stuck;

        static void Sample(LevelManager lm,int fps)
        {
            sampledFrames++;
            // Enumerate scene items, not just square.item: orphaned visible gems
            // are precisely what a cell-only audit would fail to see.
            var all=lm.GameField.GetComponentsInChildren<Item>();
            var live=all.Where(i=>i&&!i.destroying).ToArray();
            ownership &= live.All(i=>i.square&&i.square.item==i) && live.Select(i=>i.square).Distinct().Count()==live.Length;
            ownership &= lm.squaresArray.Where(s=>s&&s.item&&!s.item.destroying).All(s=>s.item.square==s);
            reservations &= lm.squaresArray.Where(s=>s&&s.Reserved).All(s=>!s.item||s.item==s.reservedFor);
            foreach(var item in all.Where(i=>i&&i.destroying)) {
                if(!destroyingSince.ContainsKey(item))destroyingSince[item]=sampledFrames;
                if(sampledFrames-destroyingSince[item]>fps*3)stuck=true;
            }
        }
        static bool Busy(LevelManager lm)=>lm.IsResolvingMatches||lm.InputSwaps>0||RoyalPowerupDirector.Busy||
            lm.GameField.GetComponentsInChildren<Item>().Any(i=>i&&(i.falling||i.destroying||i.inputSwapLocked));
        static IEnumerator Settle(LevelManager lm,int fps)
        {
            int calm=0;
            for(int f=0;f<fps*12;f++) {
                yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());Sample(lm,fps);
                calm=Busy(lm)?0:calm+1;
                if(calm>=fps)yield break; // includes landing motion and integrity watchdog window
            }
        }
        static void Audit(LevelManager lm,Action<bool,string> check,string label,int repairs)
        {
            var all=lm.GameField.GetComponentsInChildren<Item>().Where(i=>i).ToArray();
            float cell=lm.squareWidth*Mathf.Abs(lm.GameField.lossyScale.x);
            check(ownership,label+": every live scene gem retained unique reciprocal cell ownership throughout motion");
            check(reservations,label+": no refill entered a reserved special-creation cell");
            check(!stuck&&!Busy(lm),label+": no stuck destruction, falling, swap or resolver state");
            check(all.Length==64&&lm.squaresArray.All(s=>s.item)&&all.All(i=>!i.destroying&&i.square&&i.square.item==i),label+": exactly 64 live pieces refill the 64 cells; no orphan gems");
            check(all.All(i=>Vector2.Distance(i.transform.position,i.square.transform.position)<cell*.08f),label+": every settled visual rests in its owned cell");
            bool duplicates=false;
            for(int a=0;a<all.Length;a++)for(int b=a+1;b<all.Length;b++)if(Vector2.Distance(all[a].transform.position,all[b].transform.position)<cell*.5f)duplicates=true;
            check(!duplicates,label+": no two settled gem roots overlap in one cell");
            check(RoyalBoardIntegrity.Repairs==repairs,label+": no safety-net repair was needed to hide an ownership failure");
        }
        static void ResetCounters(){ownership=reservations=true;stuck=false;destroyingSince.Clear();}

        public static IEnumerator Run(LevelManager lm,Action<bool,string> check)
        {
            if(!RoyalThrone.Testing.RoyalSave.Testing)throw new InvalidOperationException("Overlap checks require an isolated QA profile");
            Directory.CreateDirectory(Folder);float capture=Time.captureDeltaTime;sampledFrames=0;
            try {
                foreach(int fps in new[]{15,30,60}) {
                    Time.captureDeltaTime=1f/fps;
                    foreach(string shape in new[]{"three-H","three-V","rocket-H","rocket-V","propeller","TNT","color-ball","concurrent"}) {
                        yield return RoyalThrone.Testing.RoyalQaService.Instance.Practice();
                        lm.limitType=LIMIT.MOVES;lm.Limit=99;
                        AI.THIS.StopAllCoroutines();AI.THIS.CancelVisibleHint();AI.THIS.allowShowTip=false;
                        foreach(var sq in lm.squaresArray){sq.item.SetColor(1+(sq.col+sq.row*2)%4);sq.item.currentType=sq.item.NextType=ItemsTypes.NONE;}
                        Item source=lm.GetSquare(4,4).item;Square dest=lm.GetSquare(3,4);
                        var group=new List<Item>{source};
                        if(shape=="three-V"||shape=="rocket-V") {group.Add(lm.GetSquare(3,2).item);group.Add(lm.GetSquare(3,3).item);if(shape=="rocket-V")group.Add(lm.GetSquare(3,5).item);}
                        else if(shape=="propeller"){group.Add(lm.GetSquare(2,3).item);group.Add(lm.GetSquare(3,3).item);group.Add(lm.GetSquare(2,4).item);}
                        else if(shape=="TNT"){group.Add(lm.GetSquare(3,2).item);group.Add(lm.GetSquare(3,3).item);group.Add(lm.GetSquare(1,4).item);group.Add(lm.GetSquare(2,4).item);}
                        else {
                            source=lm.GetSquare(3,5).item;group[0]=source;
                            group.Add(lm.GetSquare(1,4).item);group.Add(lm.GetSquare(2,4).item);
                            if(shape=="rocket-H"||shape=="color-ball")group.Add(lm.GetSquare(4,4).item);
                            if(shape=="color-ball")group.Add(lm.GetSquare(5,4).item);
                        }
                        foreach(var item in group){item.SetColor(0);item.GetComponent<RoyalPieceSkin>().ApplyNow();}
                        Item second=null;Square secondDest=null;
                        if(shape=="concurrent") {
                            second=lm.GetSquare(6,1).item;secondDest=lm.GetSquare(6,2);
                            foreach(var item in new[]{lm.GetSquare(4,2).item,lm.GetSquare(5,2).item,second}){item.SetColor(0);item.GetComponent<RoyalPieceSkin>().ApplyNow();}
                        }
                        int repairs=RoyalBoardIntegrity.Repairs;ResetCounters();
                        string label=fps+" FPS "+shape;
                        source.SwitchDirection(source.transform.position-dest.transform.position);
                        if(second)second.SwitchDirection(second.transform.position-secondDest.transform.position);
                        check(lm.InputSwaps==(second?2:1),label+": requested player swap(s) started");
                        yield return Settle(lm,fps);
                        check(group.All(i=>!i),label+": every expected matched source gem retired");
                        Audit(lm,check,label,repairs);
                    }
                    // Repeated real moves, including new legal swaps while a previous
                    // clear/refill is active. No board repainting between these moves.
                    yield return RoyalThrone.Testing.RoyalQaService.Instance.Practice();lm.limitType=LIMIT.MOVES;lm.Limit=99;
                    AI.THIS.StopAllCoroutines();AI.THIS.CancelVisibleHint();AI.THIS.allowShowTip=false;
                    ResetCounters();int startRepairs=RoyalBoardIntegrity.Repairs,started=0,duringResolution=0;
                    for(int frame=0;frame<fps*25&&started<24;frame++) {
                        yield return (UnityEngine.Application.isBatchMode?null:new WaitForEndOfFrame());Sample(lm,fps);
                        if(frame%Mathf.Max(1,fps/10)!=0)continue;
                        var plan=RoyalHintSolver.FindAll(lm).FirstOrDefault(p=>!p.IsTap&&p.First&&p.Second&&p.First.CanAcceptInput&&p.Second.CanAcceptInput);
                        if(plan==null)continue;
                        bool wasBusy=Busy(lm);int moves=lm.Limit,swaps=lm.InputSwaps;
                        plan.First.SwitchDirection(plan.First.transform.position-plan.Second.transform.position);
                        if(lm.Limit<moves||lm.InputSwaps>swaps){started++;if(wasBusy)duringResolution++;}
                    }
                    yield return Settle(lm,fps);
                    check(started>=12,fps+" FPS rapid sequence: "+started+" swaps started, "+duringResolution+" during active resolution");
                    Audit(lm,check,fps+" FPS rapid sequence",startRepairs);
                    if(!UnityEngine.Application.isBatchMode){var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Folder+"settled-"+fps+".png",tex.EncodeToPNG());Object.Destroy(tex);}
                }
                check(sampledFrames>1000,"Monitored "+sampledFrames+" rendered frames for live scene ownership and stuck destruction");
            }
            finally{Time.captureDeltaTime=capture;}
        }
    }
}
