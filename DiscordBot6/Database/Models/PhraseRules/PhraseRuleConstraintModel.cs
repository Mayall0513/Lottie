using System.Collections.Generic;

namespace DiscordBot6.Database.Models.PhraseRules {
    public sealed class PhraseRuleConstraintModel {
        public int ConstraintType { get; set; }

        public List<string> Data { get; set; } = new List<string>();

        public PhraseRuleConstraintModel(int constraintType) {
            ConstraintType = constraintType;
        }
    }
}
