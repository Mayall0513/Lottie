using System.Collections.Generic;

namespace Lottie.PhraseRules {
    public enum HomographOverrideType {
        OVERRIDE_NO,     // use no homographs for this phrase
        OVERRIDE_ADD,    // add an equivalent character for this phrase
        OVERRIDE_REMOVE, // remove an equivalent character for this phrase
        OVERRIDE_CUSTOM  // override server wide equivalent characters for this phrase
    }

    public struct PhraseHomographOverride {
        public HomographOverrideType OverrideType { get; }

        public string Pattern { get; }
        public IReadOnlyCollection<string> Homographs { get; }

        public PhraseHomographOverride(HomographOverrideType overrideType, string pattern, IEnumerable<string> homographs) {
            OverrideType = overrideType;
            Pattern = pattern;
            Homographs = homographs as IReadOnlyCollection<string>;
        }
    }
}
