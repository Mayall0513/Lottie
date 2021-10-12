using System.Threading.Tasks;

using DiscordBot6.Phrases;

namespace DiscordBot6 {
    public sealed class ServerInformation {
        public ulong Id { get; set; }

        private PhraseRuleSet[] phraseRuleSets;

        public ServerInformation(ulong id) {
            Id = id;
        }

        public async Task<PhraseRuleSet[]> GetPhraseRuleSetsAsync() {
            if (phraseRuleSets == null) {
                // try get from database, if you can't set it to new PhraseRuleSet[0] for no rules - there is no database yet

                phraseRuleSets = new PhraseRuleSet[0];
            }

            return phraseRuleSets;
        }
    }
}
