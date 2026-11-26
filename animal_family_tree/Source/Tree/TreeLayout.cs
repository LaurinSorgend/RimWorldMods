using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnimalFamilyTree
{
    /// <summary>A connector segment in tree space, belonging to one family.</summary>
    public struct TreeLine
    {
        public Vector2 a;
        public Vector2 b;
        public int family;
    }

    /// <summary>
    /// Places nodes in generation rows and orders each row so children sit under their parents.
    /// </summary>
    public class TreeLayout
    {
        public const float NodeWidth = 160f;
        public const float NodeHeight = 64f;
        public const float HGap = 24f;
        private const float Step = NodeWidth + HGap;
        private const int OrderPasses = 4;

        public readonly Dictionary<string, Rect> rects = new Dictionary<string, Rect>();
        public readonly List<TreeLine> lines = new List<TreeLine>();
        public List<TreeFamily> families = new List<TreeFamily>();
        public Rect bounds;

        private readonly FamilyGraph graph;
        private readonly Dictionary<string, float> x = new Dictionary<string, float>();
        private readonly Dictionary<string, int> rowOf = new Dictionary<string, int>();
        private List<List<FamilyNode>> rows;
        private float[] rowTops;

        private TreeLayout(FamilyGraph graph)
        {
            this.graph = graph;
        }

        public static TreeLayout Build(FamilyGraph graph)
        {
            var layout = new TreeLayout(graph);
            layout.BuildRows();
            layout.OrderRows();
            layout.PlaceRows();
            layout.families = FamilyConnectors.Collect(graph, layout.rowOf);
            int[] lanes = FamilyConnectors.AssignLanes(layout.families, layout.X, layout.rows.Count);
            layout.BuildRowTops(lanes);
            layout.BuildRects();
            FamilyConnectors.BuildLines(layout);
            return layout;
        }

        public float X(string id) => x[id];

        public int RowOf(string id) => rowOf[id];

        public float RowTop(int row) => rowTops[row];

        private void BuildRows()
        {
            rows = graph.nodes.Values.GroupBy(n => n.Generation).OrderBy(g => g.Key)
                .Select(g => g.ToList()).ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                foreach (FamilyNode n in rows[i])
                    rowOf[n.id] = i;
                SetIndexPositions(rows[i]);
            }
        }

        private void SetIndexPositions(List<FamilyNode> row)
        {
            for (int i = 0; i < row.Count; i++)
                x[row[i].id] = i * Step;
        }

        /// <summary>Barycenter sweeps: down using parents, up using children.</summary>
        private void OrderRows()
        {
            for (int pass = 0; pass < OrderPasses; pass++)
            {
                for (int i = 1; i < rows.Count; i++)
                    SortRow(i, true);
                for (int i = rows.Count - 2; i >= 0; i--)
                    SortRow(i, false);
            }
        }

        private void SortRow(int index, bool byParents)
        {
            List<FamilyNode> row = rows[index];
            var keys = new Dictionary<string, float>();
            foreach (FamilyNode n in row.Where(n => !n.IsMate))
                keys[n.id] = Barycenter(n, byParents) ?? x[n.id];
            foreach (FamilyNode n in row.Where(n => n.IsMate))
                keys[n.id] = MateKey(n, keys, byParents);
            rows[index] = row.OrderBy(n => keys[n.id]).ThenBy(n => ParentKey(n)).ToList();
            SetIndexPositions(rows[index]);
        }

        /// <summary>Tie-breaker that keeps full siblings next to each other.</summary>
        private string ParentKey(FamilyNode n)
        {
            return string.Join("|", graph.ParentsInGraph(n.id).OrderBy(id => id));
        }

        private float MateKey(FamilyNode mate, Dictionary<string, float> keys, bool byParents)
        {
            if (keys.TryGetValue(mate.mateOf.id, out float partnerKey))
                return partnerKey + 0.01f;
            return Barycenter(mate, byParents) ?? x[mate.id];
        }

        private float? Barycenter(FamilyNode n, bool byParents)
        {
            var ids = byParents ? graph.ParentsInGraph(n.id) : graph.ChildrenInGraph(n.id);
            var values = ids.Where(x.ContainsKey).Select(id => x[id]).ToList();
            return values.Count == 0 ? (float?)null : values.Average();
        }

        /// <summary>Turns the row order into coordinates without overlaps.</summary>
        private void PlaceRows()
        {
            for (int i = 1; i < rows.Count; i++)
                Pack(rows[i], n => Desired(n, true));
            for (int i = rows.Count - 2; i >= 0; i--)
                Pack(rows[i], n => Desired(n, false));
            for (int i = 1; i < rows.Count; i++)
                Pack(rows[i], n => Desired(n, true));
        }

        private float Desired(FamilyNode n, bool byParents)
        {
            if (n.IsMate && x.TryGetValue(n.mateOf.id, out float partnerX) && n.Generation == n.mateOf.Generation)
                return partnerX + Step;
            return Barycenter(n, byParents) ?? x[n.id];
        }

        private void Pack(List<FamilyNode> row, Func<FamilyNode, float> desired)
        {
            var wanted = row.Select(desired).ToList();
            var placed = new float[row.Count];
            for (int i = 0; i < row.Count; i++)
                placed[i] = i == 0 ? wanted[0] : Math.Max(wanted[i], placed[i - 1] + Step);
            float shift = 0f;
            for (int i = 0; i < row.Count; i++)
                shift += wanted[i] - placed[i];
            shift /= row.Count;
            for (int i = 0; i < row.Count; i++)
                x[row[i].id] = placed[i] + shift;
        }

        private void BuildRowTops(int[] lanesPerRow)
        {
            rowTops = new float[rows.Count];
            for (int i = 1; i < rows.Count; i++)
                rowTops[i] = rowTops[i - 1] + NodeHeight + FamilyConnectors.GapHeight(lanesPerRow[i]);
        }

        private void BuildRects()
        {
            bool first = true;
            foreach (FamilyNode n in graph.nodes.Values)
            {
                var rect = new Rect(x[n.id] - NodeWidth / 2f, rowTops[rowOf[n.id]], NodeWidth, NodeHeight);
                rects[n.id] = rect;
                bounds = first ? rect : Rect.MinMaxRect(Mathf.Min(bounds.xMin, rect.xMin),
                    Mathf.Min(bounds.yMin, rect.yMin), Mathf.Max(bounds.xMax, rect.xMax),
                    Mathf.Max(bounds.yMax, rect.yMax));
                first = false;
            }
        }
    }
}
