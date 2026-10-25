using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SimpleLeadership
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class WITab_FactionLeadership : WITab
    {
        private const float ColumnSpacing = 10f;
        private const float SectionSpacing = 10f;
        private const float TitleHeight = 30f;
        private const float PortraitSize = 128f;
        private const float InfoRowHeight = 22f;
        private const float EventButtonHeight = 40f;
        private const float TabHeight = 30f;
        private const float TabTopMargin = 8f;
        private const float WindowWidth = 590f;
        private const float DoctrinesListWidth = 265f;

        private static readonly Texture2D UnknownLeaderIcon = ContentFinder<Texture2D>.Get("UI/Overlays/QuestionMark");
        private static readonly Color PowerEventBoxColor = new(0.32f, 0.38f, 0.22f);
        private static readonly Color PowerEventTitleColor = new(0.9f, 0.85f, 0.2f);

        private enum TabMode { Leaders, Doctrines }
        private static TabMode curTab = TabMode.Leaders;
        public static FactionDoctrine selectedDoctrine;
        private Vector2 doctrineScroll;
        private Vector2 basesScroll;

        public override bool IsVisible => SelObject is Settlement settlement && settlement.Faction != Faction.OfPlayer;

        public WITab_FactionLeadership()
        {
            labelKey = "SL_Leaders";
        }

        public override void FillTab()
        {
            var selectedSettlement = (Settlement)SelObject;

            var faction = selectedSettlement.Faction;
            var leaderTracker = WorldComponent_LeaderTracker.Instance;

            var factionEventsCount = faction.GetActiveEvents<PowerEventBase>().Count();
            var settlementEventsCount = selectedSettlement.GetActiveEvents<PowerEventBase>().Count();
            var maxEvents = Mathf.Max(factionEventsCount, settlementEventsCount);
            var eventsSectionHeight = maxEvents == 0 ? 26f : maxEvents * 45f;

            var hasDoctrines = SimpleLeadershipMod.Settings.enableDoctrines;
            if (hasDoctrines is false)
                curTab = TabMode.Leaders;

            var bannerHeight = hasDoctrines ? GetBannerHeight(faction, WindowWidth - 20f) : 0f;
            var targetHeight = curTab == TabMode.Leaders
                ? (hasDoctrines ? TabTopMargin + TabHeight + bannerHeight + 4f + 8f : 0f) + 303f + eventsSectionHeight
                : 450f;
            size = new Vector2(WindowWidth, Mathf.Min(targetHeight, UI.screenHeight - 80f));

            Widgets.DrawWindowBackground(new Rect(0f, 0f, size.x, size.y));

            var tabTop = hasDoctrines ? TabTopMargin + TabHeight : 0f;
            var tabBaseRect = new Rect(0f, tabTop, size.x, size.y - tabTop);
            var contentRect = tabBaseRect.ContractedBy(10f);

            if (hasDoctrines)
            {
                var tabs = new List<TabRecord>
                {
                    new TabRecord("SL_LeadersTab".Translate(), () => curTab = TabMode.Leaders, curTab == TabMode.Leaders),
                    new TabRecord("SL_DoctrinesTab".Translate(), () => curTab = TabMode.Doctrines, curTab == TabMode.Doctrines)
                };
                TabDrawer.DrawTabs(tabBaseRect, tabs);
            }

            if (curTab == TabMode.Doctrines && hasDoctrines)
            {
                FillDoctrinesTab(contentRect, faction, leaderTracker);
            }
            else
            {
                var curY = contentRect.y;
                if (hasDoctrines)
                {
                    DrawDoctrinesBanner(new Rect(contentRect.x, curY, contentRect.width, bannerHeight), faction);
                    curY += bannerHeight + 4f;

                    Widgets.DrawLineHorizontal(contentRect.x, curY, contentRect.width, Color.gray);
                    curY += 8f;
                }

                var columnWidth = (contentRect.width - ColumnSpacing) / 2f;
                var colHeight = contentRect.yMax - curY;

                var leftColumnRect = new Rect(contentRect.x, curY, columnWidth, colHeight);
                var rightColumnRect = new Rect(contentRect.x + columnWidth + ColumnSpacing, curY, columnWidth, colHeight);

                var factionLeader = faction.leader;
                var factionLeaderLocation = GetLeaderLocationText(factionLeader);
                DrawLeadershipColumn(leftColumnRect, "SL_FactionLeadership", factionLeader, factionLeaderLocation, faction, selectedSettlement, true);

                var baseLeader = leaderTracker.GetBaseLeader(selectedSettlement);
                var baseLeaderLocation = GetLeaderLocationText(baseLeader);
                DrawLeadershipColumn(rightColumnRect, "SL_BaseLeadership", baseLeader, baseLeaderLocation, selectedSettlement.Faction, selectedSettlement, false);

                Widgets.DrawBoxSolid(new Rect(contentRect.x + columnWidth + ColumnSpacing / 2f, curY, 1f, colHeight), Color.grey);
            }
        }

        private float GetBannerHeight(Faction faction, float width)
        {
            var doctrines = faction.GetDoctrines();
            Text.Font = GameFont.Tiny;
            var subheaderHeight = Text.CalcHeight("SL_DoctrinesSubheader".Translate(), width - 16f);
            var headerBlockHeight = 24f + subheaderHeight + 14f;
            if (doctrines.Count == 0)
                return headerBlockHeight + 22f;
            return headerBlockHeight + doctrines.Count * 25f;
        }

        private void FillDoctrinesTab(Rect rect, Faction faction, WorldComponent_LeaderTracker tracker)
        {
            var doctrines = faction.GetDoctrines();
            if (doctrines.Count == 0)
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(rect.x, rect.y - 5, rect.width, 32f), "SL_DoctrinesHeader".Translate());

                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                var subHeight = Text.CalcHeight("SL_DoctrinesSubheader".Translate(), rect.width);
                Widgets.Label(new Rect(rect.x, rect.y + 26f, rect.width, subHeight), "SL_DoctrinesSubheader".Translate());
                GUI.color = Color.white;

                var emptyBoxRect = new Rect(rect.x + 20f, rect.y + 60f + subHeight, rect.width - 40f, 60f);
                Widgets.DrawBoxSolid(emptyBoxRect, new Color(0.12f, 0.12f, 0.12f));
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.gray;
                Widgets.Label(emptyBoxRect, "SL_NoDoctrines".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            else
            {
                var leftRect = new Rect(rect.x, rect.y, DoctrinesListWidth, rect.height);
                var rightRect = new Rect(rect.x + DoctrinesListWidth + 14f, rect.y, rect.width - DoctrinesListWidth - 14f, rect.height);

                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(leftRect.x, leftRect.y, leftRect.width, 26f), "SL_DoctrinesHeader".Translate());

                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                var subheaderHeight = Text.CalcHeight("SL_DoctrinesSubheader".Translate(), leftRect.width);
                Widgets.Label(new Rect(leftRect.x, leftRect.y + 26f, leftRect.width, subheaderHeight), "SL_DoctrinesSubheader".Translate());
                GUI.color = Color.white;

                var listStartY = leftRect.y + 26f + subheaderHeight + 6f;
                var listRect = new Rect(leftRect.x, listStartY, leftRect.width, leftRect.yMax - listStartY);
                if (selectedDoctrine == null || doctrines.Contains(selectedDoctrine) is false)
                    selectedDoctrine = doctrines.First();

                var curY = 0f;
                var viewRect = new Rect(0f, 0f, listRect.width - 16f, doctrines.Count * 44f);
                Widgets.BeginScrollView(listRect, ref doctrineScroll, viewRect);
                foreach (var doc in doctrines)
                {
                    var rowRect = new Rect(0f, curY, viewRect.width, 42f);
                    var rowColor = doc.def.categoryColor * 0.25f;
                    rowColor.a = 0.5f;
                    Widgets.DrawBoxSolid(rowRect, rowColor);
                    if (selectedDoctrine == doc)
                        Widgets.DrawHighlightSelected(rowRect);
                    Widgets.DrawHighlightIfMouseover(rowRect);

                    var textRect = new Rect(rowRect.x + 6f, rowRect.y + 2f, rowRect.width - 12f, 20f);
                    Text.Font = GameFont.Small;
                    GUI.color = doc.def.categoryColor;
                    Widgets.Label(textRect, doc.def.label.CapitalizeFirst());
                    GUI.color = Color.white;

                    var subRect = new Rect(rowRect.x + 6f, rowRect.y + 22f, rowRect.width - 12f, 18f);
                    Text.Font = GameFont.Tiny;
                    var placer = doc.isInvestigated && doc.proposedBy != null
                        ? "SL_PlacedBy".Translate(doc.proposedBy.LabelShortCap)
                        : "SL_PlacedByUnknown".Translate();
                    Widgets.Label(subRect, placer);

                    if (Mouse.IsOver(rowRect))
                        DrawDoctrineWindow(doc);
                    if (Widgets.ButtonInvisible(rowRect))
                        selectedDoctrine = doc;
                    curY += 44f;
                }
                Widgets.EndScrollView();

                DrawDoctrineDetails(rightRect, selectedDoctrine, faction, tracker);
            }
        }

        private void DrawDoctrineDetails(Rect rect, FactionDoctrine doc, Faction faction, WorldComponent_LeaderTracker tracker)
        {
            var curY = rect.y;

            Text.Font = GameFont.Medium;
            var titleText = doc.def.label.CapitalizeFirst();
            var titleHeight = Text.CalcHeight(titleText, rect.width);
            GUI.color = doc.def.categoryColor;
            Widgets.Label(new Rect(rect.x, curY, rect.width, titleHeight), titleText);
            GUI.color = Color.white;
            curY += titleHeight + 2f;

            Text.Font = GameFont.Tiny;
            var placer = doc.isInvestigated && doc.proposedBy != null
                ? "SL_PlacedBy".Translate(doc.proposedBy.LabelCap)
                : "SL_PlacedByUnknown".Translate();
            Widgets.Label(new Rect(rect.x, curY, rect.width, 18f), placer);
            curY += 20f;
            if (doc.isInvestigated && doc.proposedBy != null)
            {
                var profileCardRect = new Rect(rect.x, curY, rect.width, 105f);
                Widgets.DrawBoxSolid(profileCardRect, new Color(0.12f, 0.12f, 0.12f));
                var portraitRect = new Rect(profileCardRect.x + 8f, profileCardRect.y + 8f, 88f, 88f);

                GUI.DrawTexture(portraitRect, PortraitsCache.Get(doc.proposedBy, new Vector2(90f, 90f), Rot4.South));
                Widgets.InfoCardButton(portraitRect.xMax - 20f, portraitRect.yMax - 20f, doc.proposedBy);

                var infoX = portraitRect.xMax + 12f;
                var infoWidth = profileCardRect.width - portraitRect.width - 24f;

                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(infoX, profileCardRect.y + 10f, infoWidth, 22f), doc.proposedBy.Name.ToStringFull);

                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                string roleText = doc.proposedBy == faction.leader ? faction.LeaderTitle : "SL_BaseLeadership".Translate();
                Widgets.Label(new Rect(infoX, profileCardRect.y + 34f, infoWidth, 18f), roleText);
                Widgets.Label(new Rect(infoX, profileCardRect.y + 52f, infoWidth, 18f), "SL_LocationInfo".Translate(GetLeaderLocationText(doc.proposedBy)));
                GUI.color = Color.white;

                curY += profileCardRect.height + 10f;

                var bases = tracker.GetSettlementsOfBaseLeader(doc.proposedBy).ToList();
                if (bases.Count == 0 && doc.proposedBy == faction.leader)
                    bases = Find.WorldObjects.Settlements.Where(s => s.Faction == faction && s.Tile.Valid).ToList();

                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(rect.x, curY, rect.width, 22f), "SL_BasesUnderLeader".Translate());
                curY += 24f;

                var baseAreaRect = new Rect(rect.x, curY, rect.width, rect.yMax - curY);
                var baseViewRect = new Rect(0f, 0f, baseAreaRect.width - 16f, bases.Count * 28f);
                Widgets.BeginScrollView(baseAreaRect, ref basesScroll, baseViewRect);
                var baseCurY = 0f;
                foreach (var b in bases)
                {
                    var jumpBtn = new Rect(0f, baseCurY, baseViewRect.width, 25f);
                    if (Widgets.ButtonText(jumpBtn, "SL_JumpToBaseBtn".Translate(b.LabelShort)))
                        CameraJumper.TryJumpAndSelect(b);
                    baseCurY += 28f;
                }
                Widgets.EndScrollView();
            }
            else
            {
                var cardRect = new Rect(rect.x, curY, rect.width, 115f);
                Widgets.DrawBoxSolid(cardRect, new Color(0.12f, 0.12f, 0.12f));
                var portraitRect = new Rect(cardRect.x + 10f, cardRect.y + 12f, 90f, 90f);

                GUI.DrawTexture(portraitRect, UnknownLeaderIcon);
                var infoRect = new Rect(portraitRect.xMax + 10f, cardRect.y, cardRect.width - portraitRect.width - 20f, cardRect.height);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(infoRect, "SL_UnknownProposerDesc".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                curY += cardRect.height + 10f;

                if (tracker.IsInvestigationActive(faction, doc))
                {
                    var remainingTicks = Mathf.Max(0, tracker.investigationEndTick - Find.TickManager.TicksGame);
                    var cancelBtn = new Rect(rect.x, curY, rect.width, 32f);
                    if (Widgets.ButtonText(cancelBtn, "SL_CancelInvestigation".Translate()))
                        tracker.CancelInvestigation();
                    curY += 36f;
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.cyan;
                    Widgets.Label(new Rect(rect.x, curY, rect.width, 18f), "SL_ConcludesIn".Translate(remainingTicks.ToStringTicksToPeriod().Colorize(ColorLibrary.SkyBlue)));
                    curY += 18f;
                    GUI.color = Color.gray;
                    Widgets.Label(new Rect(rect.x, curY, rect.width, 36f), "SL_InvestigationInProgressWarning".Translate());
                    GUI.color = Color.white;
                }
                else if (doc.isInvestigated is false)
                {
                    var invBtn = new Rect(rect.x, curY, rect.width, 32f);
                    if (Widgets.ButtonText(invBtn, "SL_InvestigateBtn".Translate()))
                    {
                        if (tracker.activeInvestigation != null)
                        {
                            Messages.Message("SL_AlreadyInvestigatingWarning".Translate(tracker.activeInvestigation.label, tracker.investigatingFaction.NameColored), MessageTypeDefOf.RejectInput);
                            tracker.JumpToCurrentInvestigation();
                            InspectPaneUtility.OpenTab(typeof(WITab_FactionLeadership));
                            curTab = TabMode.Doctrines;
                            selectedDoctrine = tracker.investigatingFaction.GetDoctrines().FirstOrDefault(d => d.def == tracker.activeInvestigation);
                        }
                        else
                        {
                            tracker.StartInvestigation(faction, doc);
                        }
                    }
                    curY += 36f;
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    var daysText = SimpleLeadershipMod.Settings.investigationDurationDays.ToString().Colorize(ColorLibrary.SkyBlue);
                    Widgets.Label(new Rect(rect.x, curY, rect.width, 36f), "SL_InvestigateNotice".Translate(daysText));
                    GUI.color = Color.white;
                }
            }
        }

        private void DrawDoctrinesBanner(Rect rect, Faction faction)
        {
            var innerRect = rect;
            var curY = innerRect.y;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(innerRect.x, curY - 5, innerRect.width, 32f), "SL_DoctrinesHeader".Translate());
            curY += 24f;

            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            var subheaderHeight = Text.CalcHeight("SL_DoctrinesSubheader".Translate(), innerRect.width);
            Widgets.Label(new Rect(innerRect.x, curY, innerRect.width, subheaderHeight), "SL_DoctrinesSubheader".Translate());
            GUI.color = Color.white;
            curY += subheaderHeight + 6f;

            var doctrines = faction.GetDoctrines();
            if (doctrines.Count == 0)
            {
                Text.Font = GameFont.Small;
                GUI.color = Color.gray;
                Widgets.Label(new Rect(innerRect.x, curY, innerRect.width, 22f), "SL_NoDoctrines".Translate());
                GUI.color = Color.white;
            }
            else
            {
                const float barHeight = 22f;
                foreach (var doc in doctrines)
                {
                    var barRect = new Rect(innerRect.x, curY, innerRect.width, barHeight);
                    var fillColor = doc.def.categoryColor;
                    fillColor.a = 0.25f;
                    Widgets.DrawBoxSolid(barRect, fillColor);
                    Widgets.DrawHighlightIfMouseover(barRect);

                    GUI.color = doc.def.categoryColor;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(barRect.ContractedBy(6f, 0f), doc.def.ShortLabel.CapitalizeFirst());
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = Color.white;
                    if (Mouse.IsOver(barRect))
                        DrawDoctrineWindow(doc);
                    curY += barHeight + 3f;
                }
            }
        }

        private void DrawDoctrineWindow(FactionDoctrine doc)
        {
            const float width = 320f;
            const float padding = 10f;

            float contentHeight = 0;

            Text.Font = GameFont.Medium;
            contentHeight += 35f;

            Text.Font = GameFont.Small;
            contentHeight += Text.CalcHeight(doc.def.description, width - (padding * 2)) + 10f;

            foreach (var effect in doc.def.effects)
            {
                contentHeight += Text.CalcHeight(effect, width - (padding * 2) - 10f) + 15f;
            }

            var height = contentHeight + (padding * 2);

            var mousePosition = UI.MousePositionOnUIInverted;
            var winRect = new Rect(mousePosition.x + 12, mousePosition.y + 12, width, height);

            if (winRect.yMax > UI.screenHeight)
            {
                winRect.y = UI.screenHeight - winRect.height;
            }

            Find.WindowStack.ImmediateWindow(15937564 + doc.GetHashCode(), winRect, WindowLayer.Super, () =>
            {
                var r = winRect.AtZero().ContractedBy(padding);
                var curY = r.y;

                GUI.color = PowerEventTitleColor;
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(r.x, curY, r.width, 30f), doc.def.LabelCap);
                curY += 35;
                GUI.color = Color.white;

                Text.Font = GameFont.Small;
                var descHeight = Text.CalcHeight(doc.def.description, r.width);
                Widgets.Label(new Rect(r.x, curY, r.width, descHeight), doc.def.description);
                curY += descHeight + 10f;

                foreach (var effect in doc.def.effects)
                {
                    var effectTextHeight = Text.CalcHeight(effect, r.width - 10f);
                    var effectRect = new Rect(r.x, curY, r.width, effectTextHeight + 10f);
                    Widgets.DrawBoxSolid(effectRect, PowerEventBoxColor);

                    var textRect = effectRect.ContractedBy(5f);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(textRect, effect);
                    Text.Anchor = TextAnchor.UpperLeft;

                    curY += effectRect.height + 5f;
                }
            });
        }

        private void DrawLeadershipColumn(Rect rect, string titleKey, Pawn leader, string locationText, Faction faction, Settlement settlement, bool isLeft)
        {
            var curY = rect.y;

            if (DebugSettings.godMode && Widgets.ButtonImage(new Rect(rect.xMax - 24f, curY, 20f, 20f), TexButton.Paste))
            {
                Find.WindowStack.Add(new Dialog_SelectPawn(settlement, isLeft is false));
            }

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            var titleRect = new Rect(rect.x, curY, rect.width, TitleHeight);
            Widgets.Label(titleRect, titleKey.Translate());
            curY += TitleHeight + SectionSpacing;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            DrawLeaderInfo(new Rect(rect.x, curY, rect.width, PortraitSize), leader);
            curY += PortraitSize + SectionSpacing;

            var leaderName = leader != null ? leader.Name.ToStringFull : "SL_NotAvailable".Translate().ToString();
            DrawInfoRow(ref curY, rect, "SL_LeaderName".Translate(), leaderName);
            DrawInfoRow(ref curY, rect, "SL_Location".Translate(), locationText);

            if (leader != null && leader.Dead is false)
            {
                var spawnChance = Utils.CalculateSpawnChance(leader, faction, settlement);
                DrawSpawnChance(ref curY, rect, spawnChance);
            }
            else
            {
                curY += InfoRowHeight;
            }

            Widgets.DrawLineHorizontal(isLeft ? 0f : size.x / 2f, curY, size.x / 2f, Color.gray);
            curY += SectionSpacing;

            Widgets.Label(new Rect(rect.x, curY, rect.width, InfoRowHeight), "SL_CurrentEvents".Translate());
            curY += InfoRowHeight;

            DrawEvents(new Rect(rect.x, curY, rect.width, EventButtonHeight), faction, settlement, isLeft);
        }

        private string GetLeaderLocationText(Pawn leader)
        {
            if (leader != null && leader.Spawned && leader.Map?.Parent is WorldObject worldObject)
            {
                return worldObject.LabelCap;
            }
            return "SL_NotAvailable".Translate();
        }

        private void DrawLeaderInfo(Rect rect, Pawn leader)
        {
            var portraitRect = new Rect(rect.center.x - PortraitSize / 2f, rect.y, PortraitSize, PortraitSize);
            if (leader != null)
            {
                GUI.DrawTexture(portraitRect, PortraitsCache.Get(leader, new Vector2(PortraitSize, PortraitSize), Rot4.South));
                Widgets.InfoCardButton(portraitRect.xMax, portraitRect.yMax - 24f, leader);
                TooltipHandler.TipRegion(portraitRect, leader.Name.ToStringFull);
            }
            else
            {
                GUI.DrawTexture(portraitRect, UnknownLeaderIcon);
            }
        }

        private void DrawInfoRow(ref float curY, Rect container, string label, string value)
        {
            var rowRect = new Rect(container.x, curY, container.width, InfoRowHeight);
            Widgets.Label(rowRect, label);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rowRect, value);
            Text.Anchor = TextAnchor.UpperLeft;
            curY += InfoRowHeight;
        }

        private void DrawSpawnChance(ref float curY, Rect container, float spawnChance)
        {
            var rowRect = new Rect(container.x, curY, container.width, InfoRowHeight);
            Widgets.Label(rowRect, "SL_EncounterChance".Translate());

            Text.Anchor = TextAnchor.MiddleRight;
            var originalColor = GUI.color;
            var chancePercent = spawnChance * 100f;
            var chanceText = chancePercent.ToString("0") + "%";

            if (chancePercent < 40f)
            {
                GUI.color = Color.red;
            }
            else if (chancePercent < 70f)
            {
                GUI.color = Color.yellow;
            }
            else
            {
                GUI.color = Color.green;
            }

            Widgets.Label(rowRect, chanceText);
            GUI.color = originalColor;
            Text.Anchor = TextAnchor.UpperLeft;
            curY += InfoRowHeight;
        }

        private void DrawEvents(Rect rect, Faction faction, Settlement settlement, bool isFactionColumn)
        {
            var events = isFactionColumn ? faction.GetActiveEvents<PowerEventBase>() : settlement.GetActiveEvents<PowerEventBase>();

            if (events.Any())
            {
                var currentY = rect.y;
                foreach (var powerEvent in events)
                {
                    var iconSize = 30f;
                    var padding = 5f;
                    var textWidth = rect.width - iconSize - (padding * 3);

                    Text.Font = GameFont.Small;
                    var textHeight = Text.CalcHeight(powerEvent.def.LabelCap, textWidth);
                    var eventHeight = Mathf.Max(iconSize + (padding * 2), textHeight + (padding * 2));

                    var eventRect = new Rect(rect.x, currentY, rect.width, eventHeight);
                    Widgets.DrawBoxSolid(eventRect, new Color(0.15f, 0.15f, 0.15f));
                    Widgets.DrawHighlightIfMouseover(eventRect);

                    var iconRect = new Rect(eventRect.x + padding, eventRect.y + padding, iconSize, iconSize);
                    GUI.DrawTexture(iconRect, powerEvent.def.Icon);

                    Text.Anchor = TextAnchor.MiddleLeft;
                    var textRect = new Rect(iconRect.xMax + padding, eventRect.y + padding, textWidth, eventHeight - (padding * 2));
                    Widgets.Label(textRect, powerEvent.def.LabelCap);
                    Text.Anchor = TextAnchor.UpperLeft;

                    if (Mouse.IsOver(eventRect))
                    {
                        DrawPowerEventWindow(powerEvent);
                    }
                    currentY += eventHeight + 5f;
                }
            }
            else
            {
                Widgets.Label(new Rect(rect.x + 10f, rect.y, rect.width - 10f, InfoRowHeight), "SL_None".Translate());
            }
        }

        private void DrawPowerEventWindow(PowerEventBase powerEvent)
        {
            const float width = 320f;
            const float padding = 10f;

            float contentHeight = 0;

            Text.Font = GameFont.Medium;
            contentHeight += 35f;

            Text.Font = GameFont.Small;
            contentHeight += Text.CalcHeight(powerEvent.def.description, width - (padding * 2)) + 10f;

            foreach (var effect in powerEvent.def.effects)
            {
                contentHeight += Text.CalcHeight(effect, width - (padding * 2) - 10f) + 10f + 5f;
            }

            contentHeight += 25f;

            var height = contentHeight + (padding * 2);

            var mousePosition = UI.MousePositionOnUIInverted;
            var winRect = new Rect(mousePosition.x + 12, mousePosition.y + 12, width, height);

            if (winRect.yMax > UI.screenHeight)
            {
                winRect.y = UI.screenHeight - winRect.height;
            }

            Find.WindowStack.ImmediateWindow(15937564 + powerEvent.GetHashCode(), winRect, WindowLayer.Super, () =>
            {
                var r = winRect.AtZero().ContractedBy(padding);
                var curY = r.y;

                GUI.color = PowerEventTitleColor;
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(r.x, curY, r.width, 30f), powerEvent.def.LabelCap);
                curY += 35;
                GUI.color = Color.white;

                Text.Font = GameFont.Small;
                var descHeight = Text.CalcHeight(powerEvent.def.description, r.width);
                Widgets.Label(new Rect(r.x, curY, r.width, descHeight), powerEvent.def.description);
                curY += descHeight + 10f;

                foreach (var effect in powerEvent.def.effects)
                {
                    var effectTextHeight = Text.CalcHeight(effect, r.width - 10f);
                    var effectRect = new Rect(r.x, curY, r.width, effectTextHeight + 10f);
                    Widgets.DrawBoxSolid(effectRect, PowerEventBoxColor);

                    var textRect = effectRect.ContractedBy(5f);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(textRect, effect);
                    Text.Anchor = TextAnchor.UpperLeft;

                    curY += effectRect.height + 5f;
                }

                curY += 5f;
                GUI.color = Color.gray;
                var ticksLeft = powerEvent.EndTick - Find.TickManager.TicksGame;
                var expiresIn = "ExpiresIn".Translate().CapitalizeFirst() + " " + ticksLeft.ToStringTicksToPeriod();
                Widgets.Label(new Rect(r.x, curY, r.width, 20f), expiresIn);
                GUI.color = Color.white;
            });
        }
    }
}
