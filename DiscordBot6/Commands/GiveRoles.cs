using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("giveroles")]
    public sealed class GiveRoles : ModuleBase<SocketCommandContext> {
        [Command]
        public async Task Command(ulong userId, params ulong[] roleIds) {
            if (roleIds.Length == 0) {
                await Context.Channel.SendMessageAsync($"You must put the IDs of the roles you want to add after the user's ID.");
                return;
            }

            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();
                IEnumerable<ulong> userIds = socketGuildUser.Roles.Select(x => x.Id);

                if (!await server.UserMayGiveRoles(socketGuildUser.Id, userIds)) {
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                await serverUser.AddRolesPersistedAsync(roleIds);

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser != null) {
                    serverUser.IncrementRolesUpdated();
                    await guildUser.AddRolesAsync(roleIds);
                }
            }
        }
    }
}
