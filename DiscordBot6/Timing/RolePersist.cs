using Discord.WebSocket;
using DiscordBot6.Helpers;
using System.Linq;

namespace DiscordBot6.Timing {
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

                if (socketRole != null) {
                    ulong[] roleId = new ulong[1] { RoleId };
                    user.AddMemberStatusUpdate(null, roleId);

                    await socketGuildUser.RemoveRoleAsync(socketRole);
                    await user.ApplyContingentRolesAsync(socketGuildUser, socketGuildUser.GetRoleIds(), socketGuildUser.GetRoleIds().Except(roleId));
                }
            }
        }
    }
}
