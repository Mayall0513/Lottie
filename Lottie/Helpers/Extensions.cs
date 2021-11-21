using Discord;
using Discord.WebSocket;
using Lottie.ContingentRoles;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lottie.Helpers {
    public static class Extensions {
        public static IEnumerable<ulong> GetRoleIds(this SocketGuildUser user) {
            foreach (SocketRole role in user.Roles) {
                yield return role.Id;
            }
        }

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

        public static async Task<(bool anyChange, ICollection<ulong> allRolesAdded, ICollection<ulong> allRolesRemoved)> UpdateContingentRolesAsync(this Server server, User user, IEnumerable<ulong> before, IEnumerable<ulong> after) {
            IEnumerable<ContingentRole> contingentRoles = await server.GetContingentRolesAsync();
            if (contingentRoles == null) {
                return (false, null, null);
            }

            HashSet<ulong> beforeRoles = new HashSet<ulong>(before);
            HashSet<ulong> afterRoles = new HashSet<ulong>(after);
            HashSet<ulong> newRoles = new HashSet<ulong>(afterRoles);

            HashSet<ulong> rolesAdded = new HashSet<ulong>(beforeRoles.Count);
            HashSet<ulong> rolesRemoved = new HashSet<ulong>(afterRoles.Count);

            int iteration = 0;
            while (iteration < 8) {
                newRoles.UnionWith(afterRoles);

                rolesAdded.UnionWith(afterRoles);
                rolesAdded.ExceptWith(beforeRoles);

                bool anyAdded = rolesAdded.Any();
                if (anyAdded) {
                    IEnumerable<ulong> rolesToRemove0 = contingentRoles.Where(contingentRole => newRoles.Contains(contingentRole.RoleId))
                        .SelectMany(contingentRole => contingentRole.ContingentRoles).Intersect(rolesAdded);

                    if (rolesToRemove0.Any()) {
                        newRoles.ExceptWith(rolesToRemove0);
                        rolesAdded.ExceptWith(rolesToRemove0);
                    }

                    foreach (ulong roleAdded in rolesAdded) {
                        ContingentRole contingentRole = contingentRoles.FirstOrDefault(role => role.RoleId == roleAdded);
                        if (contingentRole == null) {
                            continue;
                        }

                        IEnumerable<ulong> rolesToRemove1 = contingentRole.ContingentRoles.Intersect(afterRoles);
                        await user.AddActiveContingentRoleAsync(contingentRole.RoleId, rolesToRemove1);
                        newRoles.ExceptWith(rolesToRemove1);
                    }
                }
                
                rolesRemoved.UnionWith(beforeRoles);
                rolesRemoved.ExceptWith(afterRoles);

                bool anyRemoved = rolesRemoved.Any();
                if (anyRemoved) {
                    IEnumerable<ulong> contingentRolesRemoved = (await user.GetActiveContingentRoleIdsAsync()).Intersect(rolesRemoved);

                    foreach (ulong contingentRoleId in contingentRolesRemoved) {
                        IEnumerable<ulong> rolesToAdd = await user.GetContingentRolesRemovedAsync(contingentRoleId);
                        await user.RemoveActiveContingentRoleAsync(contingentRoleId);
                        newRoles.UnionWith(rolesToAdd);
                    }
                }

                if (!anyAdded && !anyRemoved) {
                    break;
                }

                beforeRoles.Clear();
                beforeRoles.UnionWith(afterRoles);

                afterRoles.Clear();
                afterRoles.UnionWith(newRoles);

                rolesAdded.Clear();
                rolesRemoved.Clear();

                iteration++;
            }

            HashSet<ulong> allRolesAdded = new HashSet<ulong>();
            HashSet<ulong> allRolesRemoved = new HashSet<ulong>();

            allRolesAdded.UnionWith(newRoles);
            allRolesAdded.ExceptWith(after);

            allRolesRemoved.UnionWith(after);
            allRolesRemoved.ExceptWith(newRoles);

            return (allRolesAdded.Any() || allRolesRemoved.Any(), allRolesAdded, allRolesRemoved);
        }

        public static async Task ApplyContingentRolesAsync(this User user, SocketGuildUser guildUser, IEnumerable<ulong> beforeRoles, IEnumerable<ulong> afterRoles) {
            (bool anyChanges, ICollection<ulong> allRolesAdded, ICollection<ulong> allRolesRemoved) = await user.Parent.UpdateContingentRolesAsync(user, beforeRoles, afterRoles);

            if (anyChanges) {
                user.AddMemberStatusUpdate(allRolesAdded, allRolesRemoved);

                if (allRolesRemoved.Count > 0) {
                    await guildUser.RemoveRolesAsync(allRolesRemoved.Where(role => guildUser.Guild.MayEditRole(role, null)));
                }

                if (allRolesAdded.Count > 0) {
                    await guildUser.AddRolesAsync(allRolesAdded.Where(role => guildUser.Guild.MayEditRole(role, null)));

                    if (user.Parent.AutoRolePersist) {
                        await user.AddRolesPersistedAsync(allRolesAdded, null);
                    }
                }
            }
        }

        public static void CompareVoiceStatuses(SocketVoiceState beforeState, SocketVoiceState afterState, out VoiceStatusUpdate voiceStatusUpdate) {
            voiceStatusUpdate = new VoiceStatusUpdate(CompareVoiceStatusElement(beforeState.IsMuted, afterState.IsMuted), CompareVoiceStatusElement(beforeState.IsDeafened, afterState.IsDeafened));
        }

        private static int CompareVoiceStatusElement(bool before, bool after) {
            if (before == after) {
                return 0;
            }

            if (before) {
                return -1;
            }

            else {
                return 1;
            }
        }

        public static void CompareMemberStatuses(SocketGuildUser beforeUser, SocketGuildUser afterUser, out MemberStatusUpdate memberStatusUpdate) {
            // we're only interested in the IDs of the roles added and removed (and after too for checking for contingent roles)
            HashSet<ulong> rolesAfter = new HashSet<ulong>(afterUser.GetRoleIds());

            HashSet<ulong> rolesAdded = new HashSet<ulong>(rolesAfter);
            HashSet<ulong> rolesRemoved = new HashSet<ulong>(beforeUser.GetRoleIds());

            rolesAdded.ExceptWith(rolesRemoved); // remove all of the roles the user had at the start from those they had at the end to get a list of additions
            rolesRemoved.ExceptWith(rolesAfter); // remove all of the roles the user had at the end from those they had at the start to get a list of removals

            memberStatusUpdate = new MemberStatusUpdate(rolesAdded, rolesRemoved);
        }
    }
}
