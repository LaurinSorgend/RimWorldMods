using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Shared drawing and text helpers for an animal record.</summary>
    public static class AnimalInfo
    {
        /// <summary>Draws the animal's icon: the live pawn if it exists, otherwise its race.</summary>
        public static void DrawIcon(Rect rect, LineageRecord rec, Pawn pawn)
        {
            if (pawn != null && !pawn.Dead)
                Widgets.ThingIcon(rect, pawn);
            else if (rec.Race != null)
                Widgets.DefIcon(rect, rec.Race);
        }

        public static string AgeText(LineageRecord rec)
        {
            string years = rec.AgeYears.ToString("0.#");
            return rec.IsDead ? "AFT.AgeAtDeath".Translate(years) : "AFT.AgeYears".Translate(years);
        }

        /// <summary>One-line description of where the animal is or what happened to it.</summary>
        public static string StatusText(LineageRecord rec, Pawn pawn)
        {
            if (rec.IsDead)
                return "AFT.Status.Dead".Translate();
            if (pawn == null)
                return rec.gone ? "AFT.Status.Gone".Translate() : "AFT.Status.Away".Translate();
            if (pawn.MapHeld != null)
                return "AFT.Status.OnMap".Translate(pawn.MapHeld.Parent.LabelCap);
            Caravan caravan = pawn.GetCaravan();
            if (caravan != null)
                return "AFT.Status.Caravan".Translate(caravan.LabelCap);
            if (pawn.Faction != null && !pawn.Faction.IsPlayer)
                return "AFT.Status.Faction".Translate(pawn.Faction.Name);
            return "AFT.Status.Away".Translate();
        }

        public static string KindAndGender(LineageRecord rec)
        {
            string kind = rec.kind?.LabelCap ?? "?";
            return rec.gender == Gender.None ? kind : kind + ", " + rec.gender.GetLabel(true);
        }
    }
}
