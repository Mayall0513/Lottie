using System;
using System.Linq;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using PCRE;

namespace DiscordBot6.Phrases {
    public sealed class PhraseRuleSet {
        public ulong ServerId { get; private set; }
        public string Text { get; private set; }

        public PhraseRuleRequirement[] Requirements { get; private set; }
        public PhraseHomographOverride[] HomographOverrides { get; private set; }
        public PhraseSubstringModifier[] SubstringModifiers { get; private set; }

        public bool ManualPattern { get; private set; }
        public string Pattern { get; private set; }

        private PcreRegex regex;

        private ulong[] channelWhitelist = null;
        private ulong[] channelBlacklist = null;

        private ulong[] roleWhitelist = null;
        private ulong[] roleBlacklist = null;

        private bool roleWhitelistAny = true;  // false = all
        private bool roleBlacklistAny = false; // false = all

        private ulong[] userWhitelist = null;
        private ulong[] userBlacklist = null;

        private bool botDelete = true;
        private bool selfDelete = false;

        public PhraseRuleSet(ulong serverId, string text, PhraseRuleRequirement[] ruleRequirements, PhraseHomographOverride[] ruleHomographOverrides, PhraseSubstringModifier[] phraseRuleSubstringModifiers) {
            ServerId = serverId;
            Text = text;
            Requirements = ruleRequirements;
            HomographOverrides = ruleHomographOverrides;
            SubstringModifiers = phraseRuleSubstringModifiers;

            Array.Sort(SubstringModifiers);

            regex = PhraseRegexBuilder.ConstructRegex(this);
            Pattern = regex.PatternInfo.PatternString;

            DeriveMetaInformation();
        }

        public PhraseRuleSet(ulong serverId, string pattern, PcreOptions pcreOptions, PhraseRuleRequirement[] ruleRequirements) {
            ServerId = serverId;
            ManualPattern = true;
            Requirements = ruleRequirements;

            regex = new PcreRegex(pattern, pcreOptions);
            Pattern = pattern;

            DeriveMetaInformation();
        }

        private void DeriveMetaInformation() {
            // always prioritize blacklists over whitelists if they both exist for some reason - blacklists are more restrictive so less likely to cause issues.

            foreach(PhraseRuleRequirement phraseRuleModifier in Requirements) {
                switch (phraseRuleModifier.RequirementType) {
                    case RuleRequirementType.MODIFIER_CHANNELS_WHITELIST:
                        channelWhitelist = phraseRuleModifier.DataAsType<ulong[]>();
                        break;

                    case RuleRequirementType.MODIFIER_CHANNELS_BLACKLIST:
                        channelBlacklist = phraseRuleModifier.DataAsType<ulong[]>();
                        channelWhitelist = null;
                        break;

                    case RuleRequirementType.MODIFIER_ROLES_WHITELIST_ANY:
                        roleWhitelist = phraseRuleModifier.DataAsType<ulong[]>();
                        roleWhitelistAny = true;
                        break;

                    case RuleRequirementType.MODIFIER_ROLES_WHITELIST_ALL:
                        roleWhitelist = phraseRuleModifier.DataAsType<ulong[]>();
                        roleWhitelistAny = false;
                        break;

                    case RuleRequirementType.MODIFIER_ROLES_BLACKLIST_ANY:
                        roleBlacklist = phraseRuleModifier.DataAsType<ulong[]>();
                        roleBlacklistAny = true;
                        break;

                    case RuleRequirementType.MODIFIER_ROLES_BLACKLIST_ALL:
                        roleBlacklist = phraseRuleModifier.DataAsType<ulong[]>();
                        roleBlacklistAny = false;
                        break;

                    case RuleRequirementType.MODIFIER_USERS_WHITELIST:
                        userWhitelist = phraseRuleModifier.DataAsType<ulong[]>();
                        break;

                    case RuleRequirementType.MODIFIER_USERS_BLACKLIST:
                        userBlacklist = phraseRuleModifier.DataAsType<ulong[]>();
                        userWhitelist = null;
                        break;

                    case RuleRequirementType.MODIFIER_NOT_BOT:
                        botDelete = false;
                        break;

                    case RuleRequirementType.MODIFIER_SELF_DELETE:
                        selfDelete = true;
                        break;
                }
            }
        }

