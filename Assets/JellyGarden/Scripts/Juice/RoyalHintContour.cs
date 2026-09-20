using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    /// <summary>Exposed grid edges of a cell union, including concave corners and separate loops.</summary>
    public static class RoyalHintContour
    {
        public sealed class Shape
        {
            public readonly List<List<Vector2Int>> Loops=new List<List<Vector2Int>>();
            public int BoundaryEdges;
        }
        struct Edge
        {
            public Vector2Int From,To;
            public Edge(Vector2Int from,Vector2Int to){From=from;To=to;}
        }
        public static Shape Build(IEnumerable<Vector2Int> coordinates)
        {
            var cells=new HashSet<Vector2Int>(coordinates);var edges=new List<Edge>();
            var outgoing=new Dictionary<Vector2Int,List<int>>();var shape=new Shape();
            void Add(Vector2Int a,Vector2Int b){int index=edges.Count;edges.Add(new Edge(a,b));if(!outgoing.TryGetValue(a,out var list))outgoing[a]=list=new List<int>();list.Add(index);}
            foreach(var p in cells){
                if(!cells.Contains(p+Vector2Int.down))Add(p,p+Vector2Int.right);
                if(!cells.Contains(p+Vector2Int.right))Add(p+Vector2Int.right,p+Vector2Int.one);
                if(!cells.Contains(p+Vector2Int.up))Add(p+Vector2Int.one,p+Vector2Int.up);
                if(!cells.Contains(p+Vector2Int.left))Add(p+Vector2Int.up,p);
            }
            shape.BoundaryEdges=edges.Count;var used=new bool[edges.Count];
            for(int start=0;start<edges.Count;start++){
                if(used[start])continue;
                var loop=new List<Vector2Int>();int current=start;
                while(current>=0&&!used[current]){
                    var edge=edges[current];used[current]=true;loop.Add(edge.From);
                    if(edge.To==edges[start].From)break;
                    int next=-1;float best=-999;var incoming=edge.To-edge.From;
                    if(outgoing.TryGetValue(edge.To,out var options))foreach(int option in options){
                        if(used[option])continue;var direction=edges[option].To-edge.To;
                        // At a diagonal contact, turn left to retain separate closed outlines.
                        float turn=Mathf.Atan2(incoming.x*direction.y-incoming.y*direction.x,incoming.x*direction.x+incoming.y*direction.y);
                        if(turn>best){best=turn;next=option;}
                    }
                    current=next;
                }
                if(loop.Count>=4)shape.Loops.Add(loop);
            }
            return shape;
        }
    }
}
