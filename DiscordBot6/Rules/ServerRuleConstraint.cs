using System;
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

    public struct ServerRuleConstraint {
        public RuleConstraintType ConstraintType { get; set; }
        public IReadOnlyCollection<ulong> Constraints { get; set; }

        public ServerRuleConstraint(RuleConstraintType constraintType, ulong[] constraints) {
            ConstraintType = constraintType;
            Constraints = constraints;
        }
    }

    public sealed class ServerRuleConstraintModel {
        public int Id { get; set; }
        public int PhraseRuleId { get; set; }
        public DateTime CreationTime { get; set; }
   
        public int ConstraintType { get; set; }
        public List<ulong> Constraints { get; set; }
    }
}
