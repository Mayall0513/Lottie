using DiscordBot6.Constraints;
using System.Collections.Generic;

namespace DiscordBot6.Database.Models.Constraints {
    public sealed class RoleConstraintModel : IModelFor<RoleConstraint> {
        public bool WhitelistStrict { get; set; }
        public HashSet<ulong> WhitelistRequirements { get; } = new HashSet<ulong>();

        public bool BlacklistStrict { get; set; }
        public HashSet<ulong> BlacklistRequirements { get; } = new HashSet<ulong>();

        public RoleConstraint CreateConcrete() {
            return new RoleConstraint(WhitelistStrict, WhitelistRequirements, BlacklistStrict, BlacklistRequirements);
        }
    }
}
