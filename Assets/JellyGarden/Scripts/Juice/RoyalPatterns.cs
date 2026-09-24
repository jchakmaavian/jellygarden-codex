using System.Collections.Generic;
using UnityEngine;
namespace JellyGarden.Scripts.Juice
{
    public static class RoyalPatterns
    {
        public static List<Item> ExtraHint()
        {
            var lm=LevelManager.THIS;
            foreach(var item in lm.GetItems())
            {
                if(!item||item.destroying||item.falling||!item.square.CanGoOut())continue;
                if(RoyalPowerupDirector.IsPower(item.currentType))return new List<Item>{item};
                var a=item.square;
                foreach(var b in new[]{lm.GetSquare(a.col+1,a.row),lm.GetSquare(a.col,a.row+1)})
                {
                    if(!b||!b.item||!b.CanGoOut()||b.item.currentType!=ItemsTypes.NONE)continue;
                    var other=b.item; a.item=other;b.item=item;
                    bool match;
                    try{match=SquareAt(a).Count==4||SquareAt(b).Count==4;}
                    finally{a.item=item;b.item=other;}
                    if(match)return new List<Item>{item,other};
                }
            }
            return null;
        }
        public static List<Item> SquareAt(Square origin)
        {
            var empty=new List<Item>(); var lm=LevelManager.THIS;
            if(!origin||!origin.item||origin.item.currentType!=ItemsTypes.NONE||origin.item.destroying||!origin.CanGoOut())return empty;
            for(int dy=-1;dy<=0;dy++)for(int dx=-1;dx<=0;dx++)
            {
                var found=new List<Item>(4);
                for(int y=0;y<2;y++)for(int x=0;x<2;x++)
                {var sq=lm.GetSquare(origin.col+dx+x,origin.row+dy+y);if(sq&&sq.CanGoOut()&&sq.item&&!sq.item.destroying&&sq.item.currentType==ItemsTypes.NONE&&sq.item.color==origin.item.color)found.Add(sq.item);}
                if(found.Count==4)return found;
            }
            return empty;
        }
        public static void AddSquares(List<List<Item>> groups)
        {
            var used=new HashSet<Item>();foreach(var g in groups)foreach(var i in g)used.Add(i);
            var lm=LevelManager.THIS;
            for(int r=0;r<lm.maxRows;r++)for(int c=0;c<lm.maxCols;c++)
            {
                var g=SquareAt(lm.GetSquare(c,r));if(g.Count!=4||g.Exists(used.Contains))continue;
                Item host=lm.PreferredMatchHost(g);if(!host)host=g[0];
                host.NextType=ItemsTypes.HELICOPTER;groups.Add(g);foreach(var i in g)used.Add(i);
            }
        }
    }
}
