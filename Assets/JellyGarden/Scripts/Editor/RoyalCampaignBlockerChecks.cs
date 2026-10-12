using System;
using System.Linq;
using JellyGarden.Scripts;
using RoyalThrone.Campaign;
using UnityEngine;

namespace RoyalThrone.Editor
{
    /// <summary>
    /// Destructive obstacle checks on the suite's disposable QA board. This exercises
    /// real Square hits and Item destruction; it does not invoke a win or save progress.
    /// The caller must load a fresh board afterwards.
    /// </summary>
    public static class RoyalCampaignBlockerChecks
    {
        public static void VerifyBehavior(LevelManager level, Action<bool, string> check)
        {
            if (!RoyalReferenceCampaign.Active || !RoyalThrone.Testing.RoyalSave.Testing)
                throw new InvalidOperationException("Blocker checks require the isolated reference QA campaign.");
            int levelNumber = RoyalReferenceCampaign.Current.number;
            string tag = "Level " + levelNumber.ToString("00") + ": ";
            int Collected(string name) => level.targetObject.Where(t => t.referenceGoal == name).Sum(t => t.countCollected);
            int Remaining(string name) => level.targetObject.Where(t => t.referenceGoal == name).Sum(t => t.GetCount());
            Square Find(int code) => level.squaresArray.FirstOrDefault(s => s && RoyalReferenceCampaign.CodeAt(s) == code);

            foreach (int code in new[] { 21, 22, 23 })
            {
                var square = Find(code);
                if (!square || RoyalCampaignBlockers.HealthAt(square) <= 0) continue;
                int initial = RoyalCampaignBlockers.HealthAt(square), before = Collected("Box"), available = Remaining("Box");
                RoyalCampaignBlockers.BeginMatchWave(); square.DestroyBlock();
                check(RoyalCampaignBlockers.HealthAt(square) == initial - 1, tag + "box loses one layer from a direct Square hit");
                square.DestroyBlock();
                check(RoyalCampaignBlockers.HealthAt(square) == initial - 1, tag + "same-wave repeated contact cannot remove a second box layer");
                for (int hit = 1; hit < initial; hit++) { RoyalCampaignBlockers.BeginMatchWave(); square.DestroyBlock(); }
                check(square.CanGoInto() && RoyalCampaignBlockers.HealthAt(square) == 0, tag + "cleared box releases its cell for normal gravity/refill");
                check(Collected("Box") - before == Math.Min(1, available), tag + "all box layers credit exactly one box goal");
            }

            foreach (var spec in new[] { new { Code = 41, Goal = "Cupboard" }, new { Code = 43, Goal = "Potion" } })
            {
                var square = Find(spec.Code);
                if (!square || RoyalCampaignBlockers.HealthAt(square) <= 0) continue;
                var partner = level.GetSquare(square.col + 1, square.row + 1);
                int initial = RoyalCampaignBlockers.HealthAt(square), before = Collected(spec.Goal), available = Remaining(spec.Goal);
                check(RoyalCampaignBlockers.SharesObstacle(square, partner), tag + spec.Goal + " shares state across opposite footprint corners");
                RoyalCampaignBlockers.BeginMatchWave(); square.DestroyBlock(); partner.DestroyBlock();
                check(RoyalCampaignBlockers.HealthAt(square) == initial - 1, tag + spec.Goal + " receives one hit per wave across its 2×2 footprint");
                for (int hit = 1; hit < initial; hit++) { RoyalCampaignBlockers.BeginMatchWave(); partner.DestroyBlock(); }
                check(square.CanGoInto() && partner.CanGoInto() && !RoyalCampaignBlockers.SharesObstacle(square, partner), tag + spec.Goal + " releases all footprint cells after its last unit");
                check(Collected(spec.Goal) - before == Math.Min(initial, available), tag + spec.Goal + " credits each remaining authored goal unit once");
            }

            var grass = level.squaresArray.FirstOrDefault(s => s && RoyalCampaignBlockers.GrassAt(s) > 0);
            if (grass)
            {
                int initial = RoyalCampaignBlockers.GrassAt(grass), before = Collected("Grass"), available = Remaining("Grass");
                RoyalCampaignBlockers.BeginMatchWave(); grass.DestroyBlock(); grass.DestroyBlock();
                check(RoyalCampaignBlockers.GrassAt(grass) == initial - 1, tag + "grass loses one layer per contact wave");
                for (int hit = 1; hit < initial; hit++) { RoyalCampaignBlockers.BeginMatchWave(); grass.DestroyBlock(); }
                check(Collected("Grass") - before == Math.Min(1, available), tag + "grass layers credit exactly one grass cell");
            }

            var mailbox = Find(34);
            if (mailbox)
            {
                int before = Collected("Mail"), available = Remaining("Mail");
                RoyalCampaignBlockers.BeginMatchWave(); mailbox.DestroyBlock(); mailbox.DestroyBlock();
                check(Collected("Mail") - before == Math.Min(1, available), tag + "mailbox emits at most one mail per wave");
                check(!mailbox.CanGoInto() && RoyalCampaignBlockers.HealthAt(mailbox) == 1, tag + "mailbox stays fixed after emitting mail");
            }

            var special = level.squaresArray.Select(s => s ? s.item : null).FirstOrDefault(i => i && i.GetComponent<RoyalCampaignItem>() && !i.destroying);
            if (special)
            {
                string goal = RoyalReferenceCampaign.IsBonus ? "Coin" : "Egg";
                int before = Collected(goal), available = Remaining(goal);
                check(!special.square.CanGoOut() && special.square.FindMatchesAround().Count == 0,
                    tag + goal + " is excluded from swapping and color matches");
                special.DestroyItem(true); special.DestroyItem(true);
                check(special.destroying && Collected(goal) - before == Math.Min(1, available),
                    tag + goal + " credits once through the existing Item destruction path");
            }
        }
    }
}
