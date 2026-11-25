using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Left panel: search box, species and life filters, scrollable animal list.</summary>
    public class AnimalListPanel
    {
        private enum LifeFilter { Alive, Dead, All }

        private const float RowHeight = 40f;
        private const float ControlHeight = 28f;

        private string search = "";
        private ThingDef species;
        private LifeFilter life = LifeFilter.Alive;
        private bool includeRelatives;
        private Vector2 scroll;

        private List<LineageRecord> cached = new List<LineageRecord>();
        private string cacheKey;

        public void Draw(Rect rect, string selectedId, Action<string> onSelect)
        {
            var tracker = LineageTracker.Instance;
            if (tracker == null)
                return;
            Rect controls = DrawControls(rect, tracker);
            Rect listRect = new Rect(rect.x, controls.yMax + 4f, rect.width, rect.yMax - controls.yMax - 4f);
            RefreshCache(tracker);
            DrawList(listRect, tracker, selectedId, onSelect);
        }

        private Rect DrawControls(Rect rect, LineageTracker tracker)
        {
            Rect row = new Rect(rect.x, rect.y, rect.width, ControlHeight);
            search = Widgets.TextField(row, search);
            if (search.NullOrEmpty())
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(row.ContractedBy(6f, 0f), "AFT.SearchPlaceholder".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            row.y += ControlHeight + 4f;
            Rect left = row.LeftHalf().ContractedBy(0f, 0f);
            if (Widgets.ButtonText(new Rect(left.x, left.y, left.width - 2f, left.height), SpeciesLabel()))
                OpenSpeciesMenu(tracker);
            Rect right = row.RightHalf();
            if (Widgets.ButtonText(new Rect(right.x + 2f, right.y, right.width - 2f, right.height), LifeLabel()))
                life = (LifeFilter)(((int)life + 1) % 3);

            row.y += ControlHeight + 4f;
            row.height = 24f;
            Widgets.CheckboxLabeled(row, "AFT.Filter.Relatives".Translate(), ref includeRelatives);
            TooltipHandler.TipRegion(row, "AFT.Filter.RelativesTip".Translate());
            return row;
        }

        private string SpeciesLabel()
        {
            return species == null ? "AFT.AllSpecies".Translate().ToString() : species.LabelCap.ToString();
        }

        private string LifeLabel()
        {
            return ("AFT.Filter." + life).Translate();
        }

        private void OpenSpeciesMenu(LineageTracker tracker)
        {
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("AFT.AllSpecies".Translate(), () => species = null)
            };
            IEnumerable<ThingDef> races = tracker.AllRecords
                .Where(r => r.Race != null && (includeRelatives || r.wasColony))
                .Select(r => r.Race).Distinct().OrderBy(d => d.label);
            foreach (ThingDef race in races)
            {
                ThingDef captured = race;
                options.Add(new FloatMenuOption(race.LabelCap, () => species = captured));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void RefreshCache(LineageTracker tracker)
        {
            string key = $"{tracker.Version}|{search}|{species?.defName}|{life}|{includeRelatives}";
            if (key == cacheKey)
                return;
            cacheKey = key;
            cached = tracker.AllRecords.Where(Matches)
                .OrderBy(r => r.kind?.label ?? "")
                .ThenBy(r => r.DisplayName)
                .ToList();
        }

        private bool Matches(LineageRecord rec)
        {
            if (!includeRelatives && !rec.wasColony)
                return false;
            if (species != null && rec.Race != species)
                return false;
            bool alive = !rec.IsDead && !rec.gone;
            if ((life == LifeFilter.Alive && !alive) || (life == LifeFilter.Dead && alive))
                return false;
            return search.NullOrEmpty()
                || rec.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || (rec.kind?.label ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawList(Rect rect, LineageTracker tracker, string selectedId, Action<string> onSelect)
        {
            Widgets.DrawMenuSection(rect);
            Rect outRect = rect.ContractedBy(2f);
            if (cached.Count == 0)
            {
                Widgets.Label(outRect.ContractedBy(6f), "AFT.NoMatches".Translate());
                return;
            }
            Rect view = new Rect(0f, 0f, outRect.width - 16f, cached.Count * RowHeight);
            Widgets.BeginScrollView(outRect, ref scroll, view);
            for (int i = 0; i < cached.Count; i++)
            {
                Rect row = new Rect(0f, i * RowHeight, view.width, RowHeight);
                if (row.yMax < scroll.y || row.y > scroll.y + outRect.height)
                    continue;
                DrawRow(row, cached[i], tracker, cached[i].id == selectedId, onSelect);
            }
            Widgets.EndScrollView();
        }

        private static void DrawRow(Rect row, LineageRecord rec, LineageTracker tracker, bool selected,
            Action<string> onSelect)
        {
            if (selected)
                Widgets.DrawHighlightSelected(row);
            else if (Mouse.IsOver(row))
                Widgets.DrawHighlight(row);
            Pawn pawn = tracker.TryGetLivePawn(rec.id);
            AnimalInfo.DrawIcon(new Rect(row.x + 4f, row.y + 4f, 32f, 32f), rec, pawn);
            Rect text = new Rect(row.x + 42f, row.y + 2f, row.width - 44f, row.height - 4f);
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(text.TopHalf(), rec.DisplayName.Truncate(text.width));
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(text.BottomHalf(), AnimalInfo.KindAndGender(rec).Truncate(text.width));
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            if (Widgets.ButtonInvisible(row))
                onSelect(rec.id);
        }
    }
}
