using UnityEngine;
using Verse;

namespace AnimalFamilyTree
{
    /// <summary>Mod entry point; owns the settings and draws the settings window.</summary>
    public class AnimalFamilyTreeMod : Mod
    {
        public static AftSettings Settings { get; private set; }

        public AnimalFamilyTreeMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<AftSettings>();
        }

        public override string SettingsCategory() => "AFT.SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.Draw(inRect);
        }
    }
}
