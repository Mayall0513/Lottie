using System.Collections.Generic;

namespace DiscordBot6.ServerRules {
    public enum ServerRuleConstraintType {
        CONSTRAINT_USER_WHITELIST,
        CONSTRAINT_USER_BLACKLIST,

        CONSTRAINT_CHANNEL_WHITELIST,
        CONSTRAINT_CHANNEL_BLACKLIST,

        CONSTRAINT_ROLE_WHITELIST_ALL,
        CONSTRAINT_ROLE_WHITELIST_ANY,
        CONSTRAINT_ROLE_BLACKLIST_ALL,
        CONSTRAINT_ROLE_BLACKLIST_ANY,
    };

    public struct ServerRuleConstraint {
        public ServerRuleConstraintType ConstraintType { get; }

        public IReadOnlyCollection<ulong> Data { get; }

        public ServerRuleConstraint(ServerRuleConstraintType constraintType, IEnumerable<ulong> data) {
            ConstraintType = constraintType;
            Data = data as IReadOnlyCollection<ulong>;
        }
    }
}
