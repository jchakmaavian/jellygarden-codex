using System.Collections.Generic;
using UnityEngine;

namespace JellyGarden.Scripts.Juice
{
    public sealed class RoyalHintPlan
    {
        public readonly Item First,Second;
        public readonly List<Square> MatchCells;
        // One connected same-colour result for visual guidance. MatchCells still
        // holds every result of the selected swap for gameplay and move ranking.
        public readonly List<Square> PreviewCells;
        public readonly bool IsTap;
        public readonly int LongestRun;
        /// <summary>How much the move does (RoyalHintSolver.Impact): a power combo, a power made or used, or a plain match.</summary>
        public readonly int Impact;
        readonly Transform field;
        readonly Square[] board;
        readonly Item[] occupants;
        readonly int[] colours;
        readonly ItemsTypes[] types;
        readonly SquareTypes[] squareTypes;
        readonly int move;
        internal RoyalHintPlan(LevelManager lm,Item first,Item second,List<Square> cells,int longestRun=0,bool tap=false,int impact=RoyalHintSolver.Impact.Match,List<Square> preview=null)
        {
            First=first;Second=second;MatchCells=cells;LongestRun=longestRun;IsTap=tap;Impact=impact;
            MatchCells.Sort((a,b)=>a.row==b.row?a.col.CompareTo(b.col):a.row.CompareTo(b.row));
            PreviewCells=preview??new List<Square>(cells);
            PreviewCells.Sort((a,b)=>a.row==b.row?a.col.CompareTo(b.col):a.row.CompareTo(b.row));
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
        /// <summary>A match's impact, highest first. The hint shows only colour matches (2026-10-04, the user: never mark a
        /// Rocket or any single piece), the one with the biggest result: what it makes first, then how many gems it takes.</summary>
        public static class Impact
        {
            public const int ColorBall=5;    // five in a line makes a Color Ball
            public const int Tnt=4;          // an L or T makes a TNT
            public const int Rocket=3;       // four in a line makes a Rocket
            public const int Propeller=2;    // a 2x2 square makes a Propeller
            public const int Match=1;        // a plain match
        }
        static long Score(RoyalHintPlan plan)=>plan.Impact*100000L+plan.MatchCells.Count*100+plan.LongestRun;
        public static RoyalHintPlan Find(LevelManager lm)
        {
            if(!lm||lm.squaresArray==null)return null;
            RoyalHintPlan best=null;long bestScore=long.MinValue;
            void Consider(RoyalHintPlan candidate){if(candidate==null)return;long score=Score(candidate);if(best==null||score>bestScore){best=candidate;bestScore=score;}}
            // Ordinary swaps of two plain gems only (EvaluateSwap rejects power-ups): no tap hints, no combo hints. No match
            // at all returns null, and AI.CheckPossibleCombines then shuffles the board (RoyalShuffle).
            for(int row=0;row<lm.maxRows;row++)for(int col=0;col<lm.maxCols;col++){
                var square=lm.GetSquare(col,row);if(!square||!square.item)continue;
                var item=square.item;
                foreach(var direction in new[]{Vector2Int.right,Vector2Int.up}){
                    var next=lm.GetSquare(col+direction.x,row+direction.y);if(!next||!next.item)continue;
                    Consider(EvaluateSwap(lm,item,next.item));
                }
            }
            return best;
        }
        static bool CanMove(Item item)
        {return item&&!item.destroying&&!item.falling&&item.gameObject.activeInHierarchy&&item.square&&item.square.item==item&&item.square.CanGoInto()&&item.square.CanGoOut();}
        static List<Square> SinglePreview(LevelManager lm,HashSet<Square> cells,Item first,Item second)
        {
            Item After(Square sq)=>sq==first.square?second:sq==second.square?first:sq.item;
            var ordered=new List<Square>(cells);
            ordered.Sort((a,b)=>a.row==b.row?a.col.CompareTo(b.col):a.row.CompareTo(b.row));
            var remaining=new HashSet<Square>(cells);List<Square> best=null;long bestScore=long.MinValue;
            foreach(var seed in ordered){
                if(!remaining.Remove(seed))continue;
                var group=new List<Square>{seed};int colour=After(seed).color;
                for(int i=0;i<group.Count;i++)foreach(var d in new[]{Vector2Int.right,Vector2Int.left,Vector2Int.up,Vector2Int.down}){
                    var neighbour=lm.GetSquare(group[i].col+d.x,group[i].row+d.y);
                    if(neighbour&&remaining.Contains(neighbour)&&After(neighbour).color==colour){remaining.Remove(neighbour);group.Add(neighbour);}
                }
                var shape=new HashSet<Vector2Int>();foreach(var sq in group)shape.Add(new Vector2Int(sq.col,sq.row));
                int longest=0;bool cross=false,square=false;
                foreach(var p in shape){
                    int Run(Vector2Int axis){int n=1;foreach(int sign in new[]{-1,1})for(int step=1;shape.Contains(p+axis*step*sign);step++)n++;return n;}
                    int h=Run(Vector2Int.right),v=Run(Vector2Int.up);longest=Mathf.Max(longest,h,v);cross|=h>=3&&v>=3;
                    square|=shape.Contains(p+Vector2Int.right)&&shape.Contains(p+Vector2Int.up)&&shape.Contains(p+Vector2Int.one);
                }
                int impact=longest>=5?Impact.ColorBall:cross?Impact.Tnt:longest==4?Impact.Rocket:square?Impact.Propeller:Impact.Match;
                long score=impact*100000L+group.Count*100+longest;
                // Sorted seeds make equal-result ties stable, including when the
                // same exchange is evaluated with its partners in reverse order.
                if(best==null||score>bestScore){best=group;bestScore=score;}
            }
            return best;
        }
        public static RoyalHintPlan EvaluateSwap(LevelManager lm,Item first,Item second)
        {
            // A gem may also swap with an ingredient (the game accepts it when the gem makes a match): only the gem can match,
            // where it lands. Before, such moves were not seen, and a board with only them was shuffled (bug review 2026-10-05).
            bool firstIngredient=first&&first.currentType==ItemsTypes.INGREDIENT,secondIngredient=second&&second.currentType==ItemsTypes.INGREDIENT;
            if(!lm||!CanMove(first)||!CanMove(second)||firstIngredient&&secondIngredient)return null;
            if(first.currentType!=ItemsTypes.NONE&&!firstIngredient||second.currentType!=ItemsTypes.NONE&&!secondIngredient)return null;
            if(!firstIngredient&&!secondIngredient&&first.color==second.color)return null;
            var a=first.square;var b=second.square;
            if(Mathf.Abs(a.col-b.col)+Mathf.Abs(a.row-b.row)!=1)return null;
            var cells=new HashSet<Square>();int longest=0;bool cross=false,square2=false;
            Item At(int x,int y){var sq=lm.GetSquare(x,y);if(!sq)return null;return sq==a?second:sq==b?first:sq.item;}
            bool Same(int x,int y,int colour,bool squareRule=false){var sq=lm.GetSquare(x,y);var item=At(x,y);return sq&&sq.CanGoInto()&&(!squareRule||sq.CanGoOut())&&item&&!item.destroying&&!RoyalThrone.Campaign.RoyalCampaignBlockers.IsSpecialItem(item)&&item.currentType==ItemsTypes.NONE&&item.color==colour;}
            void CollectLines(Square origin)
            {
                int colour=At(origin.col,origin.row).color;int axes=0;
                foreach(var axis in new[]{Vector2Int.right,Vector2Int.up}){
                    var run=new List<Square>{origin};
                    for(int sign=-1;sign<=1;sign+=2)for(int n=1;n<Mathf.Max(lm.maxCols,lm.maxRows);n++){
                        int x=origin.col+axis.x*n*sign,y=origin.row+axis.y*n*sign;
                        if(!Same(x,y,colour))break;run.Add(lm.GetSquare(x,y));
                    }
                    if(run.Count>=3){longest=Mathf.Max(longest,run.Count);axes++;foreach(var sq in run)cells.Add(sq);}
                }
                if(axes==2)cross=true;   // runs along both axes through one piece: an L or a T
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
                        square2=true;
                        return;
                    }
                }
            }
            if(!secondIngredient){CollectLines(a);CollectSquare(a);}   // `second` lands on a
            if(!firstIngredient){CollectLines(b);CollectSquare(b);}    // `first` lands on b
            int impact=longest>=5?Impact.ColorBall:cross?Impact.Tnt:longest==4?Impact.Rocket:square2?Impact.Propeller:Impact.Match;
            return cells.Count>=3?new RoyalHintPlan(lm,first,second,new List<Square>(cells),longest,false,impact,SinglePreview(lm,cells,first,second)):null;
        }
    }
}
