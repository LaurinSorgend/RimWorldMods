using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>The "Animal family" bottom-bar tab: animal list on the left, family tree on the right.</summary>
    public class MainTabWindow_AnimalFamilyTree : MainTabWindow
    {
        private const float ListWidth = 260f;
        private const float Gap = 10f;
        private const float HeaderHeight = 30f;
        private const float FooterHeight = 22f;

        private static string pendingRootId;

        private readonly AnimalListPanel listPanel = new AnimalListPanel();
        private readonly TreeCanvas canvas = new TreeCanvas();
        private readonly Stack<string> history = new Stack<string>();
        private string rootId;
        private bool showLegend;

        private FamilyGraph graph;
        private TreeLayout layout;
        private string builtKey;
        private bool buildFailed;

        public MainTabWindow_AnimalFamilyTree()
        {
            canvas.OnReroot = id => SelectRoot(id, true);
            canvas.OnContextMenu = node => NodeMenu.Open(node, id => SelectRoot(id, true));
        }

        public override Vector2 RequestedTabSize =>
            new Vector2(Mathf.Min(1400f, UI.screenWidth), Mathf.Min(820f, UI.screenHeight - 35f));

        /// <summary>Opens the tab with the given animal as tree root.</summary>
        public static void OpenFor(string id)
        {
            pendingRootId = id;
            Find.MainTabsRoot.SetCurrentTab(AftDefOf.AFT_AnimalFamilyTree);
            if (Find.WindowStack.WindowOfType<MainTabWindow_AnimalFamilyTree>() is MainTabWindow_AnimalFamilyTree w)
                w.ApplyPendingRoot();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            LineageTracker.Instance?.Scan();
            ApplyPendingRoot();
        }

        /// <summary>Shows the tree of the given animal; remembers the previous root for "Back".</summary>
        public void SelectRoot(string id, bool rememberPrevious)
        {
            if (id == rootId)
                return;
            if (rememberPrevious && rootId != null)
                history.Push(rootId);
            rootId = id;
            canvas.SelectedId = id;
            canvas.ResetView();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect listRect = new Rect(inRect.x, inRect.y, ListWidth, inRect.height);
            Rect treeRect = new Rect(listRect.xMax + Gap, inRect.y, inRect.width - ListWidth - Gap, inRect.height);
            listPanel.Draw(listRect, rootId, id => SelectRoot(id, true));
            DrawTreeArea(treeRect);
        }

        private void DrawTreeArea(Rect rect)
        {
            Rect header = new Rect(rect.x, rect.y, rect.width, HeaderHeight);
            Rect footer = new Rect(rect.x, rect.yMax - FooterHeight, rect.width, FooterHeight);
            Rect body = new Rect(rect.x, header.yMax + 4f, rect.width, footer.y - header.yMax - 8f);
            DrawHeader(header);
            Widgets.DrawMenuSection(body);
            EnsureTree();
            if (graph == null)
                DrawCenteredMessage(body, EmptyMessage());
            else
                canvas.Draw(body.ContractedBy(1f), graph, layout);
            if (showLegend)
                Legend.Draw(body);
            DrawFooter(footer);
        }

        private void DrawHeader(Rect header)
        {
            var button = new Rect(header.x, header.y, 110f, header.height);
            if (history.Count > 0 && Widgets.ButtonText(button, "AFT.Back".Translate()))
                rootId = PopHistory();
            button.x += button.width + 6f;
            if (graph != null && Widgets.ButtonText(button, "AFT.ResetView".Translate()))
                canvas.ResetView();
            button.x = header.xMax - button.width;
            if (Widgets.ButtonText(button, "AFT.Legend".Translate()))
                showLegend = !showLegend;
        }

        private string PopHistory()
        {
            string id = history.Pop();
            canvas.SelectedId = id;
            canvas.ResetView();
            return id;
        }

        private void DrawFooter(Rect footer)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleLeft;
            string text = "AFT.MouseControls".Translate();
            if (graph != null && graph.truncated)
                text = "AFT.Truncated".Translate(AnimalFamilyTreeMod.Settings.maxNodes) + "    " + text;
            Widgets.Label(footer, text);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private string EmptyMessage()
        {
            if (rootId == null)
                return "AFT.SelectAnimal".Translate();
            string name = LineageTracker.Instance?.GetRecord(rootId)?.DisplayName ?? "?";
            return buildFailed ? "AFT.TreeError".Translate(name) : "AFT.SelectAnimal".Translate();
        }

        private static void DrawCenteredMessage(Rect rect, string message)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect.ContractedBy(20f), message);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>Rebuilds graph and layout when the root, records or settings changed.</summary>
        private void EnsureTree()
        {
            var tracker = LineageTracker.Instance;
            var s = AnimalFamilyTreeMod.Settings;
            string key = $"{rootId}|{tracker?.Version}|{s.generationsUp}|{s.generationsDown}|{s.maxNodes}";
            if (key == builtKey)
                return;
            builtKey = key;
            graph = null;
            layout = null;
            buildFailed = false;
            if (tracker == null || rootId == null)
                return;
            try
            {
                graph = FamilyGraph.Build(tracker, rootId, s);
                layout = graph != null ? TreeLayout.Build(graph) : null;
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"[Animal Family Tree] Could not build tree for {rootId}: {ex}", rootId.GetHashCode());
                graph = null;
                layout = null;
                buildFailed = true;
            }
        }

        private void ApplyPendingRoot()
        {
            if (pendingRootId == null)
                return;
            SelectRoot(pendingRootId, true);
            pendingRootId = null;
        }
    }
}
