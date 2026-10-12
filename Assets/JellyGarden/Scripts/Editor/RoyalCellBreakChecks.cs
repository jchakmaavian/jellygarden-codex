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
    public static class RoyalCellBreakChecks
    {
        public static IEnumerator RunRestored(LevelManager lm,Action seed,Action colours,Action<bool,string> check)
        {
            const string review="Logs/port-checks/Effects/Unity-review-v6/";
            Directory.CreateDirectory(review);
            var service=RoyalToolkitImpact.Existing;
            check(service,"Original toolkit pool is ready");
            check(!RoyalMatchBreakService.Existing,"Replaced cell-emitter pool is not allocated");
            int objects=service.GetComponentsInChildren<Transform>(true).Length;
            string[] names={"horizontal-3","vertical-3","match-4","match-5","T-shape","L-shape","simultaneous","cascades"};
            for(int scenario=0;scenario<names.Length;scenario++)
            {
                service.Clear();CandyShatterService.Instance.Clear();seed();yield return null;colours();
                void Paint(int x,int y,int color=0)=>lm.GetSquare(x,y).item.SetColor(color);
                if(scenario==1){for(int y=2;y<=4;y++)Paint(3,y);Paint(3,1,2);}
                else if(scenario==4 || scenario==5)
                {for(int x=2;x<=4;x++)Paint(x,3);Paint(scenario==4?3:2,4);Paint(scenario==4?3:2,5);if(scenario==4)Paint(3,6,1);}
                else if(scenario==7)
                {
                    // Clear only the middle column. Its crown falls three cells
                    // between two stationary crowns to form the second wave.
                    Paint(2,3);Paint(2,4);Paint(2,5);
                    Paint(1,5,2);Paint(2,2,2);Paint(3,5,2);
                }
                else
                {int n=scenario==2?4:scenario==3?5:3;for(int x=1;x<=n;x++)Paint(x,3);if(n==3)Paint(4,3,2);if(scenario==6)for(int y=4;y<=6;y++)Paint(6,y,1);}
                yield return null;
                var groups=new JellyGarden.Scripts.System.CombineManager().GetCombine();
                var expected=groups.SelectMany(g=>g).Where(i=>i&&i.currentType==ItemsTypes.NONE).Distinct().Select(i=>i.square.transform.position).ToArray();
                check(expected.Length>=3,names[scenario]+" fixture contains a real matched shape");
                if(scenario<2)check(expected.Length==3,names[scenario]+" fixture is exactly three cells");
                if(scenario==2||scenario==3||scenario==4||scenario==5)check(expected.Length==(scenario==2?4:5),names[scenario]+" fixture has the exact intended shape size");
                int before=service.PlayedIce;
                yield return RoyalToolkitChecks.ResolveProductionMatch(lm,check,expected.Length,review+names[scenario]);
                yield return new WaitForSeconds(1f);
                check(CandyShatterService.Instance.ActiveFragments==0,names[scenario]+": fragment tails clean up");
                check(service.GetComponentsInChildren<Transform>(true).Length==objects,names[scenario]+": toolkit objects remain pooled");
                check(lm.GetItems().Where(i=>i).All(i=>!i.destroying&&!i.falling&&i.square.item==i),names[scenario]+": refill retains cell ownership");
                if(scenario==7)check(service.PlayedIce-before>=2,"Constructed cascade resolves a second wave");
            }
            seed();yield return null;colours();
            var source=lm.GetSquare(3,3).item;source.royalMatchSize=3;
            CandyShatterService.Instance.Clear();CandyShatterService.Instance.Shatter(source.sprRenderer,1);
            int initial=CandyShatterService.Instance.ActiveFragments;
            check(initial>=4&&initial<=7,"Restored authored fragment count is bounded at4-7");
            yield return new WaitForSeconds(.4f);
            check(CandyShatterService.Instance.ActiveFragments==initial,"Gentler falling tail remains visible at0.4s");
            yield return new WaitForSeconds(.45f);
            check(CandyShatterService.Instance.ActiveFragments==0,"Smooth fragment tail cleans up by0.85s");
        }
        const string Folder="Logs/port-checks/Effects/Unity-review-v5/";
        public static IEnumerator Run(LevelManager lm,Action seed,Action colours,Action<bool,string> check)
        {
            Directory.CreateDirectory(Folder);
            var service=RoyalMatchBreakService.Existing;
            check(service,"Cell-local break service prewarms with toolkit and fragment assets");
            if(!service)yield break;
            var systems=service.GetComponentsInChildren<ParticleSystem>(true);
            int objects=service.GetComponentsInChildren<Transform>(true).Length;
            check(systems.Length<=44,"All cells share a fixed number of Ice and fragment emitters ("+systems.Length+")");
            check(service.GetComponentsInChildren<SpriteRenderer>(true).Length==RoyalMatchBreakService.CellCapacity,"Gem snapshots prewarm in a fixed pool");
            check(systems.All(p=>p.main.simulationSpace==ParticleSystemSimulationSpace.World&&!p.emission.enabled),"Per-cell particles use explicit independent world-space emission");
            check(systems.Select(p=>p.GetComponent<ParticleSystemRenderer>()).All(r=>!r.enabled||r.sharedMaterial&&r.sharedMaterial.shader.isSupported),"All fragment and toolkit shaders are supported");
            string[] names={"horizontal-3","vertical-3","match-4","match-5","T-shape","L-shape","simultaneous","cascades"};
            for(int scenario=0;scenario<names.Length;scenario++)
            {
                service.Clear();seed();yield return null;colours();
                void Paint(int x,int y,int color=0)=>lm.GetSquare(x,y).item.SetColor(color);
                if(scenario==1){for(int y=2;y<=4;y++)Paint(3,y);Paint(3,1,2);}
                else if(scenario==4 || scenario==5)
                {for(int x=2;x<=4;x++)Paint(x,3);Paint(scenario==4?3:2,4);Paint(scenario==4?3:2,5);if(scenario==4)Paint(3,6,1);}
                else if(scenario==7)
                {
                    // Clear only the middle column. Its crown falls three cells
                    // between two stationary crowns to form the second wave.
                    Paint(2,3);Paint(2,4);Paint(2,5);
                    Paint(1,5,2);Paint(2,2,2);Paint(3,5,2);
                }
                else
                {int n=scenario==2?4:scenario==3?5:3;for(int x=1;x<=n;x++)Paint(x,3);if(n==3)Paint(4,3,2);if(scenario==6)for(int y=4;y<=6;y++)Paint(6,y,1);}
                yield return null;
                var groups=new JellyGarden.Scripts.System.CombineManager().GetCombine();
                var expected=groups.SelectMany(g=>g).Where(i=>i&&i.currentType==ItemsTypes.NONE).Distinct().Select(i=>i.square.transform.position).ToArray();
                check(expected.Length>=3,names[scenario]+" fixture contains a real matched shape");
                if(scenario<2)check(expected.Length==3,names[scenario]+" fixture is exactly three cells");
                if(scenario==2||scenario==3||scenario==4||scenario==5)check(expected.Length==(scenario==2?4:5),names[scenario]+" fixture has the exact intended shape size");
                foreach(var item in lm.GetItems())if(item)item.NextType=ItemsTypes.NONE;
                int before=service.PlayedCells,iceBefore=service.EmittedIceCells,fragmentsBefore=service.EmittedFragments;
                int oldCentral=RoyalToolkitImpact.Existing.PlayedIce;
                lm.lastDraggedItem=lm.lastSwitchedItem=null;lm.FindMatches();
                float end=Time.realtimeSinceStartup+15;float first=-1;int frame=0,visibleFrames=0,peakParticles=0;
                var sampleTimes=new[]{0f,.075f,.14f,.24f,.4f,.6f,.8f};
                do
                {
                    yield return new WaitForEndOfFrame();
                    if(service.PlayedCells>before && first<0)first=Time.time;
                    if(service.VisibleGems>0)visibleFrames++;
                    peakParticles=Mathf.Max(peakParticles,service.ParticleCount);
                    if(first>=0&&frame<sampleTimes.Length&&Time.time-first>=sampleTimes[frame])
                    {
                        var tex=ScreenCapture.CaptureScreenshotAsTexture();
                        File.WriteAllBytes(Folder+names[scenario]+"-"+frame+".png",tex.EncodeToPNG());Object.Destroy(tex);frame++;
                    }
                }while((lm.IsResolvingMatches || service.ActiveCells>0) && Time.realtimeSinceStartup<end);
                int played=service.PlayedCells-before;
                var actual=Enumerable.Range(0,Mathf.Min(played,RoyalMatchBreakService.CellCapacity)).Select(n=>service.RecentCenters[(before+n)%RoyalMatchBreakService.CellCapacity]).ToArray();
                check(expected.All(p=>actual.Any(a=>Vector3.Distance(a,p)<.001f)),names[scenario]+": every matched cell emits a break at its own grid position");
                check(service.EmittedIceCells-iceBefore==played,names[scenario]+": one Ice Impact per destroyed matched gem ("+played+")");
                check(RoyalToolkitImpact.Existing.PlayedIce==oldCentral,names[scenario]+": no shared center Ice overlay");
                check(service.EmittedFragments-fragmentsBefore>=played*8,names[scenario]+": at least twice the former minimum fragment count");
                check(visibleFrames>1,names[scenario]+": intact gem snapshots remain visibly animated before disappearing");
                check(peakParticles>0&&!lm.IsResolvingMatches,names[scenario]+": visible particles and production resolver both complete");
                check(service.ActiveCells==0&&service.DroppedCells==0,names[scenario]+": every tail cleans up without dropping a cell");
                check(service.GetComponentsInChildren<Transform>(true).Length==objects,names[scenario]+": cascades allocate no new effect objects");
                check(lm.GetItems().Where(i=>i).All(i=>!i.destroying&&!i.falling&&i.square.item==i),names[scenario]+": refill retains correct cell ownership");
                if(scenario==7)check(played>expected.Length,"Constructed cascade produces a second matched wave");
            }
            // Inspect a single local break's actual particles, independent of random refill.
            service.Clear();seed();yield return null;colours();
            var source=lm.GetSquare(3,3).item;source.SetColor(1);yield return null;
            float unit=Mathf.Abs(lm.squareWidth*lm.GameField.lossyScale.x);
            var origin=source.square.transform.position;
            var random=UnityEngine.Random.state;float expectedRandom=UnityEngine.Random.value;UnityEngine.Random.state=random;
            RoyalMatchBreakService.Play(source,3,0);
            check(UnityEngine.Random.value==expectedRandom,"Match decoration does not consume gameplay RNG");UnityEngine.Random.state=random;
            yield return new WaitForSeconds(.08f);
            check(service.VisibleGems==1,"Gem is still visible during the Ice impact");
            var buffer=new ParticleSystem.Particle[2048];
            check(systems.Where(p=>p.name.Contains("Ring")).Sum(p=>p.particleCount)>=1,"AllIn1 ring particles exist at the gem cell");
            yield return new WaitForSeconds(.18f);
            var chips=systems.Where(p=>p.name.StartsWith("Match chip")).ToArray();
            int fragments=chips.Sum(p=>p.particleCount);check(fragments==8,"Blue Match-3 emits eight authored fragments instead of four");
            check(service.VisibleGems==0,"Gem disappears after the supporting pop, before the falling tail completes");
            float timescale=Time.timeScale;Time.timeScale=0;
            yield return null; // Let the current frame's already-computed delta finish.
            var times=systems.Select(p=>p.time).ToArray();yield return new WaitForSecondsRealtime(.15f);
            check(systems.Select((p,i)=>Mathf.Abs(p.time-times[i])<.001f).All(x=>x),"Pause freezes Ice and falling fragments");Time.timeScale=timescale;
            yield return new WaitForSeconds(.25f);
            bool downward=true,local=true;int count=0;
            foreach(var ps in chips)
            {
                int n=ps.GetParticles(buffer);count+=n;
                for(int i=0;i<n;i++){downward&=buffer[i].velocity.y<0;local&=Mathf.Abs(buffer[i].position.x-origin.x)<unit*.65f&&buffer[i].position.y>origin.y-unit*1.5f;}
            }
            check(count==8&&downward,"Every fragment turns downward under gravity");check(local,"Falling fragments remain within local board bounds");
            yield return new WaitForSeconds(.16f);
            bool fades=true;foreach(var ps in chips){int n=ps.GetParticles(buffer);for(int i=0;i<n;i++)fades&=buffer[i].GetCurrentColor(ps).a<250&&buffer[i].GetCurrentSize(ps)<buffer[i].startSize;}
            check(chips.Sum(p=>p.particleCount)>0&&fades,"Fragments visibly fade and shrink near the end");
            yield return new WaitForSeconds(.2f);check(service.ActiveCells==0&&service.ParticleCount==0,"Break fully cleans up by 0.8 seconds");
            // A board-sized simultaneous wave must not hit the old four-Ice limit.
            int drop=service.DroppedCells,ice=service.EmittedIceCells;
            foreach(var square in lm.squaresArray)RoyalMatchBreakService.Play(square.item,5,0);
            yield return new WaitForSeconds(.3f);
            check(service.EmittedIceCells-ice==64&&service.DroppedCells==drop,"All 64 simultaneous cells receive independent Ice impacts");
            check(service.ParticleCount>500,"Large simultaneous wave retains rich per-cell fragments");
            service.Clear();check(service.ParticleCount==0&&service.ActiveCells==0,"Board cancellation clears all pooled match effects");
        }
    }
}
