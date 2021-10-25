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
    [Group("giveroles")]
    public sealed class GiveRoles : ModuleBase<SocketCommandContext> {
        [Command]
        public async Task Command(ulong userId, params ulong[] roleIds) {
            await CommandImpl(userId, roleIds);
        }

        [Command]
        public async Task Command(IUser user, params ulong[] roleIds) {
            await CommandImpl(user.Id, roleIds);
        }

        public async Task CommandImpl(ulong userId, ulong[] roleIds) {
            if (roleIds.Length == 0) {
                await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateSimpleErrorEmbed("You must list role ID's after the user's ID"));
                return;
            }

            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();
                IEnumerable<ulong> userIds = socketGuildUser.Roles.Select(x => x.Id);

                if (!await server.UserMayGiveRoles(socketGuildUser.Id, userIds)) {
                    await ResponseHelper.SendNoPermissionsResponse(Context.Channel);
                    return;
                }

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser == null) {
                    await ResponseHelper.SendUserNotFoundResponse(Context.Channel, userId);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                await serverUser.AddRolesPersistedAsync(roleIds);

                serverUser.IncrementRolesUpdated();
                await guildUser.AddRolesAsync(roleIds);

                await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateSimpleSuccessEmbed($"Gave `{string.Join("`, `", roleIds.Select(x => Context.Guild.GetRole(x).Name))}` to {guildUser.Mention}"));
            }
        }
    }
}
