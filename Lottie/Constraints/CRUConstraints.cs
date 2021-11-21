using System.Collections.Generic;

namespace Lottie.Constraints {
    public sealed class CRUConstraints {
        private readonly GenericConstraint channelConstraint;
        private readonly RoleConstraint roleConstraint;
        private readonly GenericConstraint userConstraint;

        public CRUConstraints(GenericConstraint channelConstraint, RoleConstraint roleConstraint, GenericConstraint userConstraint) {
            this.channelConstraint = channelConstraint;
            this.roleConstraint = roleConstraint;
            this.userConstraint = userConstraint;
        }

        public bool Matches(ulong? channelId = null, IEnumerable<ulong> roleIds = null, ulong? userId = null) {
            if (channelConstraint != null && channelId != null && !channelConstraint.Matches(channelId.Value)) {
                return false;
            }

            if (roleConstraint != null && roleIds != null && !roleConstraint.Matches(roleIds)) {
                return false;
            }

            if (userId != null && userConstraint != null && !userConstraint.Matches(userId.Value)) {
                return false;
            }

            return true;
        }
    }
}
