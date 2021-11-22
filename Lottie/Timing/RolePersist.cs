using Discord.WebSocket;
using Lottie.Helpers;
using System.Linq;

namespace Lottie.Timing {
    public sealed class RolePersist : TimedObject {
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public ulong RoleId { get; set; }

        public override async void OnExpiry() {
            SocketGuild socketGuild = Lottie.Client.GetGuild(ServerId);
            if (socketGuild == null) {
                return;
            }

            SocketUser socketUser = socketGuild.GetUser(UserId);
            if (socketUser is SocketGuildUser socketGuildUser) {
                SocketRole socketRole = socketGuildUser.Roles.FirstOrDefault(role => RoleId == role.Id && socketGuild.MayEditRole(role));

                if (socketRole != null && socketGuild.MayEditRole(RoleId, null)) {
                    User user = await socketGuildUser.L_GetUserAsync();

                    ulong[] roleId = new ulong[1] { RoleId };
                    user.AddMemberStatusUpdate(null, roleId);

                    await user.RemoveRolePersistedAsync(RoleId);
                    await user.ApplyContingentRolesAsync(socketGuildUser, socketGuildUser.GetRoleIds(), socketGuildUser.GetRoleIds().Except(roleId));
                    await socketGuildUser.RemoveRolesAsync(roleId);
                }
            }
        }
    }
}
