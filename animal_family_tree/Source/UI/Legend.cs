using UnityEngine;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Small box in the top-right corner of the tree explaining colors and lines.</summary>
    public static class Legend
    {
        private const float Width = 250f;
        private const float RowHeight = 22f;

        private enum Swatch { Box, Lines, None }

        public static void Draw(Rect treeArea)
        {
            var entries = new (Color color, Swatch swatch, string key)[]
            {
                (NodeDrawer.RootColor, Swatch.Box, "AFT.Legend.Self"),
                (NodeDrawer.ColonyColor, Swatch.Box, "AFT.Legend.Colony"),
                (NodeDrawer.DeadColor, Swatch.Box, "AFT.Legend.Dead"),
                (NodeDrawer.GoneColor, Swatch.Box, "AFT.Legend.Gone"),
                (Color.white, Swatch.Lines, "AFT.Legend.Families"),
                (Color.white, Swatch.None, "AFT.Legend.Hover"),
            };
            var box = new Rect(treeArea.xMax - Width - 8f, treeArea.y + 8f, Width, entries.Length * RowHeight + 12f);
            Widgets.DrawWindowBackground(box);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            for (int i = 0; i < entries.Length; i++)
            {
                var row = new Rect(box.x + 8f, box.y + 6f + i * RowHeight, box.width - 16f, RowHeight);
                DrawSwatch(new Rect(row.x, row.y + 4f, 24f, RowHeight - 8f), entries[i].color, entries[i].swatch);
                float indent = entries[i].swatch == Swatch.None ? 0f : 32f;
                if (entries[i].swatch == Swatch.None)
                    GUI.color = Color.gray;
                Widgets.Label(new Rect(row.x + indent, row.y, row.width - indent, row.height), entries[i].key.Translate());
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void DrawSwatch(Rect r, Color color, Swatch swatch)
        {
            if (swatch == Swatch.Lines)
            {
                for (int i = 0; i < 3; i++)
                {
                    float y = r.y + 1f + i * (r.height - 2f) / 2f;
                    Widgets.DrawLine(new Vector2(r.x, y), new Vector2(r.xMax, y), FamilyConnectors.Colors[i], 2f);
                }
                return;
            }
            if (swatch == Swatch.None)
                return;
            Widgets.DrawBoxSolid(r, color);
            GUI.color = new Color(1f, 1f, 1f, 0.35f);
            Widgets.DrawBox(r);
            GUI.color = Color.white;
        }
    }
}
