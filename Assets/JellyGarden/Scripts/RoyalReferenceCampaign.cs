using System;
using System.Collections.Generic;
using System.Linq;
using JellyGarden.Scripts;
using JellyGarden.Scripts.Targets;
using RoyalThrone.Testing;
using UnityEngine;

namespace RoyalThrone.Campaign
{
    [Serializable] public sealed class CampaignManifest { public CampaignLevel[] levels; }
    [Serializable] public sealed class CampaignLevel
    {
        public int number,width,height,moves; public string name,sourceSha256;
        public int[] colors; public CampaignCell[] cells; public CampaignGoal[] goals;
        public CampaignPredefined[] predefined; public CampaignSet[] sets;
    }
    [Serializable] public sealed class CampaignCell { public int code,grass,fill; public bool predefined; }
    [Serializable] public sealed class CampaignGoal { public string name; public int count; public bool fromSettings; }
    [Serializable] public sealed class CampaignPredefined { public int x,y; public int[] ids; }
    [Serializable] public sealed class CampaignSet { public string name; public int ratio; public bool canFall; public int[] columns,fills; public CampaignSetElement[] elements; }
    [Serializable] public sealed class CampaignSetElement { public int code,ratio; }

    // Explicit QA campaign. Original level assets and normal-player keys remain authoritative
    // outside this profile. Data came from exact payloads, with its decoder/provenance in Design.
    public static class RoyalReferenceCampaign
    {
        public const string ResourcePath = "ReferenceGameplay/levels01-50";
        static CampaignManifest manifest;
        static bool enabled;
        public static bool Enabled => enabled && RoyalSave.Testing;
        // Player levels 1..OpeningLevels play these verified layouts in the player's own profile, through this same loader.
        // They were matched cell by cell against the reference build's opening boards (Design/RoyalMatchReference). The QA
        // campaign (Select, levels 1-50 in the test save) is unchanged; its level-number and home rules stay behind Enabled.
        public const int OpeningLevels = 5;
        static bool opening;
        /// <summary>A player opening level (1-5) is loaded from the verified reference layouts.</summary>
        public static bool Opening => opening && Current != null;
        public static bool Active => (Enabled || opening) && Current != null;
        public static CampaignLevel Current { get; private set; }
        public static bool IsBonus => Active && (Current.number==20||Current.number==40);
        static readonly HashSet<int> initialized=new HashSet<int>();
        static readonly Dictionary<int,Queue<int>> feeds=new Dictionary<int,Queue<int>>();
        static readonly Dictionary<RoyalThrone.Presentation.RoyalHomeScreen,int[]> originalHomeLevels=new Dictionary<RoyalThrone.Presentation.RoyalHomeScreen,int[]>();
        public static CampaignLevel[] Levels => Manifest.levels;
        static CampaignManifest Manifest
        {
            get
            {
                if(manifest!=null)return manifest;
                var data=Resources.Load<TextAsset>(ResourcePath);
                if(!data)throw new InvalidOperationException("Reference campaign resource is missing.");
                manifest=JsonUtility.FromJson<CampaignManifest>(data.text);
                if(manifest?.levels==null||manifest.levels.Length!=50)throw new InvalidOperationException("Reference campaign requires exactly 50 verified levels.");
                foreach(var l in manifest.levels)Validate(l);
                return manifest;
            }
        }
        public static void Validate(CampaignLevel l)
        {
            if(l==null||l.number<1||l.number>50||l.width<1||l.height<1||l.cells==null||l.cells.Length!=l.width*l.height||l.moves<=0||l.colors==null||l.colors.Length<2||l.goals==null||l.goals.Length==0||l.sourceSha256?.Length!=64)
                throw new InvalidOperationException("Invalid verified campaign level data.");
            if(l.colors.Any(c=>c<1||c>6))throw new InvalidOperationException("Invalid reference color code.");
        }
        public static void Select(bool value)
        {
            if(value&&!RoyalSave.Testing)throw new InvalidOperationException("Start the isolated test profile first.");
            enabled=value;Current=null;initialized.Clear();feeds.Clear();RoyalSave.ReferenceCampaign=value;
            foreach(var home in UnityEngine.Object.FindObjectsByType<RoyalThrone.Presentation.RoyalHomeScreen>(FindObjectsInactive.Include))
            {
                if(value){if(!originalHomeLevels.ContainsKey(home))originalHomeLevels[home]=home.levelNumbers;home.levelNumbers=Enumerable.Range(1,50).ToArray();}
                else if(originalHomeLevels.TryGetValue(home,out var original))home.levelNumbers=original;
                home.RefreshProgress();
            }
            if(!value)originalHomeLevels.Clear();
        }
        public static void RequirePlayable(int n)
        {
            if(!Enabled)return;
            if(n<1||n>50)throw new InvalidOperationException("Reference campaign contains levels 01–50 only.");
        }
        internal static void ReleaseBoard()
        {
            Current=null;opening=false;initialized.Clear();feeds.Clear();
        }
        public static bool TryLoad(LevelManager lm,int n,out SquareBlocks[] board)
        {
            Current=null;board=null;opening=false;
            if(Enabled)RequirePlayable(n);else if(n>=1&&n<=OpeningLevels)opening=true;else return false;
            Current=Levels.Single(l=>l.number==n);var l=Current;
            initialized.Clear();feeds.Clear();
            foreach(var group in l.predefined.GroupBy(p=>p.x))
                feeds[group.Key]=new Queue<int>(group.OrderBy(p=>p.y).SelectMany(p=>p.ids));
            lm.maxCols=l.width;lm.maxRows=l.height;lm.Limit=l.moves;lm.limitType=LIMIT.MOVES;
            lm.colorLimit=l.colors.Length;lm.target=Target.BLOCKS;
            // Completion is represented by one QA star; source files contain no score thresholds.
            lm.star1=1;lm.star2=int.MaxValue/4;lm.star3=int.MaxValue/2;
            Array.Clear(lm.ingrCountTarget,0,lm.ingrCountTarget.Length);
            lm.squaresArray=new Square[l.cells.Length];board=new SquareBlocks[l.cells.Length];
            for(int i=0;i<board.Length;i++)board[i]=new SquareBlocks{block=l.cells[i].code==0?SquareTypes.NONE:SquareTypes.EMPTY,obstacle=SquareTypes.NONE};
            lm.targetObject=l.goals.Select(g=>new TargetObject{type=Target.BLOCKS,referenceGoal=g.name,targetCount=g.count,countCollected=0,
                icon=RoyalCampaignBlockers.GoalIcon(g.name),blockSprites=new SpriteList()}).ToArray();
            // Explicit adaptation: source bonus goal is zero. Show available coins, and
            // finish when its authored 20-move budget ends rather than instantly winning.
            if(IsBonus)foreach(var t in lm.targetObject)t.targetCount=l.cells.Count(c=>c.code==60);
            lm.levelLoaded=true;return true;
        }
        public static int CodeAt(Square s)=>Active&&s&&s.row>=0&&s.col>=0&&s.row<Current.height&&s.col<Current.width?Current.cells[s.row*Current.width+s.col].code:0;
        public static int GrassAt(Square s)=>Active&&s?Current.cells[s.row*Current.width+s.col].grass:0;
        public static void Build(LevelManager lm){if(Active)RoyalCampaignBlockers.Build(lm);}
        public static void CollectGoal(string name,int amount=1)
        {
            if(!Active||amount<=0||!LevelManager.THIS)return;
            foreach(var t in LevelManager.THIS.targetObject.Where(t=>t.referenceGoal==name))
                t.countCollected=Mathf.Min(t.targetCount,t.countCollected+amount);
            LevelManager.Score=Mathf.Max(1,LevelManager.Score);LevelManager.THIS.stars=Mathf.Max(1,LevelManager.THIS.stars);
        }
        public static bool CheckBonusOutcome(LevelManager lm)
        {
            if(!IsBonus)return false;
            if(lm.Limit<=0){lm.Limit=0;lm.stars=1;LevelManager.Score=Mathf.Max(1,LevelManager.Score);lm.gameStatus=GameState.PreWinAnimations;}
            return true;
        }
        public static void ConfigureSpawn(Item item,bool falling)
        {
            if(!Active||!item||!item.square)return;
            int index=item.square.row*Current.width+item.square.col;
            bool first=!falling&&initialized.Add(index);
            int code=first?CodeAt(item.square):NextCode(item.square);
            bool fixedOpening=first&&code>=1&&code<=6;
            if(code==0||code==11||code==12||code==13||code==14||code==15||code==16)code=SetCode(item.square,code);
            var stamp=item.gameObject.AddComponent<RoyalCampaignPiece>();stamp.code=code;stamp.fixedOpening=fixedOpening;stamp.assigned=first&&!fixedOpening;
            if(RoyalCampaignBlockers.ConfigureSpawn(item,code))return;
            switch(code)
            {
                case 10: item.NextType=ItemsTypes.HELICOPTER;break;
                case 20: item.NextType=ItemsTypes.VERTICAL_STRIPPED;break;
                case 30: item.NextType=ItemsTypes.HORIZONTAL_STRIPPED;break;
                case 40: item.NextType=ItemsTypes.PACKAGE;break;
                case 50: item.NextType=ItemsTypes.BOMB;break;
            }
        }
        static int NextCode(Square square)
        {
            if(feeds.TryGetValue(square.col,out var queue)&&queue.Count>0)return queue.Dequeue();
            return SetCode(square,0);
        }
        static int SetCode(Square square,int code)
        {
            var choices=Current.sets.Where(s=>(code<14||code>16||s.name=="Set"+(code-13))&&s.canFall&&
                (s.columns==null||s.columns.Length==0||s.columns.Contains(square.col))&&s.elements!=null&&s.elements.Length>0).ToArray();
            if(choices.Length==0)return Current.colors[UnityEngine.Random.Range(0,Current.colors.Length)];
            int total=choices.Sum(s=>Mathf.Max(1,s.ratio)),pick=UnityEngine.Random.Range(0,total);var set=choices[0];
            foreach(var s in choices){set=s;pick-=Mathf.Max(1,s.ratio);if(pick<0)break;}
            return set.elements[UnityEngine.Random.Range(0,set.elements.Length)].code;
        }
        // Existing RoyalPieceSkin order: ruby, shield, crown, leaf, lily, vase.
        public static int ColorIndex(int code){switch(code){case 1:return 1;case 2:return 3;case 3:return 0;case 4:return 2;case 5:return 4;case 6:return 5;default:return 0;}}
        public static bool TryColor(Item item,int exceptColor)
        {
            if(!Active||!item||!item.square)return false;
            var stamp=item.GetComponent<RoyalCampaignPiece>();
            if(stamp&&(stamp.code==26||stamp.code==60))return true; // Egg/Coin view and identity are owned by the blocker component.
            if(item.currentType!=ItemsTypes.NONE||item.NextType!=ItemsTypes.NONE)
            {
                item.SetColor(Current.colors.Select(ColorIndex).First());
                if(item.currentType==ItemsTypes.BOMB||item.NextType==ItemsTypes.BOMB)item.color=555;
                return true;
            }
            if(stamp&&stamp.code>=1&&stamp.code<=6&&(!stamp.assigned||stamp.fixedOpening))
            {item.SetColor(ColorIndex(stamp.code));stamp.assigned=true;return true;}
            var colors=Current.colors.Select(ColorIndex).OrderBy(_=>UnityEngine.Random.value).ToArray();
            int chosen=colors.First();
            foreach(int c in colors)if(c!=exceptColor&&!WouldMatch(item.square,c)){chosen=c;break;}
            item.SetColor(chosen);return true;
        }
        static bool WouldMatch(Square s,int color)
        {
            Func<int,int,bool> same=(x,y)=>{
                if(x<0||y<0||x>=Current.width||y>=Current.height)return false;
                var q=LevelManager.THIS.GetSquare(x,y);if(!q||q==s||!q.CanGoInto())return false;
                if(q.item)return q.item.currentType==ItemsTypes.NONE&&q.item.color==color;
                int raw=CodeAt(q);return raw>=1&&raw<=6&&ColorIndex(raw)==color;
            };
            int x=s.col,y=s.row;
            if((same(x-1,y)&&same(x-2,y))||(same(x-1,y)&&same(x+1,y))||(same(x+1,y)&&same(x+2,y))||
               (same(x,y-1)&&same(x,y-2))||(same(x,y-1)&&same(x,y+1))||(same(x,y+1)&&same(x,y+2)))return true;
            for(int dx=-1;dx<=1;dx+=2)for(int dy=-1;dy<=1;dy+=2)if(same(x+dx,y)&&same(x,y+dy)&&same(x+dx,y+dy))return true;
            return false;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){enabled=false;opening=false;Current=null;manifest=null;initialized.Clear();feeds.Clear();originalHomeLevels.Clear();}
    }
}
