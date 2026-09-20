using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed class RoyalHintPlan
    {
        public readonly Item First,Second;
        public readonly List<Square> MatchCells;
        public readonly bool IsTap;
        public readonly int LongestRun;
        readonly Transform field;
        readonly Square[] board;
        readonly Item[] occupants;
        readonly int[] colours;
        readonly ItemsTypes[] types;
        readonly SquareTypes[] squareTypes;
        readonly int move;
        internal RoyalHintPlan(LevelManager lm,Item first,Item second,List<Square> cells,int longestRun=0,bool tap=false)
        {
            First=first;Second=second;MatchCells=cells;LongestRun=longestRun;IsTap=tap;
            MatchCells.Sort((a,b)=>a.row==b.row?a.col.CompareTo(b.col):a.row.CompareTo(b.row));
            field=lm.GameField;board=lm.squaresArray;move=lm.moveID;
            occupants=new Item[board.Length];colours=new int[board.Length];types=new ItemsTypes[board.Length];squareTypes=new SquareTypes[board.Length];
            for(int i=0;i<board.Length;i++)if(board[i]){squareTypes[i]=board[i].type;if(board[i].item){occupants[i]=board[i].item;colours[i]=occupants[i].color;types[i]=occupants[i].currentType;}}
        }
        public bool IsCurrent(LevelManager lm)
        {
            if(!lm||lm.GameField!=field||lm.squaresArray!=board||lm.moveID!=move||!First)return false;
            for(int i=0;i<board.Length;i++){
                var sq=board[i];var item=sq?sq.item:null;
                // A destroyed Unity object compares equal to null, even when both
                // snapshot and cell still contain its managed reference.
                if(!ReferenceEquals(occupants[i],null)&&(!item||!sq))return false;
                if(sq&&sq.type!=squareTypes[i])return false;
                if(item!=occupants[i])return false;
                if(item&&(!item.gameObject.activeInHierarchy||item.destroying||item.falling||item.square!=sq||item.color!=colours[i]||item.currentType!=types[i]))return false;
            }
            return true;
        }
    }

    /// <summary>Evaluates a virtual colour view. Never swaps ownership, changes NextType, or consumes RNG.</summary>
    public static class RoyalHintSolver
    {
        public static RoyalHintPlan Find(LevelManager lm)
        {
            if(!lm||lm.squaresArray==null)return null;
            RoyalHintPlan best=null;Item tap=null;
            for(int row=0;row<lm.maxRows;row++)for(int col=0;col<lm.maxCols;col++){
                var square=lm.GetSquare(col,row);if(!square||!square.item)continue;
                var item=square.item;
                if(!tap&&CanMove(item)&&RoyalPowerupDirector.IsPower(item.currentType))tap=item;
                foreach(var direction in new[]{Vector2Int.right,Vector2Int.up}){
                    var next=lm.GetSquare(col+direction.x,row+direction.y);if(!next||!next.item)continue;
                    var candidate=EvaluateSwap(lm,item,next.item);
                    if(candidate!=null&&(best==null||candidate.MatchCells.Count*10+candidate.LongestRun>best.MatchCells.Count*10+best.LongestRun))best=candidate;
                }
            }
            // A power remains a legal way to progress even when no ordinary swap exists.
            return best??(tap?new RoyalHintPlan(lm,tap,null,new List<Square>{tap.square},0,true):null);
        }
        static bool CanMove(Item item)
        {return item&&!item.destroying&&!item.falling&&item.gameObject.activeInHierarchy&&item.square&&item.square.item==item&&item.square.CanGoInto()&&item.square.CanGoOut();}
        public static RoyalHintPlan EvaluateSwap(LevelManager lm,Item first,Item second)
        {
            if(!lm||!CanMove(first)||!CanMove(second)||first.currentType!=ItemsTypes.NONE||second.currentType!=ItemsTypes.NONE||first.color==second.color)return null;
            var a=first.square;var b=second.square;
            if(Mathf.Abs(a.col-b.col)+Mathf.Abs(a.row-b.row)!=1)return null;
            var cells=new HashSet<Square>();int longest=0;
            Item At(int x,int y){var sq=lm.GetSquare(x,y);if(!sq)return null;return sq==a?second:sq==b?first:sq.item;}
            bool Same(int x,int y,int colour,bool squareRule=false){var sq=lm.GetSquare(x,y);var item=At(x,y);return sq&&sq.CanGoInto()&&(!squareRule||sq.CanGoOut())&&item&&!item.destroying&&item.currentType==ItemsTypes.NONE&&item.color==colour;}
            void CollectLines(Square origin)
            {
                int colour=At(origin.col,origin.row).color;
                foreach(var axis in new[]{Vector2Int.right,Vector2Int.up}){
                    var run=new List<Square>{origin};
                    for(int sign=-1;sign<=1;sign+=2)for(int n=1;n<Mathf.Max(lm.maxCols,lm.maxRows);n++){
                        int x=origin.col+axis.x*n*sign,y=origin.row+axis.y*n*sign;
                        if(!Same(x,y,colour))break;run.Add(lm.GetSquare(x,y));
                    }
                    if(run.Count>=3){longest=Mathf.Max(longest,run.Count);foreach(var sq in run)cells.Add(sq);}
                }
            }
            void CollectSquare(Square origin)
            {
                if(!RoyalEffectsProfile.Get)return;
                int colour=At(origin.col,origin.row).color;
                for(int dy=-1;dy<=0;dy++)for(int dx=-1;dx<=0;dx++){
                    int x=origin.col+dx,y=origin.row+dy;
                    if(Same(x,y,colour,true)&&Same(x+1,y,colour,true)&&Same(x,y+1,colour,true)&&Same(x+1,y+1,colour,true)){
                        var square=new[]{lm.GetSquare(x,y),lm.GetSquare(x+1,y),lm.GetSquare(x,y+1),lm.GetSquare(x+1,y+1)};
                        // RoyalPatterns.AddSquares consumes line groups first and
                        // rejects a helicopter square sharing any already-used item.
                        foreach(var sq in square)if(cells.Contains(sq))return;
                        foreach(var sq in square)cells.Add(sq);
                        return;
                    }
                }
            }
            CollectLines(a);CollectLines(b);CollectSquare(a);CollectSquare(b);
            return cells.Count>=3?new RoyalHintPlan(lm,first,second,new List<Square>(cells),longest):null;
        }
    }
}
