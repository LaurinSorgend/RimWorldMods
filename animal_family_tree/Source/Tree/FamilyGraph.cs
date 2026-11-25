using System.Collections.Generic;
using System.Linq;

namespace AnimalFamilyTree
{
    /// <summary>One animal in a tree, with its blood distance to the root.</summary>
    public class FamilyNode
    {
        public string id;
        public LineageRecord rec;
        public int up;
        public int down;
        public FamilyNode mateOf;
        public string relation;

        public bool IsMate => mateOf != null;

        /// <summary>Generation relative to the root: negative = older.</summary>
        public int Generation => IsMate ? mateOf.Generation : down - up;
    }

    /// <summary>The set of relatives shown around a root animal.</summary>
    public class FamilyGraph
    {
        public string rootId;
        public readonly Dictionary<string, FamilyNode> nodes = new Dictionary<string, FamilyNode>();
        public bool truncated;

        public FamilyNode Root => nodes[rootId];

        private readonly LineageTracker tracker;
        private readonly AftSettings settings;

        private FamilyGraph(LineageTracker tracker, string rootId, AftSettings settings)
        {
            this.tracker = tracker;
            this.rootId = rootId;
            this.settings = settings;
        }

        /// <summary>Builds the graph, or returns null if the root has no record.</summary>
        public static FamilyGraph Build(LineageTracker tracker, string rootId, AftSettings settings)
        {
            LineageRecord rootRec = tracker.GetRecord(rootId);
            if (rootRec == null)
                return null;
            var graph = new FamilyGraph(tracker, rootId, settings);
            graph.CollectBloodRelatives(rootRec);
            graph.CollectMates();
            RelationNamer.NameAll(graph, tracker);
            return graph;
        }

        public IEnumerable<string> ParentsInGraph(string id)
        {
            return tracker.ParentIdsOf(id).Where(nodes.ContainsKey);
        }

        public IEnumerable<string> ChildrenInGraph(string id)
        {
            return tracker.ChildIdsOf(id).Where(nodes.ContainsKey);
        }

        private void CollectBloodRelatives(LineageRecord rootRec)
        {
            var queue = new Queue<FamilyNode>();
            Enqueue(queue, new FamilyNode { id = rootId, rec = rootRec });
            while (queue.Count > 0)
            {
                FamilyNode current = queue.Dequeue();
                if (current.down == 0 && current.up < settings.generationsUp)
                {
                    foreach (string parentId in tracker.ParentIdsOf(current.id))
                        TryAdd(queue, parentId, current.up + 1, 0);
                }
                if (current.down + 1 - current.up <= settings.generationsDown)
                {
                    foreach (string childId in tracker.ChildIdsOf(current.id))
                        TryAdd(queue, childId, current.up, current.down + 1);
                }
            }
        }

        private void TryAdd(Queue<FamilyNode> queue, string id, int up, int down)
        {
            if (nodes.ContainsKey(id))
                return;
            LineageRecord rec = tracker.GetRecord(id);
            if (rec == null)
                return;
            if (nodes.Count >= settings.maxNodes)
            {
                truncated = true;
                return;
            }
            Enqueue(queue, new FamilyNode { id = id, rec = rec, up = up, down = down });
        }

        private void Enqueue(Queue<FamilyNode> queue, FamilyNode node)
        {
            nodes[node.id] = node;
            queue.Enqueue(node);
        }

        /// <summary>Adds the missing other parent of every child in the graph.</summary>
        private void CollectMates()
        {
            foreach (FamilyNode child in nodes.Values.ToList())
            {
                List<string> parents = tracker.ParentIdsOf(child.id).ToList();
                FamilyNode known = parents.Where(nodes.ContainsKey).Select(p => nodes[p])
                    .FirstOrDefault(p => !p.IsMate);
                if (known == null)
                    continue;
                foreach (string parentId in parents)
                    TryAddMate(parentId, known);
            }
        }

        private void TryAddMate(string id, FamilyNode partner)
        {
            if (nodes.ContainsKey(id))
                return;
            LineageRecord rec = tracker.GetRecord(id);
            if (rec == null)
                return;
            if (nodes.Count >= settings.maxNodes + settings.maxNodes / 4)
            {
                truncated = true;
                return;
            }
            nodes[id] = new FamilyNode { id = id, rec = rec, mateOf = partner };
        }
    }
}
