using Discord;
using Discord.WebSocket;
using DiscordBot6.Constraints;
using DiscordBot6.Helpers;
using PCRE;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot6.PhraseRules {
    public struct PhraseRuleConstraints {
        GenericConstraint UserConstraints { get; }
        GenericConstraint ChannelConstraints { get; }
        RoleConstraint RoleConstraints { get; }

        public PhraseRuleConstraints(GenericConstraint userConstraints, GenericConstraint channelConstraints, RoleConstraint roleConstraints) {
            UserConstraints = userConstraints;
            ChannelConstraints = channelConstraints;
            RoleConstraints = roleConstraints;
        }

        public bool MatchesConstriants(ulong userId, ulong channelId, IEnumerable<ulong> roleIds) {
            return UserConstraints.Matches(userId) && ChannelConstraints.Matches(channelId) && RoleConstraints.Matches(roleIds);
        }
    }

    public struct PhraseRulePhraseModifiers {
        public IReadOnlyCollection<PhraseRuleModifier> Modifiers { get; }
        public IReadOnlyCollection<PhraseHomographOverride> HomographOverrides { get; }
        public IReadOnlyCollection<PhraseSubstringModifier> SubstringModifiers { get; }

        public PhraseRulePhraseModifiers(IEnumerable<PhraseRuleModifier> constraints, IEnumerable<PhraseHomographOverride> homographOverrides, IEnumerable<PhraseSubstringModifier> substringModifiers) {
            Modifiers = constraints as IReadOnlyCollection<PhraseRuleModifier>;
            HomographOverrides = homographOverrides as IReadOnlyCollection<PhraseHomographOverride>;
            SubstringModifiers = substringModifiers.OrderByDescending(x => x.SubstringStart) as IReadOnlyCollection<PhraseSubstringModifier>;
        }
    }

    public sealed class PhraseRule {
        private static readonly PcreRegex urlRegex;

        public PhraseRuleConstraints Constraints { get; }

        private readonly PcreRegex regex;

        private bool matchBots = true;
        private bool matchSelf = false;
        private bool matchURLs = true;

        static PhraseRule() {
            urlRegex = new PcreRegex(@"(?:^|\s)\S*https?://\S*$", PcreOptions.Compiled | PcreOptions.Caseless);
        }

        public PhraseRule(ulong serverId, string text, PhraseRuleConstraints constraints, PhraseRulePhraseModifiers phraseModifiers) {
            Constraints = constraints;
            regex = RegexHelper.CreateRegex(text, serverId, phraseModifiers);

            DeriveMetaInformation(phraseModifiers.Modifiers);
        }

        public PhraseRule(string pattern, PcreOptions pcreOptions, PhraseRuleConstraints constraints, IEnumerable<PhraseRuleModifier> phraseModifiers) {
            Constraints = constraints;
            regex = new PcreRegex(pattern, pcreOptions);

            DeriveMetaInformation(phraseModifiers);
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
            SocketGuildUser guildUser = socketMessage.Author as SocketGuildUser;
            SocketGuildChannel guildChannel = socketMessage.Channel as SocketGuildChannel;
            IEnumerable<ulong> roleIds = guildUser.Roles.Select(x => x.Id);

            if (!Constraints.MatchesConstriants(guildUser.Id, guildChannel.Id, roleIds)) { // check to see if the server rule constraints are met
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

        private void DeriveMetaInformation(IEnumerable<PhraseRuleModifier> phraseConstraints) {
            foreach (PhraseRuleModifier modifier in phraseConstraints) {
                switch (modifier.ModifierType) {
                    case PhraseRuleModifierType.MODIFIER_NOT_BOT:
                        matchBots = false;
                        break;

                    case PhraseRuleModifierType.MODIFIER_SELF:
                        matchSelf = true;
                        break;

                    case PhraseRuleModifierType.MODIFIER_NOT_URL:
                        matchURLs = true;
                        break;
                }
            }
        }
    }
}