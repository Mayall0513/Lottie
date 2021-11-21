using Discord.WebSocket;
using Lottie.Helpers;
using System.Linq;

namespace Lottie.Timing {
    public sealed class RolePersist : TimedObject {
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public ulong RoleId { get; set; }

        public override async void OnExpiry() {
            SocketGuild socketGuild = DiscordBot6.Client.GetGuild(ServerId);
            if (socketGuild == null) {
                return;
            }

            Server server = await Server.GetServerAsync(ServerId);
            User user = await server?.GetUserAsync(UserId);
            await user.RemoveRolePersistedAsync(RoleId);

            SocketUser socketUser = socketGuild.GetUser(UserId);
            if (socketUser is SocketGuildUser socketGuildUser) {
                SocketRole socketRole = socketGuildUser.Roles.FirstOrDefault(role => RoleId == role.Id && socketGuild.MayEditRole(role));

                if (socketRole != null && socketGuild.MayEditRole(RoleId, null)) {
                    ulong[] roleId = new ulong[1] { RoleId };
                    user.AddMemberStatusUpdate(null, roleId);

                    await user.ApplyContingentRolesAsync(socketGuildUser, socketGuildUser.GetRoleIds(), socketGuildUser.GetRoleIds().Except(roleId));
                    await socketGuildUser.RemoveRolesAsync(roleId);
                }
            }
        }
    }
}
