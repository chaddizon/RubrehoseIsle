using UnityEngine;

namespace Rubrehose.Core
{
    // Runtime-visible mirror of a few palette colors from rubrehose_prototype.html.
    // RubrehoseEditorUtils (Assets/Editor) keeps its own copies of Ink/Cream/Purple/Teal
    // since editor-only code can't be referenced from these Assets/Scripts runtime
    // classes — keep both in sync by hand if the palette changes. WarningRed has no
    // Editor-side counterpart yet; it exists only for the fight timer's low-time cue.
    public static class Palette
    {
        public static readonly Color Ink = new Color32(26, 26, 26, 255);
        public static readonly Color Cream = new Color32(244, 239, 224, 255);
        public static readonly Color Teal = new Color32(29, 158, 117, 255);
        public static readonly Color WarningRed = new Color32(196, 64, 48, 255);
    }
}
