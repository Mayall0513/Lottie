using System;
using System.Linq;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using PCRE;

using DiscordBot6.Rules;

namespace DiscordBot6.Phrases {
    public sealed class PhraseRule : ServerRule {
        public string Text { get; private set; }

        public IReadOnlyCollection<PhraseRuleConstraint> Constraints { get; private set; }
        public IReadOnlyCollection<PhraseHomographOverride> HomographOverrides { get; private set; }
        public IReadOnlyCollection<PhraseSubstringModifier> SubstringModifiers { get; private set; }

        public bool ManualPattern { get; private set; }
        public string Pattern { get; private set; }

        private PcreRegex regex;

        private bool botDelete = true;
        private bool selfDelete = false;

        public PhraseRule(ulong serverId, string text, ServerRuleConstraint[] serverRuleConstraints, PhraseRuleConstraint[] phraseRuleConstraints, PhraseHomographOverride[] homographOverrides, PhraseSubstringModifier[] substringModifiers) : base(serverId, serverRuleConstraints) {
            Text = text;
            Constraints = phraseRuleConstraints;
            HomographOverrides = homographOverrides;

            Array.Sort(substringModifiers);
            SubstringModifiers = substringModifiers;

            regex = PhraseRegexBuilder.ConstructRegex(serverId, this);
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

            if (CanApply(socketMessage.Author.Id, guildChannel.Id, roleIds)) {
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

        public static PhraseRuleFactory FromFactory(string text) {
            return new PhraseRuleFactory(text);
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
        public ulong Id { get; set; }
        public ulong ServerId { get; set; }
        public DateTime CreationTime { get; set; }

        public string Text { get; set; }
        public bool ManualPattern { get; set; }
        public string Pattern { get; set; }
        public bool BotDelete { get; set; }
        public bool SelfDelete { get; set; }
    }

    public sealed class PhraseRuleFactory {
        private readonly string text;

        private readonly List<ServerRuleConstraint> serverRuleConstraints = new List<ServerRuleConstraint>();
        private readonly List<PhraseRuleConstraint> ruleRequirements = new List<PhraseRuleConstraint>();
        private readonly List<PhraseHomographOverride> ruleHomographOverrides = new List<PhraseHomographOverride>();
        private readonly List<PhraseSubstringModifier> substringModifiers = new List<PhraseSubstringModifier>();

        public PhraseRuleFactory(string text) {
            this.text = text;
        }

        public PhraseRuleFactory WithServerRuleRequirement(ServerRuleConstraint ruleConstraint) {
            serverRuleConstraints.Add(ruleConstraint);
            return this;
        }

        public PhraseRuleFactory WithPhraseRuleRequirement(PhraseRuleConstraint ruleConstraint) {
            ruleRequirements.Add(ruleConstraint);
            return this;
        }

        public PhraseRuleFactory WithHomographOverride(PhraseHomographOverride homographOverride) {
            ruleHomographOverrides.Add(homographOverride);
            return this;
        }

        public PhraseRuleFactory WithRuleSubstringModifier(PhraseSubstringModifier substringModifier) {
            substringModifiers.Add(substringModifier);
            return this;
        }

        public PhraseRule Build(ulong serverId) {
            return new PhraseRule(serverId, text, serverRuleConstraints.ToArray(), ruleRequirements.ToArray(), ruleHomographOverrides.ToArray(), substringModifiers.ToArray());
        }
    }
}