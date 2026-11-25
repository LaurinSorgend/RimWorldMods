using UnityEngine;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Small box in the top-right corner of the tree explaining colors and lines.</summary>
    public static class Legend
    {
        private const float Width = 230f;
        private const float RowHeight = 22f;

        public static void Draw(Rect treeArea)
        {
            var entries = new (Color color, bool line, string key)[]
            {
                (NodeDrawer.RootColor, false, "AFT.Legend.Self"),
                (NodeDrawer.ColonyColor, false, "AFT.Legend.Colony"),
                (NodeDrawer.DeadColor, false, "AFT.Legend.Dead"),
                (NodeDrawer.GoneColor, false, "AFT.Legend.Gone"),
                (TreeCanvas.LineColor, true, "AFT.Legend.Blood"),
                (TreeCanvas.MateLineColor, true, "AFT.Legend.Mates"),
            };
            var box = new Rect(treeArea.xMax - Width - 8f, treeArea.y + 8f, Width, entries.Length * RowHeight + 12f);
            Widgets.DrawWindowBackground(box);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleLeft;
            for (int i = 0; i < entries.Length; i++)
            {
                var row = new Rect(box.x + 8f, box.y + 6f + i * RowHeight, box.width - 16f, RowHeight);
                DrawSwatch(new Rect(row.x, row.y + 4f, 24f, RowHeight - 8f), entries[i].color, entries[i].line);
                Widgets.Label(new Rect(row.x + 32f, row.y, row.width - 32f, row.height), entries[i].key.Translate());
            }
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static void DrawSwatch(Rect r, Color color, bool line)
        {
            if (line)
            {
                Widgets.DrawLine(new Vector2(r.x, r.center.y), new Vector2(r.xMax, r.center.y), color, 2f);
                return;
            }
            Widgets.DrawBoxSolid(r, color);
            GUI.color = new Color(1f, 1f, 1f, 0.35f);
            Widgets.DrawBox(r);
            GUI.color = Color.white;
        }
    }
}
