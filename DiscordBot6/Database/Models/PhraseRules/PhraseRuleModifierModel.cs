using DiscordBot6.PhraseRules;
using System.Collections.Generic;

namespace DiscordBot6.Database.Models.PhraseRules {
    public sealed class PhraseRuleModifierModel : IModelFor<PhraseRuleModifier> {
        public ulong Id { get; set; }
        public int ConstraintType { get; set; }

        public List<string> Data { get; } = new List<string>();

        public PhraseRuleModifier CreateConcrete() {
            return new PhraseRuleModifier((PhraseRuleModifierType)ConstraintType, Data);
        }
    }
}
