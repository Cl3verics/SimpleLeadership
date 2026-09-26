using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SimpleLeadership
{
    [HotSwappable]
    public abstract class PowerEventBase : IExposable
    {
        public PowerEventDef def;
        protected int endTick;

        public int EndTick => endTick;

        public void Initialize(PowerEventDef def, params object[] args)
        {
            this.def = def;
            endTick = Find.TickManager.TicksGame + (int)(def.durationDays.RandomInRange * 60000f);
            SetParameters(args);
        }

        public void ReduceDuration(float factor)
        {
            var remaining = endTick - Find.TickManager.TicksGame;
            if (remaining > 0)
            {
                endTick = Find.TickManager.TicksGame + Mathf.RoundToInt(remaining * factor);
            }
        }

        public virtual void SetParameters(params object[] args) { }

        public bool IsActive()
        {
            return Find.TickManager.TicksGame < endTick;
        }

        public virtual void OnStart()
        {
            if (ShouldGiveMessage() && string.IsNullOrEmpty(def.startMessage) is false)
            {
                SendMessage(GetFormattedMessage(def.startMessage), MessageTypeDefOf.NeutralEvent);
            }
        }

        public virtual void OnResolve()
        {
            if (ShouldGiveMessage() && string.IsNullOrEmpty(def.endMessage) is false)
            {
                SendMessage(GetFormattedMessage(def.endMessage), MessageTypeDefOf.NeutralEvent);
            }
        }

        protected void SendMessage(string message, MessageTypeDef type)
        {
            var target = GetTarget();
            if (target is WorldObject worldObject)
            {
                Messages.Message(message, worldObject, type);
            }
            else
            {
                Messages.Message(message, type);
            }
        }

        protected virtual string GetFormattedMessage(string message)
        {
            return message;
        }

        public virtual bool ShouldGiveMessage() => SimpleLeadershipMod.Settings.enableAlerts;

        public abstract bool IsDuplicate(PowerEventBase other);

        public abstract bool IsTarget(object target);

        public virtual object GetTarget() => null;

        public virtual void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref endTick, "endTick");
        }
    }
}
