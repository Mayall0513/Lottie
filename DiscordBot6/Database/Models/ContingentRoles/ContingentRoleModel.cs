﻿using DiscordBot6.ContingentRoles;
using System.Collections.Generic;

namespace DiscordBot6.Database.Models.ContingentRoles {
    public sealed class ContingentRoleModel : IModelFor<ContingentRole> {
        public ulong Id { get; set; }
        public ulong ServerId { get; set; }
        public ulong RoleId { get; set; }

        public HashSet<ulong> ContingentRoles = new HashSet<ulong>();

        public ContingentRole CreateConcrete() {
            return new ContingentRole(ServerId, RoleId, ContingentRoles);
        }
    }
}