        public bool Matches(SocketMessage socketMessage) {
            if (!botDelete && socketMessage.Source == MessageSource.Bot) {
                return false;
            }

            if (!selfDelete && socketMessage.Author.Id == Program.BotAccountId) {
                return false;
            }

            if (channelWhitelist != null && !channelWhitelist.Contains(socketMessage.Channel.Id)) {
                return false;
            }

            if (channelBlacklist != null && channelBlacklist.Contains(socketMessage.Channel.Id)) {
                return false;
            }

            if (roleWhitelist != null) {
                if (roleWhitelistAny) {
                    foreach (SocketRole role in ((SocketGuildUser) socketMessage.Author).Roles) {
                        if (roleWhitelist.Contains(role.Id)) {
                            return regex.IsMatch(socketMessage.Content);
                        }
                    }

                    return false;
                }

                else {
                    int rolesFound = 0;

                    foreach (SocketRole role in ((SocketGuildUser) socketMessage.Author).Roles) {
                        if (roleWhitelist.Contains(role.Id)) {
                            rolesFound++;

                            if (rolesFound == roleWhitelist.Length) {
                                return regex.IsMatch(socketMessage.Content);
                            }
                        }
                    }

                    return false;
                }
            }

            if (roleBlacklist != null) {
                if (roleBlacklistAny) {
                    foreach (SocketRole role in ((SocketGuildUser) socketMessage.Author).Roles) {
                        if (roleBlacklist.Contains(role.Id)) {
                            return false;
                        }
                    }
                }

                else {
                    int rolesFound = 0;

                    foreach (SocketRole role in ((SocketGuildUser) socketMessage.Author).Roles) {
                        if (roleBlacklist.Contains(role.Id)) {
                            rolesFound++;

                            if (rolesFound == roleBlacklist.Length) {
                                return false;
                            }
                        }
                    }
                }
            }

            if (userWhitelist != null && !userWhitelist.Contains(socketMessage.Author.Id)) {
                return false;
            }

            if (userBlacklist != null && userBlacklist.Contains(socketMessage.Author.Id)) {
                return false;
            }

            return regex.IsMatch(socketMessage.Content);
        }

        public static PhraseRuleSetFactory FromFactory() {
            return new PhraseRuleSetFactory();
        }
    }

    public sealed class PhraseRuleSetFactory {
        private string text;

        private List<PhraseRuleRequirement> ruleRequirements = new List<PhraseRuleRequirement>();
        private List<PhraseHomographOverride> ruleHomographOverrides = new List<PhraseHomographOverride>();
        private List<PhraseSubstringModifier> substringModifiers = new List<PhraseSubstringModifier>();

        public PhraseRuleSetFactory WithText(string text) {
            this.text = text;
            return this;
        }

        public PhraseRuleSetFactory WithRuleRequirement(PhraseRuleRequirement ruleRequirement) {
            ruleRequirements.Add(ruleRequirement);
            return this;
        }

        public PhraseRuleSetFactory WithHomographOverride(PhraseHomographOverride homographOverride) {
            ruleHomographOverrides.Add(homographOverride);
            return this;
        }

        public PhraseRuleSetFactory WithRuleSubstringModifier(PhraseSubstringModifier substringModifier) {
            substringModifiers.Add(substringModifier);
            return this;
        }

        public PhraseRuleSet Build(ulong serverId) {
            return new PhraseRuleSet(serverId, text, ruleRequirements.ToArray(), ruleHomographOverrides.ToArray(), substringModifiers.ToArray());
        }
    }
}