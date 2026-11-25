using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Draws one animal box in the tree.</summary>
    [StaticConstructorOnStartup]
    public static class NodeDrawer
    {
        public static readonly Color RootColor = new Color(0.42f, 0.34f, 0.12f);
        public static readonly Color ColonyColor = new Color(0.15f, 0.23f, 0.31f);
        public static readonly Color OtherColor = new Color(0.21f, 0.21f, 0.21f);
        public static readonly Color DeadColor = new Color(0.12f, 0.12f, 0.12f);
        public static readonly Color GoneColor = new Color(0.21f, 0.21f, 0.21f, 0.55f);
        private static readonly Color RelationColor = new Color(0.95f, 0.85f, 0.55f);

        private static readonly Texture2D Tombstone =
            ContentFinder<Texture2D>.Get("UI/AnimalFamilyTree/Tombstone", false);

        public static void Draw(Rect r, FamilyNode node, Pawn pawn, bool isRoot, bool selected, float zoom)
        {
            Widgets.DrawBoxSolid(r, BackgroundColor(node.rec, isRoot));
            GUI.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            Widgets.DrawBox(r, selected ? 2 : 1);
            GUI.color = node.rec.gone && !node.rec.IsDead ? new Color(1f, 1f, 1f, 0.6f) : Color.white;
            float pad = 4f * zoom;
            Rect icon = new Rect(r.x + pad, r.y + pad, r.height - 2f * pad, r.height - 2f * pad);
            AnimalInfo.DrawIcon(icon, node.rec, pawn);
            DrawTombstone(icon, node.rec);
            GUI.color = Color.white;
            DrawGender(r, node.rec, zoom);
            Rect text = new Rect(icon.xMax + pad, r.y + pad, r.xMax - icon.xMax - 2f * pad - 14f * zoom, icon.height);
            DrawText(text, node, zoom);
            if (Mouse.IsOver(r))
                Widgets.DrawHighlight(r);
        }

        public static Color BackgroundColor(LineageRecord rec, bool isRoot)
        {
            if (isRoot)
                return RootColor;
            if (rec.IsDead)
                return DeadColor;
            if (rec.gone)
                return GoneColor;
            return rec.wasColony ? ColonyColor : OtherColor;
        }

        private static void DrawTombstone(Rect icon, LineageRecord rec)
        {
            if (!rec.IsDead || Tombstone == null || !AnimalFamilyTreeMod.Settings.deadAsTombstone)
                return;
            float size = icon.width * 0.5f;
            GUI.DrawTexture(new Rect(icon.xMax - size, icon.yMax - size, size, size), Tombstone);
        }

        private static void DrawGender(Rect r, LineageRecord rec, float zoom)
        {
            if (rec.gender == Gender.None || zoom < 0.5f)
                return;
            float size = 12f * zoom;
            GUI.DrawTexture(new Rect(r.xMax - size - 3f, r.y + 3f, size, size), rec.gender.GetIcon());
        }

        private static void DrawText(Rect rect, FamilyNode node, float zoom)
        {
            if (zoom < 0.5f)
                return;
            Text.Font = zoom >= 0.85f ? GameFont.Small : GameFont.Tiny;
            float lineH = Text.LineHeight;
            var line = new Rect(rect.x, rect.y, rect.width, lineH);
            Text.WordWrap = false;
            Widgets.Label(line, node.rec.DisplayName.Truncate(rect.width));
            Text.Font = GameFont.Tiny;
            line.y += lineH;
            line.height = Text.LineHeight;
            GUI.color = RelationColor;
            Widgets.Label(line, (node.relation ?? "").CapitalizeFirst().Truncate(rect.width));
            if (zoom >= 0.85f && line.yMax + Text.LineHeight <= rect.yMax + 4f)
            {
                line.y += Text.LineHeight;
                GUI.color = Color.gray;
                Widgets.Label(line, AnimalInfo.AgeText(node.rec).Truncate(rect.width));
            }
            GUI.color = Color.white;
            Text.WordWrap = true;
            Text.Font = GameFont.Small;
        }

        /// <summary>Multi-line tooltip text for a node.</summary>
        public static string Tooltip(FamilyNode node, Pawn pawn, FamilyGraph graph)
        {
            var sb = new StringBuilder();
            sb.AppendLine(node.rec.DisplayName.Colorize(ColoredText.TipSectionTitleColor));
            sb.AppendLine("AFT.Tooltip.Relation".Translate(node.relation ?? "?"));
            sb.AppendLine("AFT.Tooltip.Kind".Translate(AnimalInfo.KindAndGender(node.rec)));
            sb.AppendLine(AnimalInfo.AgeText(node.rec).CapitalizeFirst());
            sb.AppendLine(AnimalInfo.StatusText(node.rec, pawn));
            int kids = LineageTracker.Instance?.ChildIdsOf(node.id).Count ?? 0;
            if (kids > 0)
                sb.AppendLine("AFT.Tooltip.Children".Translate(kids));
            sb.AppendLine();
            sb.Append("AFT.Tooltip.Hint".Translate().Colorize(Color.gray));
            return sb.ToString();
        }
    }
}
