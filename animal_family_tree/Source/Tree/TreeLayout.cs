using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnimalFamilyTree
{
    /// <summary>A connector segment in tree space.</summary>
    public struct TreeLine
    {
        public Vector2 a;
        public Vector2 b;
        public bool mateBar;
    }

    /// <summary>
    /// Places nodes in generation rows and orders each row so children sit under their parents.
    /// </summary>
    public class TreeLayout
    {
        public const float NodeWidth = 160f;
        public const float NodeHeight = 64f;
        public const float HGap = 24f;
        public const float VGap = 64f;
        private const float Step = NodeWidth + HGap;
        private const float BarDrop = 10f;
        private const int OrderPasses = 4;

        public readonly Dictionary<string, Rect> rects = new Dictionary<string, Rect>();
        public readonly List<TreeLine> lines = new List<TreeLine>();
        public Rect bounds;

        private readonly FamilyGraph graph;
        private readonly Dictionary<string, float> x = new Dictionary<string, float>();
        private List<List<FamilyNode>> rows;
        private int topGeneration;

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
            layout.BuildRects();
            new ConnectorBuilder(graph, layout).Build();
            return layout;
        }

        private void BuildRows()
        {
            var byGen = graph.nodes.Values.GroupBy(n => n.Generation).OrderBy(g => g.Key).ToList();
            topGeneration = byGen[0].Key;
            rows = byGen.Select(g => g.ToList()).ToList();
            foreach (var row in rows)
                SetIndexPositions(row);
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
            rows[index] = row.OrderBy(n => keys[n.id]).ToList();
            SetIndexPositions(rows[index]);
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
            if (n.IsMate && x.TryGetValue(n.mateOf.id, out float partnerX) && SameRow(n, n.mateOf))
                return partnerX + Step;
            return Barycenter(n, byParents) ?? x[n.id];
        }

        private static bool SameRow(FamilyNode a, FamilyNode b) => a.Generation == b.Generation;

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

        private void BuildRects()
        {
            bool first = true;
            foreach (FamilyNode n in graph.nodes.Values)
            {
                float y = (n.Generation - topGeneration) * (NodeHeight + VGap);
                var rect = new Rect(x[n.id] - NodeWidth / 2f, y, NodeWidth, NodeHeight);
                rects[n.id] = rect;
                bounds = first ? rect : MinMaxRect(bounds, rect);
                first = false;
            }
        }

        private static Rect MinMaxRect(Rect a, Rect b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin),
                Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }

        /// <summary>Builds elbow connectors from parents (or a parent pair's bar) to each child.</summary>
        private class ConnectorBuilder
        {
            private readonly FamilyGraph graph;
            private readonly TreeLayout layout;
            private readonly HashSet<string> drawnBars = new HashSet<string>();

            public ConnectorBuilder(FamilyGraph graph, TreeLayout layout)
            {
                this.graph = graph;
                this.layout = layout;
            }

            public void Build()
            {
                foreach (FamilyNode child in graph.nodes.Values)
                {
                    var parents = graph.ParentsInGraph(child.id).Take(2).ToList();
                    if (parents.Count == 0)
                        continue;
                    Vector2 anchor = parents.Count == 2 ? PairAnchor(parents[0], parents[1]) : Bottom(parents[0]);
                    AddElbow(anchor, layout.rects[child.id]);
                }
            }

            private Vector2 Bottom(string id)
            {
                Rect r = layout.rects[id];
                return new Vector2(r.center.x, r.yMax);
            }

            private Vector2 PairAnchor(string a, string b)
            {
                Vector2 pa = Bottom(a), pb = Bottom(b);
                float barY = Mathf.Max(pa.y, pb.y) + BarDrop;
                string key = string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
                if (drawnBars.Add(key))
                {
                    Add(pa, new Vector2(pa.x, barY), true);
                    Add(pb, new Vector2(pb.x, barY), true);
                    Add(new Vector2(pa.x, barY), new Vector2(pb.x, barY), true);
                }
                return new Vector2((pa.x + pb.x) / 2f, barY);
            }

            private void AddElbow(Vector2 anchor, Rect child)
            {
                var top = new Vector2(child.center.x, child.yMin);
                float midY = top.y > anchor.y ? top.y - VGap / 3f : anchor.y + BarDrop;
                Add(anchor, new Vector2(anchor.x, midY), false);
                Add(new Vector2(anchor.x, midY), new Vector2(top.x, midY), false);
                Add(new Vector2(top.x, midY), top, false);
            }

            private void Add(Vector2 a, Vector2 b, bool mateBar)
            {
                layout.lines.Add(new TreeLine { a = a, b = b, mateBar = mateBar });
            }
        }
    }
}
