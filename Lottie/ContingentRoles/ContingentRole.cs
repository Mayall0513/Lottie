using System.Collections.Generic;

namespace Lottie.ContingentRoles {
    public sealed class ContingentRole {
        public ulong ServerId { get; set; }
        public ulong RoleId { get; set; }
        public IReadOnlyCollection<ulong> ContingentRoles { get; }

        public ContingentRole(ulong serverId, ulong roleId, IEnumerable<ulong> contingentRoles) {
            ServerId = serverId;
            RoleId = roleId;
            ContingentRoles = contingentRoles as IReadOnlyCollection<ulong>;
        }
    }
}
