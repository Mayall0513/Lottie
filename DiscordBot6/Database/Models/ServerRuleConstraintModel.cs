using System.Collections.Generic;

namespace DiscordBot6.Database.Models {
    public sealed class ServerRuleConstraintModel {
        public int ConstraintType { get; set; }

        public List<ulong> Data { get; set; } = new List<ulong>();

        public ServerRuleConstraintModel(int constraintType) {
            ConstraintType = constraintType;
        }
    }
}
