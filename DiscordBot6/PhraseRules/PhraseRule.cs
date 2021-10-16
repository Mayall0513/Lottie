using System;
using System.Linq;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using PCRE;

using DiscordBot6.Rules;

namespace DiscordBot6.Phrases {
    public sealed class PhraseRule : ServerRule {
        public string Text { get; }

        public IReadOnlyCollection<PhraseRuleConstraint> Constraints { get; }
        public IReadOnlyCollection<PhraseHomographOverride> HomographOverrides { get; }
        public IReadOnlyCollection<PhraseSubstringModifier> SubstringModifiers { get; }

        public bool ManualPattern { get; }
        public string Pattern { get; }

        private PcreRegex regex;

        private bool botDelete = true;
        private bool selfDelete = false;

        public PhraseRule(ulong serverId, string text, ServerRuleConstraint[] serverRuleConstraints, PhraseRuleConstraint[] phraseRuleConstraints, PhraseHomographOverride[] homographOverrides, PhraseSubstringModifier[] substringModifiers) : base(serverId, serverRuleConstraints) {
            Text = text;
            Constraints = phraseRuleConstraints;
            HomographOverrides = homographOverrides;

            Array.Sort(substringModifiers);
            SubstringModifiers = substringModifiers;

            regex = PhraseRegexBuilder.CreateRegex(this);
            Pattern = regex.PatternInfo.PatternString;

            DeriveMetaInformation();
        }

        public PhraseRule(ulong serverId, string pattern, PcreOptions pcreOptions, ServerRuleConstraint[] serverRuleConstraints, PhraseRuleConstraint[] phraseRuleConstraints) : base(serverId, serverRuleConstraints) {
            ManualPattern = true;
            Constraints = phraseRuleConstraints;

            regex = new PcreRegex(pattern, pcreOptions);
            Pattern = pattern;

            DeriveMetaInformation();
        }

        public bool Matches(string text) {
            return regex.IsMatch(text);
        }

        public bool CanApply(SocketMessage socketMessage) {
            SocketGuildChannel guildChannel = socketMessage.Channel as SocketGuildChannel;
            SocketGuildUser guildUser = socketMessage.Author as SocketGuildUser;
            IReadOnlyCollection<ulong> roleIds = guildUser.Roles.Select(x => x.Id).ToArray();

            if (!CanApply(socketMessage.Author.Id, guildChannel.Id, roleIds)) {
                return false;
            }

            if (!botDelete && socketMessage.Source == MessageSource.Bot) {
                return false;
            }

            if (!selfDelete && socketMessage.Author.Id == Program.BotAccountId) {
                return false;
            }

            return true;
        }

        private void DeriveMetaInformation() {
            foreach (PhraseRuleConstraint phraseRuleModifier in Constraints) {
                switch (phraseRuleModifier.RequirementType) {
                    case RuleRequirementType.MODIFIER_NOT_BOT:
                        botDelete = false;
                        break;

                    case RuleRequirementType.MODIFIER_SELF_DELETE:
                        selfDelete = true;
                        break;
                }
            }
        }
    }

    public sealed class PhraseRuleModel {
        public string Text { get; set; }
        public bool ManualPattern { get; set; }
        public string Pattern { get; set; }
        public long? PcreOptions { get; set; }
        public bool BotDelete { get; set; }
        public bool SelfDelete { get; set; }

        public Dictionary<ulong, ServerRuleConstraint> ServerRules { get; set; } = new Dictionary<ulong, ServerRuleConstraint>();
        public Dictionary<ulong, PhraseRuleConstraint> Constraints { get; set; } = new Dictionary<ulong, PhraseRuleConstraint>();
        public Dictionary<ulong, PhraseHomographOverride> HomographOverrides { get; set; } = new Dictionary<ulong, PhraseHomographOverride>();
        public Dictionary<ulong, PhraseSubstringModifier> SubstringModifiers { get; set; } = new Dictionary<ulong, PhraseSubstringModifier>();

        public PhraseRuleModel(string text, bool manualPattern, string pattern, long pcreOptions, bool botDelete, bool selfDelete) {
            Text = text;
            ManualPattern = manualPattern;
            Pattern = pattern;
            PcreOptions = pcreOptions;
            BotDelete = botDelete;
            SelfDelete = selfDelete;
        }
    }
}