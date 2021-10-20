using DiscordBot6.PhraseRules;
using System.Collections.Generic;

namespace DiscordBot6.Database.Models.PhraseRules {
    public sealed class PhraseRuleConstraintModel : IModelFor<PhraseRuleConstraint> {
        public ulong Id { get; set; }
        public int ConstraintType { get; set; }

        public List<string> Data { get; set; } = new List<string>();

        public PhraseRuleConstraint CreateConcrete() {
            return new PhraseRuleConstraint((PhraseRuleConstraintType) ConstraintType, Data);
        }
    }
}
