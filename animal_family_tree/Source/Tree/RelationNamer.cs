using System.Linq;
using System.Text;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Turns blood distances into labels like "great-grandmother" or "half-sister".</summary>
    public static class RelationNamer
    {
        public static void NameAll(FamilyGraph graph, LineageTracker tracker)
        {
            foreach (FamilyNode node in graph.nodes.Values.Where(n => !n.IsMate))
                node.relation = BloodLabel(node, graph, tracker);
            foreach (FamilyNode node in graph.nodes.Values.Where(n => n.IsMate))
                node.relation = MateLabel(node, graph);
        }

        private static string MateLabel(FamilyNode node, FamilyGraph graph)
        {
            string mate = Key("Mate");
            if (node.mateOf.id == graph.rootId)
                return mate;
            return "AFT.Rel.Possessive".Translate(node.mateOf.relation, mate);
        }

        private static string BloodLabel(FamilyNode node, FamilyGraph graph, LineageTracker tracker)
        {
            Gender g = node.rec.gender;
            int up = node.up, down = node.down;
            if (up == 0 && down == 0)
                return Key("Self");
            if (down == 0)
                return Lineal(up, Gendered(g, "Mother", "Father", "Parent"));
            if (up == 0)
                return Lineal(down, Gendered(g, "Daughter", "Son", "Child"));
            if (up == 1 && down == 1)
                return SiblingLabel(node, graph, tracker);
            if (down == 1)
                return Greats(up - 2) + Gendered(g, "Aunt", "Uncle", "AuntOrUncle");
            if (up == 1)
                return Greats(down - 2) + Gendered(g, "Niece", "Nephew", "NieceOrNephew");
            return Key("Cousin");
        }

        private static string SiblingLabel(FamilyNode node, FamilyGraph graph, LineageTracker tracker)
        {
            var rootParents = tracker.ParentIdsOf(graph.rootId);
            var nodeParents = tracker.ParentIdsOf(node.id);
            int shared = rootParents.Count(nodeParents.Contains);
            bool half = shared == 1 && rootParents.Count == 2 && nodeParents.Count == 2;
            string sibling = Gendered(node.rec.gender, "Sister", "Brother", "Sibling");
            return half ? Key("Half") + sibling : sibling;
        }

        /// <summary>1 = parent, 2 = grandparent, 3 = great-grandparent ...</summary>
        private static string Lineal(int distance, string baseWord)
        {
            if (distance == 1)
                return baseWord;
            return Greats(distance - 2) + Key("Grand") + baseWord;
        }

        private static string Greats(int count)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
                sb.Append(Key("Great"));
            return sb.ToString();
        }

        private static string Gendered(Gender g, string female, string male, string neutral)
        {
            return Key(g == Gender.Female ? female : g == Gender.Male ? male : neutral);
        }

        private static string Key(string name)
        {
            return ("AFT.Rel." + name).Translate();
        }
    }
}
