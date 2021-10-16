using System.Collections.Generic;

namespace DiscordBot6.Phrases {
    public enum HomographOverrideType {
        OVERRIDE_NO,     // use no homographs for this phrase
        OVERRIDE_ADD,    // add an equivalent character for this phrase
        OVERRIDE_REMOVE, // remove an equivalent character for this phrase
        OVERRIDE_CUSTOM  // override server wide equivalent characters for this phrase
    }

    /// <summary>
    /// Used to add or remove homographs on a phrase-specific basis.
    /// </summary>
    public sealed class PhraseHomographOverride {
        public string Pattern { get; }
        public HomographOverrideType OverrideType { get; }

        public List<string> Homographs { get; } = new List<string>();

        public PhraseHomographOverride(string pattern, HomographOverrideType overrideType) {
            Pattern = pattern;
            OverrideType = overrideType;
        }
    }
}
