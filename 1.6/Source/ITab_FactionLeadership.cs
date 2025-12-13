using System;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

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
        private float ColumnSpacing => 10f;
        private float SectionSpacing => 10f;
        private float TitleHeight => 30f;
        private float PortraitSize => 128f;
        private float InfoRowHeight => 22f;
        private float EventButtonHeight => 40f;

        private static readonly Texture2D UnknownLeaderIcon = ContentFinder<Texture2D>.Get("UI/Overlays/QuestionMark");
        private static readonly Color PowerEventBoxColor = new Color(0.32f, 0.38f, 0.22f);
        private static readonly Color PowerEventTitleColor = new Color(0.9f, 0.85f, 0.2f);

        public WITab_FactionLeadership()
        {
            size = new Vector2(520f, 300f);
            labelKey = "SL_Leaders";
        }

        public override void FillTab()
        {
            Rect mainRect = new Rect(0f, 0f, size.x, size.y);
            Widgets.DrawWindowBackground(mainRect);

            Settlement selectedSettlement = SelObject as Settlement;
            if (selectedSettlement == null)
            {
                return;
            }

            Faction faction = selectedSettlement.Faction;
            WorldComponent_LeaderTracker leaderTracker = WorldComponent_LeaderTracker.Instance;

            float columnWidth = (mainRect.width - ColumnSpacing) / 2f;

            Rect leftColumnRect = new Rect(mainRect.x, mainRect.y, columnWidth, mainRect.height).ContractedBy(10f);
            Rect rightColumnRect = new Rect(leftColumnRect.xMax + ColumnSpacing, mainRect.y, columnWidth, mainRect.height).ContractedBy(10f);

            Pawn factionLeader = faction.leader;
            string factionLeaderLocation = GetLeaderLocationText(factionLeader);
            DrawLeadershipColumn(leftColumnRect, "SL_FactionLeadership", factionLeader, factionLeaderLocation, faction, null, leaderTracker, true);

            Pawn baseLeader = leaderTracker.GetBaseLeader(selectedSettlement);
            string baseLeaderLocation = selectedSettlement.LabelCap;
            DrawLeadershipColumn(rightColumnRect, "SL_BaseLeadership", baseLeader, baseLeaderLocation, selectedSettlement.Faction, selectedSettlement, leaderTracker, false);

            Widgets.DrawBoxSolid(new Rect(mainRect.center.x, mainRect.y, 1f, mainRect.height), Color.grey);
        }

        private void DrawLeadershipColumn(Rect rect, string titleKey, Pawn leader, string locationText, Faction faction, Settlement settlement, WorldComponent_LeaderTracker leaderTracker, bool isLeft)
        {
            float curY = rect.y;

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x, curY, rect.width, TitleHeight), titleKey.Translate().ToString().ToUpper());
            curY += TitleHeight + SectionSpacing;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            DrawLeaderInfo(new Rect(rect.x, curY, rect.width, PortraitSize), leader);
            curY += PortraitSize + SectionSpacing;

            DrawInfoRow(ref curY, rect, "SL_Location".Translate().ToString().ToUpper(), locationText);

            Widgets.DrawLineHorizontal(isLeft ? 0f : size.x / 2f, curY, size.x / 2f, Color.gray);
            curY += SectionSpacing;

            Widgets.Label(new Rect(rect.x, curY, rect.width, InfoRowHeight), "SL_CurrentEvents".Translate().ToString().ToUpper());
            curY += InfoRowHeight;

            DrawEvents(new Rect(rect.x, curY, rect.width, EventButtonHeight), faction, settlement, leaderTracker);
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
            Rect portraitRect = new Rect(rect.center.x - PortraitSize / 2f, rect.y, PortraitSize, PortraitSize);
            if (leader != null)
            {
                GUI.DrawTexture(portraitRect, PortraitsCache.Get(leader, new Vector2(PortraitSize, PortraitSize), Rot4.South));
            }
            else
            {
                GUI.DrawTexture(portraitRect, UnknownLeaderIcon);
            }
        }

        private void DrawInfoRow(ref float curY, Rect container, string label, string value)
        {
            Rect rowRect = new Rect(container.x, curY, container.width, InfoRowHeight);
            Widgets.Label(rowRect, label);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(rowRect, value);
            Text.Anchor = TextAnchor.UpperLeft;
            curY += InfoRowHeight;
        }

        private void DrawEvents(Rect rect, Faction faction, Settlement settlement, WorldComponent_LeaderTracker leaderTracker)
        {
            var events = settlement != null ? settlement.GetActiveEvents<PowerEventBase>() : faction.GetActiveEvents<PowerEventBase>();
            
            if (events.Any())
            {
                float currentY = rect.y;
                foreach (var powerEvent in events)
                {
                    Rect eventRect = new Rect(rect.x, currentY, rect.width, EventButtonHeight);
                    Widgets.DrawBoxSolid(eventRect, new Color(0.15f, 0.15f, 0.15f));
                    Widgets.DrawHighlightIfMouseover(eventRect);

                    Rect iconRect = new Rect(eventRect.x + 5f, eventRect.y, eventRect.height, eventRect.height).ContractedBy(5f);
                    GUI.DrawTexture(iconRect, powerEvent.def.Icon);

                    Text.Anchor = TextAnchor.MiddleLeft;
                    Rect textRect = new Rect(iconRect.xMax + 10f, eventRect.y, eventRect.width - iconRect.width - 20f, eventRect.height);
                    Widgets.Label(textRect, powerEvent.def.LabelCap);
                    Text.Anchor = TextAnchor.UpperLeft;

                    if (Mouse.IsOver(eventRect))
                    {
                        DrawPowerEventWindow(powerEvent);
                    }
                    currentY += EventButtonHeight + 5f;
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

            float height = contentHeight + (padding * 2);

            var mousePosition = UI.MousePositionOnUIInverted;
            Rect winRect = new Rect(mousePosition.x + 12, mousePosition.y + 12, width, height);

            if (winRect.yMax > UI.screenHeight)
            {
                winRect.y = UI.screenHeight - winRect.height;
            }

            Find.WindowStack.ImmediateWindow(15937564 + powerEvent.GetHashCode(), winRect, WindowLayer.Super, () =>
            {
                Rect r = winRect.AtZero().ContractedBy(padding);
                float curY = r.y;

                GUI.color = PowerEventTitleColor;
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(r.x, curY, r.width, 30f), powerEvent.def.LabelCap);
                curY += 35;
                GUI.color = Color.white;

                Text.Font = GameFont.Small;
                float descHeight = Text.CalcHeight(powerEvent.def.description, r.width);
                Widgets.Label(new Rect(r.x, curY, r.width, descHeight), powerEvent.def.description);
                curY += descHeight + 10f;

                foreach (var effect in powerEvent.def.effects)
                {
                    float effectTextHeight = Text.CalcHeight(effect, r.width - 10f);
                    Rect effectRect = new Rect(r.x, curY, r.width, effectTextHeight + 10f);
                    Widgets.DrawBoxSolid(effectRect, PowerEventBoxColor);

                    Rect textRect = effectRect.ContractedBy(5f);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(textRect, effect);
                    Text.Anchor = TextAnchor.UpperLeft;

                    curY += effectRect.height + 5f;
                }

                curY += 5f;
                GUI.color = Color.gray;
                int ticksLeft = powerEvent.EndTick - Find.TickManager.TicksGame;
                string expiresIn = "ExpiresIn".Translate().CapitalizeFirst() + " " + ticksLeft.ToStringTicksToPeriod();
                Widgets.Label(new Rect(r.x, curY, r.width, 20f), expiresIn);
                GUI.color = Color.white;
            }, doBackground: true);
        }
    }
}
