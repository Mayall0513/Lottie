using System.Linq;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using PCRE;

using DiscordBot6.Helpers;
using DiscordBot6.Database;
using DiscordBot6.ServerRules;
using DiscordBot6.Database.Models.PhraseRules;

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

        public static PhraseRule FromModel(ulong serverId, PhraseRuleModel phraseRuleModel) {
            IEnumerable<ServerRuleConstraint> serverRuleConstraints = Repository.ConvertValues(phraseRuleModel.ServerRules.Values, x => new ServerRuleConstraint((ServerRuleConstraintType)x.ConstraintType, x.Data));
            IEnumerable<PhraseRuleConstraint> phraseRuleConstraints = Repository.ConvertValues(phraseRuleModel.PhraseRules.Values, x => new PhraseRuleConstraint((PhraseRuleConstraintType)x.ConstraintType, x.Data));

            if (phraseRuleModel.ManualPattern) {
                return new PhraseRule(serverId, phraseRuleModel.Pattern, (PcreOptions) (phraseRuleModel.PcreOptions ?? 0), serverRuleConstraints, phraseRuleConstraints);
            }

            else {
                IEnumerable<PhraseHomographOverride> homographOverrides = Repository.ConvertValues(phraseRuleModel.HomographOverrides.Values, x => new PhraseHomographOverride((HomographOverrideType)x.OverrideType, x.Pattern, x.Homographs));
                IEnumerable<PhraseSubstringModifier> substringModifiers = Repository.ConvertValues(phraseRuleModel.SubstringModifiers.Values, x => new PhraseSubstringModifier((SubstringModifierType)x.ModifierType, x.SubstringStart, x.SubstringEnd, x.Data));

                return new PhraseRule(serverId, phraseRuleModel.Text, serverRuleConstraints, phraseRuleConstraints, homographOverrides, substringModifiers);
            }
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