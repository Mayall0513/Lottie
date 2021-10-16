using System.Collections.Generic;

namespace DiscordBot6.Rules {
    public enum RuleConstraintType {
        CONSTRAINT_USER_WHITELIST,
        CONSTRAINT_USER_BLACKLIST,

        CONSTRAINT_CHANNEL_WHITELIST,
        CONSTRAINT_CHANNEL_BLACKLIST,

        CONSTRAINT_ROLE_WHITELIST_ALL,
        CONSTRAINT_ROLE_WHITELIST_ANY,
        CONSTRAINT_ROLE_BLACKLIST_ALL,
        CONSTRAINT_ROLE_BLACKLIST_ANY,
    };

    public sealed class ServerRuleConstraint {
        public RuleConstraintType ConstraintType { get; }
        public List<ulong> Constraints { get; } = new List<ulong>();

        public ServerRuleConstraint(RuleConstraintType constraintType) {
            ConstraintType = constraintType;
        }
    }
}
