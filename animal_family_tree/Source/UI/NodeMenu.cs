using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Right-click menu for a tree node.</summary>
    public static class NodeMenu
    {
        public static void Open(FamilyNode node, Action<string> viewTree)
        {
            Pawn pawn = LineageTracker.Instance?.TryGetLivePawn(node.id);
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("AFT.ViewTree".Translate(), () => viewTree(node.id)),
                LocateOption(node.rec, pawn)
            };
            FloatMenuOption info = InfoCardOption(node.rec, pawn);
            if (info != null)
                options.Add(info);
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static FloatMenuOption LocateOption(LineageRecord rec, Pawn pawn)
        {
            Thing target = pawn == null ? null : pawn.Dead ? (Thing)pawn.Corpse : pawn;
            GlobalTargetInfo info = target != null ? new GlobalTargetInfo(target) : GlobalTargetInfo.Invalid;
            if (target == null || !CameraJumper.CanJump(info))
            {
                return new FloatMenuOption("AFT.Locate".Translate() + " (" +
                    "AFT.CannotLocate".Translate(rec.DisplayName) + ")", null);
            }
            return new FloatMenuOption("AFT.Locate".Translate(), () =>
            {
                Find.MainTabsRoot.EscapeCurrentTab(false);
                CameraJumper.TryJumpAndSelect(info);
            });
        }

        private static FloatMenuOption InfoCardOption(LineageRecord rec, Pawn pawn)
        {
            if (pawn != null && !pawn.Discarded)
                return new FloatMenuOption("AFT.InfoCard".Translate(), () => Find.WindowStack.Add(new Dialog_InfoCard(pawn)));
            if (rec.Race != null)
                return new FloatMenuOption("AFT.InfoCard".Translate(), () => Find.WindowStack.Add(new Dialog_InfoCard(rec.Race)));
            return null;
        }
    }
}
