using DiscordBot6.Database.Models.ServerRules;
using DiscordBot6.PhraseRules;
using DiscordBot6.ServerRules;
using PCRE;
using System.Collections.Generic;

namespace DiscordBot6.Database.Models.PhraseRules {
    public sealed class PhraseRuleModel : IModelFor<PhraseRule> {
        public ulong Id { get; set; }
        public ulong ServerId { get; set; }

        public string Text { get; set; }
        public bool ManualPattern { get; set; }
        public string Pattern { get; set; }
        public long? PcreOptions { get; set; }

        public Dictionary<ulong, ServerRuleConstraintModel> ServerRules { get; set; } = new Dictionary<ulong, ServerRuleConstraintModel>();
        public Dictionary<ulong, PhraseRuleConstraintModel> PhraseRules { get; set; } = new Dictionary<ulong, PhraseRuleConstraintModel>();
        public Dictionary<ulong, PhraseHomographOverrideModel> HomographOverrides { get; set; } = new Dictionary<ulong, PhraseHomographOverrideModel>();
        public Dictionary<ulong, PhraseSubstringModifierModel> SubstringModifiers { get; set; } = new Dictionary<ulong, PhraseSubstringModifierModel>();

        public PhraseRule CreateConcrete() {
            IEnumerable<ServerRuleConstraint> serverRuleConstraints = Repository.ConvertValues(ServerRules.Values, x => x.CreateConcrete());
            IEnumerable<PhraseRuleConstraint> phraseRuleConstraints = Repository.ConvertValues(PhraseRules.Values, x => x.CreateConcrete());

            if (ManualPattern) {
                return new PhraseRule(Id, Pattern, (PcreOptions)(PcreOptions ?? 0), serverRuleConstraints, phraseRuleConstraints);
            }

            else {
                IEnumerable<PhraseHomographOverride> homographOverrides = Repository.ConvertValues(HomographOverrides.Values, x => x.CreateConcrete());
                IEnumerable<PhraseSubstringModifier> substringModifiers = Repository.ConvertValues(SubstringModifiers.Values, x => x.CreateConcrete());

                return new PhraseRule(Id, Text, serverRuleConstraints, phraseRuleConstraints, homographOverrides, substringModifiers);
            }
        }
    }
}
