using Discord.WebSocket;
using DiscordBot6.ContingentRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot6.Helpers {
    public static class Extensions {
        public static bool MayEditRole(this SocketGuild socketGuild, ulong roleId, SocketGuildUser commandUser = null) {
            return MayEditRole(socketGuild, socketGuild.Roles.FirstOrDefault(role => role.Id == roleId), commandUser);
        }

        public static bool MayEditRole(this SocketGuild socketGuild, SocketRole role, SocketGuildUser commandUser = null) {
            if (role == null) {
                return false;
            }

            if (commandUser == null) {
                return socketGuild.CurrentUser.Hierarchy > role.Position;
            }

            else {
                return socketGuild.CurrentUser.Hierarchy > role.Position && commandUser.Hierarchy > role.Position;
            }
        }


        public static async Task<bool> UpdateContingentRoles_AddedAsync(this Server server, User user, SocketGuildUser socketUser, IEnumerable<ulong> allRoles) {
            IEnumerable<ContingentRole> serverContingentRoles = await server.GetContingentRolesAsync();
            IEnumerable<ContingentRole> activeContingentRoles = serverContingentRoles.Where(contingentRole => allRoles.Contains(contingentRole.RoleId));

            if(activeContingentRoles.Any()) {
                HashSet<ulong> rolesToRemove = new HashSet<ulong>();

                foreach (ContingentRole contingentRole in activeContingentRoles) {
                    IEnumerable<ulong> newRoles = contingentRole.ContingentRoles.Intersect(allRoles);

                    rolesToRemove.UnionWith(newRoles.Where(roleId => socketUser.Guild.MayEditRole(roleId)));
                    await user.AddActiveContingentRoleAsync(contingentRole.RoleId, newRoles);
                }

                if (rolesToRemove.Count > 0) { // there are roles we need to remove
                    user.IncrementRolesUpdated();
                    await socketUser.RemoveRolesAsync(rolesToRemove);

                    return true;
                }
            }

            return false;

        }

        public static async Task<bool> UpdateContingentRoles_RemovedAsync(this Server server, User user, SocketGuildUser socketUser, IEnumerable<ulong> rolesRemoved, bool botEvent) {
            IEnumerable<ulong> contingentRolesRemoved = rolesRemoved.Intersect(await user.GetActiveContingentRoleIds());

            if (contingentRolesRemoved.Any()) {
                HashSet<ulong> rolesToAdd = new HashSet<ulong>();

                foreach (ulong contigentRoleId in contingentRolesRemoved) {
                    IEnumerable<ulong> newRoles = await user.GetContingentRolesRemoved(contigentRoleId);

                    rolesToAdd.UnionWith(newRoles.Where(roleId => socketUser.Guild.MayEditRole(roleId)));
                    await user.RemoveActiveContingentRoleAsync(contigentRoleId);
                }

                if (rolesToAdd.Count > 0) {
                    user.IncrementRolesUpdated();
                    await socketUser.AddRolesAsync(rolesToAdd);

                    if (!botEvent && server.AutoRolePersist) {
                        await user.AddRolesPersistedAsync(rolesToAdd, null);
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
