using DiscordBot6.Constraints;
using System.Collections.Generic;

namespace DiscordBot6.Database.Models.Constraints {
    public sealed class RoleConstraintModel : IModelFor<RoleConstraint> {
        public bool WhitelistStrict { get; set; }
        public List<ulong> WhitelistRequirements { get; } = new List<ulong>();

        public bool BlacklistStrict { get; set; }
        public List<ulong> BlacklistRequirements { get; } = new List<ulong>();

        public RoleConstraint CreateConcrete() {
            return new RoleConstraint(WhitelistStrict, WhitelistRequirements, BlacklistStrict, BlacklistRequirements);
        }
    }
}
