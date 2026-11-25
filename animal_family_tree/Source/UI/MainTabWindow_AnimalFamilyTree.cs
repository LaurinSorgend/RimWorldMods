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

        private static string pendingRootId;

        private readonly AnimalListPanel listPanel = new AnimalListPanel();
        private string rootId;

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
            ApplyPendingRoot();
            LineageTracker.Instance?.Scan();
        }

        public void SelectRoot(string id)
        {
            rootId = id;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect listRect = new Rect(inRect.x, inRect.y, ListWidth, inRect.height);
            Rect treeRect = new Rect(listRect.xMax + Gap, inRect.y, inRect.width - ListWidth - Gap, inRect.height);
            listPanel.Draw(listRect, rootId, SelectRoot);
            DrawTreeArea(treeRect);
        }

        private void DrawTreeArea(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            if (rootId == null)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "AFT.SelectAnimal".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void ApplyPendingRoot()
        {
            if (pendingRootId == null)
                return;
            SelectRoot(pendingRootId);
            pendingRootId = null;
        }
    }
}
