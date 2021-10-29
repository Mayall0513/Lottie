using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("checkroles")]
    public sealed class CheckRoles : ModuleBase<SocketCommandContext> {
        [Command]
        public async Task Command(ulong id) {
            await CommandImpl(id);
        }

        [Command]
        public async Task Command(IUser user) {
            await CommandImpl(user.Id);
        }

        private async Task CommandImpl(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayCheckRolePersists(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.SendNoPermissionResponseAsync(socketGuildUser);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                if (serverUser == null) {
                    await Context.Channel.SendUserNotFoundResponseAsync(userId);
                    return;
                }

                IEnumerable<ulong> persistedRolesId = await serverUser.GetRolesPersistedAsync();
                IEnumerable<SocketRole> persistedRoles = Context.Guild.Roles.Where(x => persistedRolesId.Contains(x.Id));

                if (!persistedRoles.Any()) {
                    await Context.Channel.SendGenericSuccessResponseAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), "User has no role persists");
                    return;
                }

                StringBuilder mutePersistsBuilder = new StringBuilder();
                foreach (SocketRole persistedRole in persistedRoles) {
                    mutePersistsBuilder.Append("<@&").Append(persistedRole.Id).Append("> `").Append(persistedRole.Name).Append("`").Append(DiscordBot6.DiscordNewLine);
                }

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                await Context.Channel.SendGenericSuccessResponseAsync(userId, guildUser?.GetAvatarUrl(size: 64), mutePersistsBuilder.ToString());
            }
        }

    }
}
