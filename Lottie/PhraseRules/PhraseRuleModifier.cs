using System.Collections.Generic;

namespace Lottie.PhraseRules {
    public enum PhraseRuleModifierType {
        MODIFIER_WORD,         // must be a standalone word
        MODIFIER_WORDSTART,    // must be at the start of a word
        MODIFIER_WORDEND,      // must be at the end of a word
        MODIFIER_MESSAGE,      // must be the whole message
        MODIFIER_MESSAGESTART, // must be at the start of the message
        MODIFIER_MESSAGEEND,   // must be at the end of a message

        MODIFIER_NOT_WORDSTART,    // must not be at the start of a word
        MODIFIER_NOT_WORDEND,      // must not be at the end of a word
        MODIFIER_NOT_MESSAGESTART, // must not be at the start of a message
        MODIFIER_NOT_MESSAGEEND,   // must not be at the end of a message

        MODIFIER_NOT_BEFORE, // must not be before specific text
        MODIFIER_NOT_AFTER,  // must not be after specific text#

        MODIFIER_CASESENSITIVE, // default is case insensitive

        MODIFIER_SELF,     // bot will match its own message if it matches the ruleset - default is that it won't
        MODIFIER_NOT_BOT,  // bot will not match messages from other bots
        MODIFIER_NOT_URL,  // bot will not match messages inside of URLs
    }

    /// <summary>
    /// Defines a rule applied to all of a phrase
    /// </summary>
    public struct PhraseRuleModifier {
        public PhraseRuleModifierType ModifierType { get; }

        public IReadOnlyCollection<string> Data { get; }

        public PhraseRuleModifier(PhraseRuleModifierType modifierType, IEnumerable<string> data) {
            ModifierType = modifierType;
            Data = data as IReadOnlyCollection<string>;
        }
    }
}
