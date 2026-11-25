using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Adds the family tree comp to every animal race at startup, including modded ones.</summary>
    [StaticConstructorOnStartup]
    public static class CompInjector
    {
        static CompInjector()
        {
            int count = 0;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.Where(IsAnimal))
            {
                if (def.comps == null)
                    def.comps = new List<CompProperties>();
                if (def.comps.Any(c => c is CompProperties_AnimalFamilyTree))
                    continue;
                def.comps.Add(new CompProperties_AnimalFamilyTree());
                count++;
            }
            Log.Message($"[Animal Family Tree] Added family tree button to {count} animal races.");
        }

        private static bool IsAnimal(ThingDef def)
        {
            return def.race != null && def.race.Animal && typeof(Pawn).IsAssignableFrom(def.thingClass);
        }
    }
}
