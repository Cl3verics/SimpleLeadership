using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SimpleLeadership;

[HotSwappable]
public class Dialog_SelectPawn : Window
{
    public override Vector2 InitialSize => new(620f, 500f);

    public List<Pawn> allPawns;
    private readonly Settlement selObject;
    private readonly bool isBaseLeader;
    private Vector2 scrollPosition;

    string searchKey;
    public Faction specificFaction;
    public XenotypeDef specificXenotype;

    public Dialog_SelectPawn(Settlement settlement, bool isBaseLeader)
    {
        doCloseButton = true;
        doCloseX = true;
        closeOnClickedOutside = false;
        absorbInputAroundWindow = false;
        allPawns = Find.WorldPawns.AllPawnsAlive.Where(pawn => pawn.MapHeld is null && pawn.story != null && pawn.Name != null).ToList();
        selObject = settlement;
        this.isBaseLeader = isBaseLeader;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;

        Text.Anchor = TextAnchor.MiddleLeft;
        var searchLabel = new Rect(inRect.x, inRect.y, 60, 24);
        Widgets.Label(searchLabel, "SL_Search".Translate());
        var searchRect = new Rect(searchLabel.xMax + 5, searchLabel.y, 200, 24f);
        searchKey = Widgets.TextField(searchRect, searchKey);
        Text.Anchor = TextAnchor.UpperLeft;

        var factionFilterLabel = "SL_FactionFilter".Translate(specificFaction?.Name ?? "None".Translate());
        var factionFilterWidth = Text.CalcSize(factionFilterLabel).x + 10;
        var factionButtonRect = new Rect(searchRect.xMax + 15, searchRect.y, factionFilterWidth, 24f);
        if (Widgets.ButtonText(factionButtonRect, factionFilterLabel))
        {
            var list = new List<FloatMenuOption>();
            var factions = allPawns.Select(x => x.Faction).Where(x => x != null).Distinct();
            foreach (var faction in factions)
            {
                list.Add(new FloatMenuOption(faction.Name, () => specificFaction = faction, iconTex: faction.def.FactionIcon, iconColor: faction.Color));
            }
            list.Add(new FloatMenuOption("None".Translate(), () => specificFaction = null));
            Find.WindowStack.Add(new FloatMenu(list));
        }

        var xenotypeFilterLabel = "SL_XenotypeFilter".Translate(specificXenotype?.LabelCap ?? "None".Translate());
        var xenotypeFilterWidth = Text.CalcSize(xenotypeFilterLabel).x + 10;
        var xenotypeButtonRect = new Rect(factionButtonRect.xMax + 15, searchRect.y, xenotypeFilterWidth, 24f);
        if (Widgets.ButtonText(xenotypeButtonRect, xenotypeFilterLabel))
        {
            var list = new List<FloatMenuOption>();
            var xenotypes = allPawns
                .Where(x => x.genes?.Xenotype != null)
                .Select(x => x.genes.Xenotype)
                .Distinct()
                .OrderBy(x => x.LabelCap.ToString());

            foreach (var xenotype in xenotypes)
            {
                list.Add(new FloatMenuOption(xenotype.LabelCap, () => specificXenotype = xenotype, iconTex: xenotype.Icon, iconColor: Color.white));
            }

            list.Add(new FloatMenuOption("None".Translate(), () => specificXenotype = null));

            Find.WindowStack.Add(new FloatMenu(list));
        }
        var outRect = new Rect(inRect);
        outRect.y = searchRect.yMax + 5;
        outRect.yMax -= 70f;
        outRect.width -= 16f;

        var pawns = searchKey.NullOrEmpty() ? allPawns : allPawns.Where(x => x.Name.ToStringFull.ToLower().Contains(searchKey.ToLower())).ToList();
        if (specificFaction != null)
        {
            pawns = pawns.Where(x => x.Faction == specificFaction).ToList();
        }
        if (specificXenotype != null)
        {
            pawns = pawns.Where(x => x.genes?.Xenotype == specificXenotype).ToList();
        }
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, pawns.Count * 35f);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        try
        {
            var num = 0f;
            foreach (var pawn in pawns.OrderBy(x => x.Name.ToStringFull))
            {
                Widgets.InfoCardButton(0, num, pawn);
                var iconRect = new Rect(24, num, 24, 24);
                Widgets.ThingIcon(iconRect, pawn);
                iconRect.x += 24;
                if (pawn.Faction != null)
                {
                    FactionUIUtility.DrawFactionIconWithTooltip(iconRect, pawn.Faction);
                    iconRect.x += 24;
                }
                if (pawn.genes?.Xenotype != null)
                {
                    GUI.DrawTexture(iconRect, pawn.genes.Xenotype.Icon);
                    TooltipHandler.TipRegion(iconRect, pawn.genes.Xenotype.LabelCap);
                    iconRect.x += 24;
                }
                var rect = new Rect(iconRect.xMax + 5, num, viewRect.width * 0.55f, 32f);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(rect, pawn.Name.ToStringFull);
                Text.Anchor = TextAnchor.UpperLeft;
                rect.x = rect.xMax + 10;
                rect.width = 100;
                if (Widgets.ButtonText(rect, "SL_Select".Translate()))
                {
                    var handleOldLeader = (Pawn leader) =>
                    {
                        if (leader == null)
                            return;

                        var pawnFaction = pawn.Faction;
                        if (pawnFaction?.leader == pawn)
                        {
                            pawnFaction.leader = leader;
                            Utils.EnsureRoyalLeaderStatus(pawnFaction, leader);
                        }
                        else
                        {
                            var data = WorldComponent_LeaderTracker.Instance
                                .GetLeadershipDataFor(selObject.Faction);
                            if (data?.settlementLeaders.GetValueOrDefault(selObject) == pawn)
                            {
                                WorldComponent_LeaderTracker.Instance.ReplaceBaseLeader(selObject, leader);
                            }
                        }
                        leader.SetFactionDirect(pawnFaction);
                    };

                    if (isBaseLeader is false)
                    {
                        var oldLeader = selObject.Faction.leader;
                        handleOldLeader(oldLeader);

                        selObject.Faction.leader = pawn;
                        Utils.EnsureRoyalLeaderStatus(selObject.Faction, pawn);
                        if (oldLeader != null && oldLeader != pawn)
                            WorldComponent_LeaderTracker.Instance.Notify_LeaderLost(selObject.Faction, oldLeader);
                    }
                    else
                    {
                        var settlementLeaders = WorldComponent_LeaderTracker.Instance
                            .GetLeadershipDataFor(selObject.Faction)
                            ?.settlementLeaders;
                        var oldLeader = settlementLeaders?[selObject];
                        handleOldLeader(oldLeader);

                        if (settlementLeaders != null)
                        {
                            WorldComponent_LeaderTracker.Instance.ReplaceBaseLeader(selObject, pawn);
                        }
                    }
                    pawn.SetFactionDirect(selObject.Faction);

                    SoundDefOf.Click.PlayOneShotOnCamera();
                    Close();
                }
                num += 35f;
            }
        }
        finally
        {
            Widgets.EndScrollView();
        }
    }
}