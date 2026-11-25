using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Saved snapshot of one animal, kept after the game deletes the pawn.</summary>
    public class LineageRecord : IExposable
    {
        public string id;
        public string name;
        public PawnKindDef kind;
        public Gender gender;
        public long birthAbsTicks;
        public long deathAbsTicks = -1;
        public bool wasColony;
        public bool gone;
        public List<string> parentIds = new List<string>();

        public bool IsDead => deathAbsTicks >= 0;

        public ThingDef Race => kind?.race;

        /// <summary>Name if the animal has one, otherwise the kind label.</summary>
        public string DisplayName => name.NullOrEmpty() ? (kind?.LabelCap.ToString() ?? "?") : name;

        /// <summary>Age in years, at death for dead animals.</summary>
        public float AgeYears
        {
            get
            {
                long end = IsDead ? deathAbsTicks : GenTicks.TicksAbs;
                return (end - birthAbsTicks) / (float)GenDate.TicksPerYear;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref name, "name");
            Scribe_Defs.Look(ref kind, "kind");
            Scribe_Values.Look(ref gender, "gender");
            Scribe_Values.Look(ref birthAbsTicks, "birth");
            Scribe_Values.Look(ref deathAbsTicks, "death", -1);
            Scribe_Values.Look(ref wasColony, "wasColony");
            Scribe_Values.Look(ref gone, "gone");
            Scribe_Collections.Look(ref parentIds, "parents", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && parentIds == null)
                parentIds = new List<string>();
        }

        /// <summary>Copies current data from a pawn. Returns true if anything changed.</summary>
        public bool UpdateFrom(Pawn pawn, bool inColony)
        {
            bool changed = false;
            changed |= Set(ref name, pawn.Name?.ToStringShort);
            changed |= Set(ref kind, pawn.kindDef);
            changed |= Set(ref gender, pawn.gender);
            changed |= Set(ref birthAbsTicks, pawn.ageTracker.BirthAbsTicks);
            if (inColony && !wasColony)
            {
                wasColony = true;
                changed = true;
            }
            if (pawn.Dead && !IsDead)
            {
                deathAbsTicks = DeathAbsTicks(pawn);
                changed = true;
            }
            foreach (Pawn parent in LineageTracker.ParentsOf(pawn))
                changed |= AddParent(parent.GetUniqueLoadID());
            return changed;
        }

        private bool AddParent(string parentId)
        {
            if (parentIds.Contains(parentId))
                return false;
            parentIds.Add(parentId);
            return true;
        }

        private static long DeathAbsTicks(Pawn pawn)
        {
            int offset = GenTicks.TicksAbs - Find.TickManager.TicksGame;
            int deathGameTick = pawn.Corpse?.timeOfDeath ?? Find.TickManager.TicksGame;
            return deathGameTick + offset;
        }

        private static bool Set<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            return true;
        }
    }
}
