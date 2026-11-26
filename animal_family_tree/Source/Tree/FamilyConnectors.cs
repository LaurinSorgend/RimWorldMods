using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AnimalFamilyTree
{
    /// <summary>One set of parents and the offspring they share within one row.</summary>
    public class TreeFamily
    {
        public List<string> parents;
        public List<string> children = new List<string>();
        public int row;
        public int lane;
        public Color color;
        public float left;
        public float right;

        public bool Involves(string id) => id != null && (parents.Contains(id) || children.Contains(id));
    }

    /// <summary>
    /// Draws each family as its own colored bus line in the gap above the children,
    /// with overlapping families stacked on separate lanes.
    /// </summary>
    public static class FamilyConnectors
    {
        private const float TopMargin = 16f;
        private const float BottomMargin = 24f;
        private const float LaneSpacing = 9f;
        private const float MinGap = 56f;
        private const float DropSpacing = 7f;
        private const float LaneMargin = 14f;

        private static readonly Color[] Palette =
        {
            new Color(0.85f, 0.85f, 0.85f),
            new Color(0.95f, 0.60f, 0.35f),
            new Color(0.45f, 0.75f, 0.95f),
            new Color(0.60f, 0.90f, 0.50f),
            new Color(0.95f, 0.50f, 0.70f),
            new Color(0.95f, 0.85f, 0.40f),
            new Color(0.70f, 0.60f, 0.95f),
            new Color(0.40f, 0.85f, 0.80f),
        };

        public static IReadOnlyList<Color> Colors => Palette;

        public static float GapHeight(int lanes)
        {
            return Mathf.Max(MinGap, TopMargin + BottomMargin + Mathf.Max(0, lanes - 1) * LaneSpacing);
        }

        /// <summary>Groups children by their set of parents shown in the tree.</summary>
        public static List<TreeFamily> Collect(FamilyGraph graph, Dictionary<string, int> rowOf)
        {
            var byKey = new Dictionary<string, TreeFamily>();
            foreach (FamilyNode child in graph.nodes.Values)
            {
                var parents = graph.ParentsInGraph(child.id).Take(2).OrderBy(id => id).ToList();
                if (parents.Count == 0)
                    continue;
                int row = rowOf[child.id];
                string key = string.Join("|", parents) + "#" + row;
                if (!byKey.TryGetValue(key, out var family))
                    byKey[key] = family = new TreeFamily { parents = parents, row = row };
                family.children.Add(child.id);
            }
            return byKey.Values.ToList();
        }

        /// <summary>Assigns lanes so families whose lines overlap horizontally never share a height.</summary>
        public static int[] AssignLanes(List<TreeFamily> families, Func<string, float> xOf, int rowCount)
        {
            var lanesPerRow = new int[rowCount];
            foreach (TreeFamily f in families)
            {
                var xs = f.parents.Concat(f.children).Select(xOf).ToList();
                f.left = xs.Min() - DropSpacing * 2f;
                f.right = xs.Max() + DropSpacing * 2f;
            }
            int colorIndex = 0;
            foreach (var row in families.GroupBy(f => f.row))
            {
                var laneEnds = new List<float>();
                foreach (TreeFamily f in row.OrderBy(f => f.left))
                {
                    f.lane = FreeLane(laneEnds, f.left);
                    laneEnds[f.lane] = f.right;
                    f.color = Palette[colorIndex++ % Palette.Length];
                }
                lanesPerRow[row.Key] = laneEnds.Count;
            }
            return lanesPerRow;
        }

        private static int FreeLane(List<float> laneEnds, float left)
        {
            for (int i = 0; i < laneEnds.Count; i++)
            {
                if (laneEnds[i] + LaneMargin < left)
                    return i;
            }
            laneEnds.Add(float.MinValue);
            return laneEnds.Count - 1;
        }

        public static void BuildLines(TreeLayout layout)
        {
            Dictionary<(string, int), float> dropX = ParentDropPositions(layout);
            for (int i = 0; i < layout.families.Count; i++)
            {
                TreeFamily f = layout.families[i];
                float laneY = f.row == 0
                    ? layout.RowTop(0) - BottomMargin
                    : layout.RowTop(f.row - 1) + TreeLayout.NodeHeight + TopMargin + f.lane * LaneSpacing;
                var busXs = new List<float>();
                foreach (string parent in f.parents)
                {
                    float px = dropX[(parent, i)];
                    Add(layout, new Vector2(px, layout.rects[parent].yMax), new Vector2(px, laneY), i);
                    busXs.Add(px);
                }
                foreach (string child in f.children)
                {
                    Rect r = layout.rects[child];
                    Add(layout, new Vector2(r.center.x, laneY), new Vector2(r.center.x, r.yMin), i);
                    busXs.Add(r.center.x);
                }
                Add(layout, new Vector2(busXs.Min(), laneY), new Vector2(busXs.Max(), laneY), i);
            }
        }

        /// <summary>Spreads a parent's drop lines when it has offspring with several mates.</summary>
        private static Dictionary<(string, int), float> ParentDropPositions(TreeLayout layout)
        {
            var result = new Dictionary<(string, int), float>();
            var byParent = new Dictionary<string, List<int>>();
            for (int i = 0; i < layout.families.Count; i++)
            {
                foreach (string parent in layout.families[i].parents)
                {
                    if (!byParent.TryGetValue(parent, out var list))
                        byParent[parent] = list = new List<int>();
                    list.Add(i);
                }
            }
            foreach (var pair in byParent)
            {
                var ordered = pair.Value.OrderBy(i => (layout.families[i].left + layout.families[i].right) / 2f).ToList();
                float center = layout.rects[pair.Key].center.x;
                for (int k = 0; k < ordered.Count; k++)
                    result[(pair.Key, ordered[k])] = center + (k - (ordered.Count - 1) / 2f) * DropSpacing;
            }
            return result;
        }

        private static void Add(TreeLayout layout, Vector2 a, Vector2 b, int family)
        {
            layout.lines.Add(new TreeLine { a = a, b = b, family = family });
        }
    }
}
