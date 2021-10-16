﻿using System;
using System.Collections.Generic;

namespace DiscordBot6.Phrases {
    public enum SubstringModifierType {
        MODIFIER_CHARACTERCOUNT_EXACT,   // Require exactly the amount of characters given in the phrase
        MODIFIER_CHARACTERCOUNT_MINIMUM, // Require atleast the amount of characters given in the phrase
        MODIFIER_CHARACTERCOUNT_MAXIMUM, // Require less than or the amount of characters given in the phrase

        MODIFIER_NO_HOMOGRAPHS,     // match only exactly what the user gave
        MODIFIER_ADD_HOMOGRAPHS,    // add extra homographs
        MODIFIER_REMOVE_HOMOGRAPHS, // remove homographs
        MODIFIER_CUSTOM_HOMOGRAPHS  // override server wide and phrase wide homographs
    }

    /// <summary>
    /// Defines a rule that is only applied on a specific substring of a phrase.
    /// </summary>
    public class PhraseSubstringModifier : IComparable<PhraseSubstringModifier> {
        public int SubstringStart { get; }
        public int SubstringEnd { get; }
        public SubstringModifierType ModifierType { get; }

        public List<string> Data { get; } = new List<string>();

        public PhraseSubstringModifier(int substringStart, int substringEnd, SubstringModifierType modifierType) {
            SubstringStart = substringStart;
            SubstringEnd = substringEnd;
            ModifierType = modifierType;
        }

        public int CompareTo(PhraseSubstringModifier otherModifier) {
            return SubstringStart - otherModifier.SubstringStart;
        }
    }
}
