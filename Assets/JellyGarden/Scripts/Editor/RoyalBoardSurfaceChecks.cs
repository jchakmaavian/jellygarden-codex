using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Juice;
using RoyalThrone.Presentation;
using UnityEngine;

namespace RoyalThrone.Editor
{
    /// <summary>Exercises the live board surface cache only on the disposable workshop board.</summary>
    public static class RoyalBoardSurfaceChecks
    {
        static MeshRenderer[] Renderers(RoyalBoardSurface surface)=>surface.GetComponentsInChildren<MeshRenderer>(true)
            .Where(r=>r.transform.parent==surface.transform&&r.name.StartsWith("Board surface - ")).ToArray();
        static Dictionary<string,Mesh> Meshes(MeshRenderer[] renderers)=>renderers.ToDictionary(r=>r.name,r=>r.GetComponent<MeshFilter>().sharedMesh);
        static Dictionary<string,Material> Materials(MeshRenderer[] renderers)=>renderers.ToDictionary(r=>r.name,r=>r.sharedMaterial);
        // The complete 8x8 fixture: grout under all 64 cells, the drop shadow only under the 15 cells of the bottom row and
        // right column (the only silhouette that can show it), and eight segments on each gold edge.
        static int FixtureVertices(string name)=>name.EndsWith("grout")?64*4:name.EndsWith("shadow")?15*4:8*4;
        static int SortingOrder(string name)=>name.EndsWith("grout")?-2:name.EndsWith("shadow")?-1:2;
        public static IEnumerator Run(LevelManager lm,Action<bool,string> check)
        {
            if(!lm.GameField||!lm.GameField.name.StartsWith("Royal workshop test board"))throw new InvalidOperationException("Board surface checks require the disposable workshop board.");
            check(lm.maxCols==8&&lm.maxRows==8&&lm.squaresArray.All(s=>s&&s.type!=SquareTypes.NONE),"Board surface fixture is a complete 8x8 silhouette");
            AI.THIS.CancelVisibleHint();
            var squares=lm.squaresArray;var items=squares.Select(s=>s.item).ToArray();var squareObjects=squares.Select(s=>s.gameObject).ToArray();
            var item=lm.GetSquare(3,3).item;var skin=item.GetComponent<RoyalPieceSkin>();
            int colour=item.color;var type=item.currentType;var next=item.NextType;
            int moves=lm.Limit,score=LevelManager.Score,move=lm.moveID,gems=InitScript.Gems,lives=InitScript.lifes;
            var outlines=squares.SelectMany(s=>s.GetComponentsInChildren<Transform>(true))
                .Where(t=>t.name.ToLowerInvariant().Contains("outline")&&t.GetComponent<SpriteRenderer>()).ToArray();
            var surface=lm.GameField.GetComponent<RoyalBoardSurface>()??lm.GameField.gameObject.AddComponent<RoyalBoardSurface>();
            try
            {
                foreach(var pieceType in new[]{ItemsTypes.NONE,ItemsTypes.HORIZONTAL_STRIPPED,ItemsTypes.VERTICAL_STRIPPED,ItemsTypes.PACKAGE,ItemsTypes.BOMB,ItemsTypes.HELICOPTER})
                for(int color=0;color<(pieceType==ItemsTypes.NONE?6:1);color++){
                    item.SetColor(color);item.currentType=pieceType;item.NextType=ItemsTypes.NONE;skin.ClearHintPose();skin.ApplyNow();
                    // Idle rockets keep their two-headed art at 78% of the cell (Rocket review v6); every other piece fills 94%.
                    bool rocket=pieceType==ItemsTypes.HORIZONTAL_STRIPPED||pieceType==ItemsTypes.VERTICAL_STRIPPED;
                    float fill=rocket?RoyalPieceSkin.RocketIdleCellFill:RoyalPieceSkin.CellFill;
                    var bounds=item.sprRenderer.bounds.size;float x=bounds.x/lm.squareWidth,y=bounds.y/lm.squareHeight;
                    check(Mathf.Abs(Mathf.Max(x,y)-fill)<.004f&&x<=.95f&&y<=.95f,
                        "Gameplay "+pieceType+" colour "+color+" fills "+Mathf.RoundToInt(fill*100)+"% of its cell without crossing the cell bounds");
                }
                item.currentType=ItemsTypes.VERTICAL_STRIPPED;skin.ApplyNow();yield return new WaitForEndOfFrame();
                var vertical=RoyalGameplayArt.Art("rocket-vertical");var horizontal=RoyalGameplayArt.Art("rocket");
                check(vertical&&horizontal&&vertical.texture!=horizontal.texture&&item.sprRenderer.sprite==vertical,
                    "Vertical rocket renders the dedicated vertical artwork after the Animator and skin update");
                check(Quaternion.Angle(item.sprRenderer.transform.localRotation,Quaternion.identity)<.1f&&item.sprRenderer.bounds.size.y>item.sprRenderer.bounds.size.x,
                    "Dedicated vertical rocket stays upright without applying a second 90-degree rotation");

                var presentation=lm.GetComponent<RoyalGameplayPresentation>();
                if(presentation)
                {
                    presentation.StyleBoard(lm);yield return new WaitForEndOfFrame();
                    check(squares.All(s=>{
                        var cell=s.GetComponent<SpriteRenderer>();var bounds=cell.bounds;
                        return cell.enabled&&Mathf.Abs(bounds.size.x-lm.squareWidth)<.004f&&Mathf.Abs(bounds.size.y-lm.squareHeight)<.004f&&
                            Vector2.Distance(bounds.center,s.transform.position)<.004f;
                    }),"Every cell background fits exactly one square instead of spilling its native512px bounds beyond the board");
                    var corner=squares[0];var cornerType=corner.type;var cornerRenderer=corner.GetComponent<SpriteRenderer>();
                    try
                    {
                        corner.type=SquareTypes.NONE;cornerRenderer.enabled=true;presentation.StyleBoard(lm);
                        check(!cornerRenderer.enabled,"Irregular-board NONE cells remain invisible even if an inherited renderer was enabled");
                    }
                    finally{corner.type=cornerType;presentation.StyleBoard(lm);}
                    check(cornerRenderer.enabled&&Mathf.Abs(cornerRenderer.bounds.size.x-lm.squareWidth)<.004f,
                        "Restoring a playable fixture cell restores its fitted background without scaling its transform");
                }

                surface.RebuildIfNeeded(lm);var renderers=Renderers(surface);
                var expected=new HashSet<string>{"Board surface - grout","Board surface - shadow","Board surface - top","Board surface - bottom","Board surface - left","Board surface - right"};
                check(surface.RenderLayerCount==6&&renderers.Length==6&&expected.SetEquals(renderers.Select(r=>r.name)),
                    "Board surface renders exactly the grout, shadow and four gold edge layers");
                check(surface.EdgeCount==32,"Complete 8x8 board has exactly 32 exposed frame edges");
                var meshes=Meshes(renderers);var materials=Materials(renderers);var rendererIds=renderers.ToDictionary(r=>r.name,r=>r.GetEntityId());
                check(meshes.Values.All(m=>m&&m.vertexCount>0)&&materials.Values.All(m=>m&&m.mainTexture)&&
                    meshes.Values.Select(m=>m.GetEntityId()).Distinct().Count()==6&&materials.Values.Select(m=>m.GetEntityId()).Distinct().Count()==6,
                    "All six board layers have live textured materials and independent cached meshes");
                check(renderers.All(r=>r.enabled&&r.sortingOrder==SortingOrder(r.name)),
                    "Board grout and shadow stay behind cells and gold edges remain visible above the board");
                check(meshes.All(p=>p.Value.vertexCount==FixtureVertices(p.Key)),
                    "Surface geometry covers 64 cells, shadows the 15 bottom-right silhouette cells and has eight segments on each outside edge");
                for(int i=0;i<3;i++)surface.RebuildIfNeeded(lm);
                check(Renderers(surface).All(r=>r.GetEntityId()==rendererIds[r.name]&&r.GetComponent<MeshFilter>().sharedMesh==meshes[r.name]&&r.sharedMaterial==materials[r.name])&&Renderers(surface).Length==6,
                    "Repeated same-board rebuild checks reuse renderer, mesh and material IDs without duplication");

                // EnableMap clears GameField children in production. Exercise that
                // lifecycle by deleting only this fixture's decorative surface GOs.
                foreach(var renderer in Renderers(surface))UnityEngine.Object.DestroyImmediate(renderer.gameObject);
                check(Renderers(surface).Length==0&&squareObjects.All(s=>s)&&items.All(i=>i),
                    "Surface renderer cleanup removes only decorative children and preserves the fixture board");
                surface.RebuildIfNeeded(lm);renderers=Renderers(surface);
                check(surface.EdgeCount==32&&surface.RenderLayerCount==6&&renderers.Length==6&&expected.SetEquals(renderers.Select(r=>r.name)),
                    "Same-field same-array rebuild recreates all six renderers after level cleanup");
                check(renderers.All(r=>r.GetEntityId()!=rendererIds[r.name]&&r.GetComponent<MeshFilter>().sharedMesh==meshes[r.name]&&r.sharedMaterial==materials[r.name]),
                    "Recovered renderers keep the original mesh and material IDs instead of allocating replacement assets");
                surface.RebuildIfNeeded(lm);
                check(Renderers(surface).Length==6&&Renderers(surface).All(r=>r.GetComponent<MeshFilter>().sharedMesh.vertexCount==FixtureVertices(r.name)),
                    "Recovery followed by another rebuild creates no duplicate renderer or mesh geometry");
                if(outlines.Length>0)check(outlines.All(t=>t&&!t.GetComponent<SpriteRenderer>().enabled),
                    "Serialized board outline objects remain present while their old renderers are hidden");
                check(ReferenceEquals(lm.squaresArray,squares)&&squares.Select((s,index)=>s&&s.item==items[index]&&items[index].square==s).All(v=>v),
                    "Surface lifecycle preserves every square and item ownership reference");
                check(lm.Limit==moves&&LevelManager.Score==score&&lm.moveID==move&&InitScript.Gems==gems&&InitScript.lifes==lives,
                    "Board surface and native artwork checks leave moves, score, progression and lives unchanged");
            }
            finally
            {
                if(item){item.SetColor(colour);item.currentType=type;item.NextType=next;skin.ClearHintPose();skin.ApplyNow();}
                if(surface&&lm&&lm.GameField==surface.transform)surface.RebuildIfNeeded(lm);
            }
        }
    }
}
