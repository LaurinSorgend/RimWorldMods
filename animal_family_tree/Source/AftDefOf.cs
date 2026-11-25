using RimWorld;

namespace AnimalFamilyTree
{
    [DefOf]
    public static class AftDefOf
    {
        public static MainButtonDef AFT_AnimalFamilyTree;

        static AftDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AftDefOf));
        }
    }
}
