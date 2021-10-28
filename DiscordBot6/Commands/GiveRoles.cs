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

                IEnumerable<SocketRole> serverRoles = Context.Guild.Roles.Where(role => roleIds.Contains(role.Id));
                List<SocketRole> invalidRoles = serverRoles.Where(role => role.Position > socketGuildUser.Hierarchy || role.Position > Context.Guild.CurrentUser.Hierarchy).ToList();

                IEnumerable<SocketRole> remainingRoles = serverRoles.Except(invalidRoles);
                IEnumerable<ulong> remainingRoleIds = remainingRoles.Select(role => role.Id);

                StringBuilder errors = new StringBuilder();

                if (invalidRoles.Count > 0) {
                    errors.Append("Could not give ")
                        .AppendJoin(", ", invalidRoles.Select(role => role.Mention))
                        .Append(" because").Append(invalidRoles.Count == 1 ? " it is " : " they are ")
                        .Append("above you or me in the role hierarchy")
                        .Append(DiscordBot6.DiscordNewLine).Append(DiscordBot6.DiscordNewLine);
                }

                User serverUser = await server.GetUserAsync(userId);
                await serverUser.AddRolesPersistedAsync(remainingRoleIds);

                serverUser.IncrementRolesUpdated();
                await guildUser.AddRolesAsync(remainingRoleIds);

                if (errors.Length > 0) {
                    await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateSimpleMixedEmbed(errors + $"Gave {string.Join(", ", remainingRoles.Select(role => role.Mention))} to {guildUser.Mention}"));
                }

                else {
                    await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateSimpleSuccessEmbed($"Gave {string.Join(", ", remainingRoles.Select(role => role.Mention))} to {guildUser.Mention}"));
                }
            }
        }
    }
}
