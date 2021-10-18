using System.Threading.Tasks;

using DiscordBot6.Database;
using DiscordBot6.PhraseRules;

namespace DiscordBot6 {
    public sealed class ServerInformation {
        public ulong Id { get; set; }

        private PhraseRule[] phraseRules;

        public ServerInformation(ulong id) {
            Id = id;
        }

        public async Task<PhraseRule[]> GetPhraseRuleSetsAsync() {
            if (phraseRules == null) {
                phraseRules = await Repository.GetPhraseRules(Id);
            }

            return phraseRules;
        }
    }
}
