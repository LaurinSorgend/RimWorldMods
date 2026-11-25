using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AnimalFamilyTree
{
    public class CompProperties_AnimalFamilyTree : CompProperties
    {
        public CompProperties_AnimalFamilyTree()
        {
            compClass = typeof(CompAnimalFamilyTree);
        }
    }

    /// <summary>Adds a "Family tree" button to selected colony animals.</summary>
    [StaticConstructorOnStartup]
    public class CompAnimalFamilyTree : ThingComp
    {
        private static readonly Texture2D Icon =
            ContentFinder<Texture2D>.Get("UI/AnimalFamilyTree/Gizmo", false) ?? BaseContent.BadTex;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (!(parent is Pawn pawn) || pawn.Dead || pawn.Faction == null || !pawn.Faction.IsPlayer)
                yield break;
            yield return new Command_Action
            {
                defaultLabel = "AFT.Gizmo.Label".Translate(),
                defaultDesc = "AFT.Gizmo.Desc".Translate(),
                icon = Icon,
                action = () => Open(pawn)
            };
        }

        private static void Open(Pawn pawn)
        {
            LineageTracker.Instance?.Scan();
            MainTabWindow_AnimalFamilyTree.OpenFor(pawn.GetUniqueLoadID());
        }
    }
}
