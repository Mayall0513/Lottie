using System;
using System.Collections.Generic;

namespace DiscordBot6.Phrases {
    public enum RuleRequirementType {
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

        MODIFIER_NOT_BOT,    // bot will not delete messages from other bots
        MODIFIER_SELF_DELETE // bot will delete its own message if it matches the ruleset - default is that it won't
    }

    /// <summary>
    /// Defines a rule applied to all of a phrase
    /// </summary>
    public struct PhraseRuleConstraint {
        public RuleRequirementType RequirementType { get; set; }
        public IReadOnlyCollection<string> Data { get; set; }

        public PhraseRuleConstraint(RuleRequirementType requirementType, string[] data) {
            RequirementType = requirementType;
            Data = data;
        }
    }

    public sealed class PhraseRuleConstraintModel {
        public int Id { get; set; }
        public int PhraseRuleId { get; set; }
        public DateTime CreationTime { get; set; }

        public int ConstraintType { get; set; }
        public ICollection<string> Data { get; set; }
    }
}
