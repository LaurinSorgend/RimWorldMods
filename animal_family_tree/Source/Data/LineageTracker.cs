using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>
    /// Keeps lineage records for colony animals and their relatives, scanning once per in-game hour.
    /// </summary>
    public class LineageTracker : GameComponent
    {
        private const int ScanInterval = GenDate.TicksPerHour;
        private static readonly List<string> NoIds = new List<string>();

        private Dictionary<string, LineageRecord> records = new Dictionary<string, LineageRecord>();
        private readonly Dictionary<string, Pawn> livePawns = new Dictionary<string, Pawn>();
        private Dictionary<string, List<string>> childIndex;

        /// <summary>Increases whenever records change, so UI can cache derived data.</summary>
        public int Version { get; private set; }

        public LineageTracker(Game game)
        {
        }

        public static LineageTracker Instance => Current.Game?.GetComponent<LineageTracker>();

        public IEnumerable<LineageRecord> AllRecords => records.Values;

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref records, "records", LookMode.Value, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && records == null)
                records = new Dictionary<string, LineageRecord>();
        }

        public override void FinalizeInit()
        {
            RebuildLiveCache();
            Scan();
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % ScanInterval == 0)
                Scan();
        }

        public LineageRecord GetRecord(string id)
        {
            return id != null && records.TryGetValue(id, out var rec) ? rec : null;
        }

        /// <summary>Returns the pawn if it still exists in the game.</summary>
        public Pawn TryGetLivePawn(string id)
        {
            return id != null && livePawns.TryGetValue(id, out var pawn) && !pawn.Discarded ? pawn : null;
        }

        public IReadOnlyList<string> ParentIdsOf(string id)
        {
            return GetRecord(id)?.parentIds ?? NoIds;
        }

        public IReadOnlyList<string> ChildIdsOf(string id)
        {
            if (childIndex == null)
                RebuildChildIndex();
            return childIndex.TryGetValue(id, out var kids) ? kids : NoIds;
        }

        /// <summary>Parents of a pawn according to the game's relations.</summary>
        public static IEnumerable<Pawn> ParentsOf(Pawn pawn)
        {
            if (pawn.relations == null)
                return Enumerable.Empty<Pawn>();
            return pawn.relations.DirectRelations
                .Where(r => r.def == PawnRelationDefOf.Parent && r.otherPawn != null)
                .Select(r => r.otherPawn);
        }

        /// <summary>Updates records from the current game state.</summary>
        public void Scan()
        {
            bool changed = false;
            foreach (Pawn pawn in ColonyAnimals().ToList())
                changed |= TrackWithRelatives(pawn, true);
            changed |= RefreshCachedPawns();
            if (changed)
                MarkChanged();
        }

        private static IEnumerable<Pawn> ColonyAnimals()
        {
            return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction
                .Where(p => p.RaceProps.Animal);
        }

        private bool RefreshCachedPawns()
        {
            bool changed = false;
            bool followFormer = AnimalFamilyTreeMod.Settings.trackFormerColonyAnimals;
            foreach (var pair in livePawns.ToList())
            {
                LineageRecord rec = GetRecord(pair.Key);
                if (pair.Value.Discarded)
                {
                    changed |= MarkGone(pair.Key, rec);
                    continue;
                }
                if (followFormer && rec != null && rec.wasColony && !pair.Value.Dead)
                    changed |= TrackWithRelatives(pair.Value, false);
                else
                    changed |= Track(pair.Value, false);
            }
            return changed;
        }

        private bool MarkGone(string id, LineageRecord rec)
        {
            livePawns.Remove(id);
            if (rec == null || rec.gone)
                return false;
            rec.gone = true;
            return true;
        }

        private bool TrackWithRelatives(Pawn pawn, bool inColony)
        {
            bool changed = Track(pawn, inColony);
            foreach (Pawn parent in ParentsOf(pawn).ToList())
                changed |= Track(parent, false);
            if (pawn.relations != null)
            {
                foreach (Pawn child in pawn.relations.Children.ToList())
                    changed |= Track(child, false);
            }
            return changed;
        }

        private bool Track(Pawn pawn, bool inColony)
        {
            if (pawn?.RaceProps == null || !pawn.RaceProps.Animal || pawn.Discarded)
                return false;
            string id = pawn.GetUniqueLoadID();
            livePawns[id] = pawn;
            if (records.TryGetValue(id, out var rec))
                return rec.UpdateFrom(pawn, inColony);
            rec = new LineageRecord { id = id };
            records[id] = rec;
            rec.UpdateFrom(pawn, inColony);
            return true;
        }

        private void RebuildLiveCache()
        {
            livePawns.Clear();
            foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
            {
                string id = pawn.GetUniqueLoadID();
                if (records.ContainsKey(id))
                    livePawns[id] = pawn;
            }
        }

        private void RebuildChildIndex()
        {
            childIndex = new Dictionary<string, List<string>>();
            foreach (LineageRecord rec in records.Values)
            {
                foreach (string parentId in rec.parentIds)
                {
                    if (!childIndex.TryGetValue(parentId, out var kids))
                        childIndex[parentId] = kids = new List<string>();
                    kids.Add(rec.id);
                }
            }
        }

        private void MarkChanged()
        {
            childIndex = null;
            Version++;
        }
    }
}
