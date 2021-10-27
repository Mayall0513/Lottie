using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Helpers;
using DiscordBot6.Timing;
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
                IEnumerable<ulong> userIds = socketGuildUser.Roles.Select(x => x.Id);

                if (!await server.UserMayCheckRolePersists(socketGuildUser.Id, userIds)) {
                    await ResponseHelper.SendNoPermissionsResponse(Context.Channel);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);

                if (serverUser == null) {
                    await ResponseHelper.SendUserNotFoundResponse(Context.Channel, userId);
                    return;
                }

                IEnumerable<ulong> persistedRolesId = await serverUser.GetRolesPersistedAsync();
                IEnumerable<SocketRole> persistedRoles = Context.Guild.Roles.Where(x => persistedRolesId.Contains(x.Id));

                if (!persistedRoles.Any()) {
                    await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateSimpleSuccessEmbed("No role persists"));
                    return;
                }

                StringBuilder mutePersistsBuilder = new StringBuilder();
                
                foreach (SocketRole persistedRole in persistedRoles) {
                    mutePersistsBuilder.Append("<@&").Append(persistedRole.Id).Append("> `").Append(persistedRole.Name).Append("`").Append(DiscordBot6.DiscordNewLine);
                }

                await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateSimpleSuccessEmbed(mutePersistsBuilder.ToString()));
            }
        }

    }
}
