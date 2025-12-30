using Verse;
using RimWorld;

namespace SimpleLeadership
{
    public abstract class PowerEventBase : IExposable
    {
        public PowerEventDef def;
        protected int endTick;

        public int EndTick => endTick;

        public PowerEventBase()
        {
        }

        public void Initialize(PowerEventDef def, params object[] args)
        {
            this.def = def;
            this.endTick = Find.TickManager.TicksGame + (int)(def.durationDays.RandomInRange * 60000f);
            SetParameters(args);
        }

        public virtual void SetParameters(params object[] args) { }

        public bool IsActive()
        {
            return Find.TickManager.TicksGame < endTick;
        }

        public virtual void OnStart()
        {
            if (ShouldGiveMessage() && !string.IsNullOrEmpty(def.startMessage))
            {
                string message = GetFormattedMessage(def.startMessage);
                Messages.Message(message, MessageTypeDefOf.NeutralEvent);
            }
        }

        public virtual void OnResolve()
        {
            if (ShouldGiveMessage() && !string.IsNullOrEmpty(def.endMessage))
            {
                string message = GetFormattedMessage(def.endMessage);
                Messages.Message(message, MessageTypeDefOf.NeutralEvent);
            }
        }

        protected virtual string GetFormattedMessage(string message)
        {
            return message;
        }

        public virtual bool ShouldGiveMessage()
        {
            return true;
        }

        public abstract bool IsDuplicate(PowerEventBase other);

        public abstract bool IsTarget(object target);

        public virtual void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref endTick, "endTick");
        }
    }
}
