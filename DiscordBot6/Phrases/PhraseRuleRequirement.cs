namespace DiscordBot6.Phrases {
    public enum RuleRequirementType {
        MODIFIER_WORD,         // must be a standalone word
        MODIFIER_WORDSTART,    // must be at the start of a word
        MODIFIER_WORDEND,      // must be at the end of a word
        MODIFIER_MESSAGE,      // must be the whole message
        MODIFIER_MESSAGESTART, // must be at the start of the message
        MODIFIER_MESSAGEEND,   // must be at the end of a message

        MODIFIER_CASESENSITIVE, // default is case insensitive

        MODIFIER_NOT_WORDSTART,    // must not be at the start of a word
        MODIFIER_NOT_WORDEND,      // must not be at the end of a word
        MODIFIER_NOT_MESSAGESTART, // must not be at the start of a message
        MODIFIER_NOT_MESSAGEEND,   // must not be at the end of a message
        
        MODIFIER_NOT_BEFORE, // must not be before specific text
        MODIFIER_NOT_AFTER,  // must not be after specific text

        MODIFIER_CHANNELS_WHITELIST, // bot will only delete messages in these channels
        MODIFIER_CHANNELS_BLACKLIST, // bot will not delete messages sent in these channels
        
        MODIFIER_ROLES_WHITELIST_ANY, // bot will only delete messages from users who have any of these roles
        MODIFIER_ROLES_WHITELIST_ALL, // bot will only delete messages from users who have all of these roles
        MODIFIER_ROLES_BLACKLIST_ANY, // bot will not delete messages from users who have any of these roles
        MODIFIER_ROLES_BLACKLIST_ALL, // bot will not delete messages from users who have all of these roles

        MODIFIER_USERS_WHITELIST, // bot will delete messages from only these users
        MODIFIER_USERS_BLACKLIST, // bot will not delete messages from these these users

        MODIFIER_NOT_BOT,    // bot will not delete messages from other bots
        MODIFIER_SELF_DELETE // bot will delete its own message if it matches the ruleset - default is that it won't
    }

    /// <summary>
    /// Defines a rule applied to all of a phrase
    /// </summary>
    public struct PhraseRuleRequirement {
        public RuleRequirementType RequirementType { get; set; }
        public object Data { get; set; }

        public PhraseRuleRequirement(RuleRequirementType requirementType, object data) {
            RequirementType = requirementType;
            Data = data;
        }

        public T DataAsType<T>() where T : class {
            return Data as T;
        }
    }
}
