using Discord;
using Discord.WebSocket;
using DiscordBot6.Database;
using DiscordBot6.Database.Models.PhraseRules;
using DiscordBot6.Helpers;
using DiscordBot6.ServerRules;
using PCRE;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot6.PhraseRules {
    public sealed class PhraseRule : ServerRule {
        private static PcreRegex urlRegex;

        public string Text { get; }

        public IReadOnlyCollection<PhraseRuleConstraint> PhraseConstraints { get; }
        public IReadOnlyCollection<PhraseHomographOverride> HomographOverrides { get; }
        public IReadOnlyCollection<PhraseSubstringModifier> SubstringModifiers { get; }

        public bool ManualPattern { get; }
        public string Pattern => regex.PatternInfo.PatternString;

        private PcreRegex regex;

        private bool matchBots = true;
        private bool matchSelf = false;
        private bool matchURLs = true;

        static PhraseRule() {
            urlRegex = new PcreRegex(@"(?:^|\s)\S*https?://\S*$", PcreOptions.Compiled | PcreOptions.Caseless);
        }

        public PhraseRule(ulong serverId, string text, IEnumerable<ServerRuleConstraint> serverRuleConstraints, IEnumerable<PhraseRuleConstraint> phraseRuleConstraints, IEnumerable<PhraseHomographOverride> homographOverrides, IEnumerable<PhraseSubstringModifier> substringModifiers) : base(serverId, serverRuleConstraints) {
            Text = text;
            PhraseConstraints = phraseRuleConstraints as IReadOnlyCollection<PhraseRuleConstraint>;
            HomographOverrides = homographOverrides as IReadOnlyCollection<PhraseHomographOverride>;
            SubstringModifiers = substringModifiers.OrderByDescending(modifier => modifier.SubstringStart) as IReadOnlyCollection<PhraseSubstringModifier>;

            regex = RegexHelper.CreateRegex(this);
            DeriveMetaInformation();
        }

        public PhraseRule(ulong serverId, string pattern, PcreOptions pcreOptions, IEnumerable<ServerRuleConstraint> serverRuleConstraints, IEnumerable<PhraseRuleConstraint> phraseRuleConstraints) : base(serverId, serverRuleConstraints) {
            ManualPattern = true;
            PhraseConstraints = phraseRuleConstraints as IReadOnlyCollection<PhraseRuleConstraint>;

            regex = new PcreRegex(pattern, pcreOptions);
            DeriveMetaInformation();
        }

        public bool Matches(string text) {
            if (matchURLs) {
                return regex.IsMatch(text);
            }

            else {
                foreach (PcreMatch match in regex.Matches(text)) {
                    if (match.Success && !urlRegex.IsMatch(text.Substring(0, match.Index))) {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool CanApply(SocketMessage socketMessage) {
            SocketGuildChannel guildChannel = socketMessage.Channel as SocketGuildChannel;
            SocketGuildUser guildUser = socketMessage.Author as SocketGuildUser;
            IReadOnlyCollection<ulong> roleIds = guildUser.Roles.Select(x => x.Id).ToArray();

            if (!base.CanApply(socketMessage.Author.Id, guildChannel.Id, roleIds)) { // check to see if the server rule constraints are met
                return false;
            }

            if (!matchBots && socketMessage.Source == MessageSource.Bot) { // we're not allowed to check bots
                return false;
            }

            if (!matchSelf && socketMessage.Author.Id == Program.BotAccountId) { // we're not allowed to check ourselves
                return false;
            }

            return true;
        }

        private void DeriveMetaInformation() {
            foreach (PhraseRuleConstraint phraseRuleModifier in PhraseConstraints) {
                switch (phraseRuleModifier.ConstraintType) {
                    case PhraseRuleConstraintType.MODIFIER_NOT_BOT:
                        matchBots = false;
                        break;

                    case PhraseRuleConstraintType.MODIFIER_NOT_SELF:
                        matchSelf = true;
                        break;

                    case PhraseRuleConstraintType.MODIFIER_NOT_URL:
                        matchURLs = false;
                        break;
                }
            }
        }
    }
}