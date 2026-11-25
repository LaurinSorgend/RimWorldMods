using UnityEngine;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>User settings for tree size and how the dead are shown.</summary>
    public class AftSettings : ModSettings
    {
        public int generationsUp = 3;
        public int generationsDown = 3;
        public int maxNodes = 150;
        public bool deadAsTombstone = true;
        public bool trackFormerColonyAnimals = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref generationsUp, "generationsUp", 3);
            Scribe_Values.Look(ref generationsDown, "generationsDown", 3);
            Scribe_Values.Look(ref maxNodes, "maxNodes", 150);
            Scribe_Values.Look(ref deadAsTombstone, "deadAsTombstone", true);
            Scribe_Values.Look(ref trackFormerColonyAnimals, "trackFormerColonyAnimals", true);
        }

        /// <summary>Draws the mod settings page.</summary>
        public void Draw(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect.LeftPart(0.6f));
            list.Label("AFT.Settings.GenerationsUp".Translate(generationsUp));
            generationsUp = Mathf.RoundToInt(list.Slider(generationsUp, 1, 8));
            list.Label("AFT.Settings.GenerationsDown".Translate(generationsDown));
            generationsDown = Mathf.RoundToInt(list.Slider(generationsDown, 1, 8));
            list.Label("AFT.Settings.MaxNodes".Translate(maxNodes));
            maxNodes = Mathf.RoundToInt(list.Slider(maxNodes, 30, 400) / 10f) * 10;
            list.Gap();
            list.CheckboxLabeled("AFT.Settings.DeadAsTombstone".Translate(), ref deadAsTombstone,
                "AFT.Settings.DeadAsTombstoneTip".Translate());
            list.CheckboxLabeled("AFT.Settings.TrackFormer".Translate(), ref trackFormerColonyAnimals,
                "AFT.Settings.TrackFormerTip".Translate());
            list.End();
        }
    }
}
