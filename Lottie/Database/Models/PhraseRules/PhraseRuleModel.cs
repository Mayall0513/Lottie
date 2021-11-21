using Lottie.Constraints;
using Lottie.PhraseRules;
using PCRE;
using System.Collections.Generic;

namespace Lottie.Database.Models.PhraseRules {
    public sealed class PhraseRuleModel : IModelFor<PhraseRule> {
        public ulong Id { get; set; }
        public ulong ServerId { get; set; }

        public string Text { get; set; }
        public bool ManualPattern { get; set; }
        public string Pattern { get; set; }
        public long? PcreOptions { get; set; }

        public CRUConstraints Constraints { get; set; }

        public Dictionary<ulong, PhraseRuleModifierModel> PhraseRules { get; } = new Dictionary<ulong, PhraseRuleModifierModel>();
        public Dictionary<ulong, PhraseHomographOverrideModel> HomographOverrides { get; } = new Dictionary<ulong, PhraseHomographOverrideModel>();
        public Dictionary<ulong, PhraseSubstringModifierModel> SubstringModifiers { get; } = new Dictionary<ulong, PhraseSubstringModifierModel>();

        public PhraseRule CreateConcrete() {
            IEnumerable<PhraseRuleModifier> phraseRuleModifiers = Repository.ConvertValues(PhraseRules.Values, x => x.CreateConcrete());

            if (Pattern == null) {
                return new PhraseRule(Pattern, (PcreOptions) (PcreOptions ?? 0), Constraints, phraseRuleModifiers);
            }

            else {
                IEnumerable<PhraseHomographOverride> homographOverrides = Repository.ConvertValues(HomographOverrides.Values, x => x.CreateConcrete());
                IEnumerable<PhraseSubstringModifier> substringModifiers = Repository.ConvertValues(SubstringModifiers.Values, x => x.CreateConcrete());

                PhraseRulePhraseModifiers phraseRulePhraseModifiers = new PhraseRulePhraseModifiers(phraseRuleModifiers, homographOverrides, substringModifiers);

                return new PhraseRule(Id, Text, Constraints, phraseRulePhraseModifiers);
            }
        }
    }
}
