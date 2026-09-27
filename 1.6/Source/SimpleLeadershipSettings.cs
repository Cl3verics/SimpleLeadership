using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace SimpleLeadership
{
    public class SimpleLeadershipSettings : ModSettings
    {
        [Header("SL_General")]
        [Label("SL_EnableAlerts")]
        public bool enableAlerts = true;

        [Label("SL_EnableEvents")]
        public bool enableEvents = true;

        [Label("SL_DistanceWeight")]
        [Percentage]
        public float distanceWeight = 0.6f;

        [Label("SL_LeaderSpawnChance")]
        [Percentage]
        public float leaderSpawnChance = 0.02f;

        [Label("SL_BasesPerLeader")]
        [Range(1, 20)]
        [Step(1f)]
        public int basesPerLeader = 5;

        [Header("SL_DoctrinesSettings")]
        [Label("SL_MaxDoctrinesPerFaction")]
        [Range(1, 6)]
        [Step(1f)]
        public int maxDoctrinesPerFaction = 3;

        [Label("SL_InvestigationDurationDays")]
        [Range(1, 30)]
        [Step(1f)]
        public int investigationDurationDays = 8;

        [Label("SL_AbsoluteBordersCooldownDays")]
        [Range(1, 60)]
        [Step(1f)]
        public int absoluteBordersCooldownDays = 15;

        [Label("SL_LeaderLegacyDurationMultiplier")]
        [Percentage]
        public float leaderLegacyDurationMultiplier = 0.5f;

        [Label("SL_InterventionRaidChance")]
        [Percentage]
        public float interventionRaidChance = 0.35f;

        [Label("SL_InterventionRaidDelayHours")]
        [Range(0.1f, 12f)]
        [Step(0.1f)]
        public float interventionRaidDelayHours = 0.8f;

        [Label("SL_InterventionRaidCooldownDays")]
        [Range(1f, 30f)]
        [Step(1f)]
        public float interventionRaidCooldownDays = 1f;

        [Label("SL_MutualDefenseChance")]
        [Percentage]
        public float mutualDefenseRetaliationChance = 0.4f;

        [Label("SL_MutualDefenseDelayHours")]
        [Range(0.1f, 12f)]
        [Step(0.1f)]
        public float mutualDefenseRetaliationDelayHours = 1.5f;

        [Label("SL_MutualDefenseCooldownDays")]
        [Range(1f, 30f)]
        [Step(1f)]
        public float mutualDefenseCooldownDays = 5f;

        [Label("SL_ForcesPreservationMultiplier")]
        [Percentage]
        public float forcesPreservationRetreatMultiplier = 0.5f;

        [Label("SL_HitAndRunCasualtyThreshold")]
        [Percentage]
        public float hitAndRunCasualtyThreshold = 0.2f;

        [Label("SL_HitAndRunRegroupPointMultiplier")]
        [Percentage]
        public float hitAndRunRegroupPointMultiplier = 0.8f;

        [Label("SL_HitAndRunRegroupDelayHours")]
        [Range(1f, 24f)]
        [Step(0.5f)]
        public float hitAndRunRegroupDelayHours = 6f;

        [Label("SL_HitAndRunLockoutDays")]
        [Range(1f, 15f)]
        [Step(1f)]
        public float hitAndRunLockoutDays = 5f;

        [Label("SL_AttritionSiegeChance")]
        [Percentage]
        public float attritionSiegeChance = 0.75f;

        [Label("SL_NoMercyExecutionRange")]
        [Range(5f, 30f)]
        [Step(1f)]
        public float noMercyExecutionRange = 15f;

        [Label("SL_ScorchedEarthMaxFires")]
        [Range(1, 30)]
        [Step(1f)]
        public int scorchedEarthMaxFires = 10;

        [Label("SL_ScorchedEarthCasualtyThreshold")]
        [Percentage]
        public float scorchedEarthCasualtyThreshold = 0.5f;

        [Label("SL_GraciousRepatriationMultiplier")]
        [Percentage]
        public float graciousRepatriationGoodwillMultiplier = 0.5f;

        [Label("SL_FallenVenerationCorpseGoodwill")]
        [Range(1, 10)]
        [Step(1f)]
        public int fallenVenerationCorpseGoodwill = 2;

        [Header("SL_Blacklist")]
        [Description("SL_BlacklistDesc")]
        [DrawMethod("DrawFactionBlacklist", SerializeField = false)]
        [SettingOptions(drawValue: false, showDefaultValue: false)]
        public List<string> factionBlacklist = [];

        public override void ExposeData()
        {
            base.ExposeData();
            SimpleSettings.AutoExpose(this);
            Scribe_Collections.Look(ref factionBlacklist, "factionBlacklist", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
                factionBlacklist ??= [];
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            SimpleSettings.DrawWindow(this, inRect);
        }

        public float DrawFactionBlacklist(ModSettings _, SimpleSettings.MemberWrapper member, Rect area)
        {
            var defs = DefDatabase<FactionDef>.AllDefs
                .Where(d => d.humanlikeFaction && !d.isPlayer)
                .OrderBy(d => d.label)
                .ToList();

            const float RowHeight = 28f;
            float height = 0f;
            foreach (var def in defs)
            {
                bool blacklisted = factionBlacklist.Contains(def.defName);
                bool prev = blacklisted;
                Widgets.CheckboxLabeled(new Rect(area.x, area.y + height, area.width, RowHeight), def.label.CapitalizeFirst(), ref blacklisted);
                if (prev != blacklisted)
                {
                    if (blacklisted) factionBlacklist.Add(def.defName);
                    else factionBlacklist.Remove(def.defName);
                }
                height += RowHeight;
            }
            return height;
        }
    }
}
