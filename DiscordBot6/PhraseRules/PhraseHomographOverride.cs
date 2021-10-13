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
    public struct PhraseHomographOverride {
        public string Pattern { get; set; }
        public string[] Homographs { get; set; }

        public HomographOverrideType OverrideType { get; set; }

        public PhraseHomographOverride(string pattern, string[] homographs, HomographOverrideType overrideType) {
            Pattern = pattern;
            Homographs = homographs;
            OverrideType = overrideType;
        }
    }
}
