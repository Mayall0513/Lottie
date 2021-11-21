using System.Collections.Generic;

namespace Lottie.PhraseRules {
    public enum SubstringModifierType {
        MODIFIER_CHARACTERCOUNT_EXACT,   // Require exactly the amount of characters given in the phrase
        MODIFIER_CHARACTERCOUNT_MINIMUM, // Require atleast the amount of characters given in the phrase
        MODIFIER_CHARACTERCOUNT_MAXIMUM, // Require less than or the amount of characters given in the phrase

        MODIFIER_HOMOGRAPHS_NO,     // match only exactly what the user gave
        MODIFIER_HOMOGRAPHS_ADD,    // add extra homographs
        MODIFIER_HOMOGRAPHS_REMOVE, // remove homographs
        MODIFIER_HOMOGRAPHS_CUSTOM  // override server wide and phrase wide homographs
    }

    /// <summary>
    /// Defines a rule that is only applied on a specific substring of a phrase.
    /// </summary>
    public struct PhraseSubstringModifier {
        public SubstringModifierType ModifierType { get; }
        public int SubstringStart { get; }
        public int SubstringEnd { get; }

        public IReadOnlyCollection<string> Data { get; }

        public PhraseSubstringModifier(SubstringModifierType modifierType, int substringStart, int substringEnd, IEnumerable<string> data) {
            ModifierType = modifierType;
            SubstringStart = substringStart;
            SubstringEnd = substringEnd;
            Data = data as IReadOnlyCollection<string>;
        }
    }
}
