using DiscordBot6.ServerRules;
using System.Collections.Generic;

namespace DiscordBot6.Database.Models.ServerRules {
    public sealed class ServerRuleConstraintModel : IModelFor<ServerRuleConstraint> {
        public ulong Id { get; set; }
        public int ConstraintType { get; set; }

        public List<ulong> Data { get; set; } = new List<ulong>();

        public ServerRuleConstraint CreateConcrete() {
            return new ServerRuleConstraint((ServerRuleConstraintType) ConstraintType, Data);
        }
    }
}
