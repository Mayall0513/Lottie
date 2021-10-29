using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("giveroles")]
    public sealed class GiveRoles : ModuleBase<SocketCommandContext> {
        [Command]
        public async Task Command(ulong userId, params string[] roles) {
            await CommandImpl(userId, roles);
        }

        [Command]
        public async Task Command(IUser user, params string[] roles) {
            await CommandImpl(user.Id, roles);
        }

        public async Task CommandImpl(ulong userId, string[] roles) {
            if (roles.Length == 0) {
                await Context.Channel.SendGenericErrorResponseAsync(Context.User.Id, Context.User.GetAvatarUrl(size: 64), "You must list roles after the user's ID");
                return;
            }

            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayGiveRoles(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.SendNoPermissionResponseAsync(socketGuildUser);
                    return;
                }

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser == null) {
                    await Context.Channel.SendUserNotFoundResponseAsync(userId);
                    return;
                }

                bool anyRoles = CommandHelper.GetRoles(roles, Context.Guild, socketGuildUser, out HashSet<SocketRole> validRoles, out HashSet<SocketRole> lockedRoles, out HashSet<ulong> phantomRoles, out List<string> invalidRoles);

                string[] errorMessages = new string[lockedRoles.Count + invalidRoles.Count];
                int index = 0;

                foreach(SocketRole lockedRole in lockedRoles) {
                    errorMessages[index++] = $"Could not give {lockedRole.Mention} since it is above you or me in the role hierarchy.";
                }

                foreach (string invalidRole in invalidRoles) {
                    errorMessages[index++] = $"Could not find role with name `{invalidRole}`.";
                }

                if (!anyRoles) {
                    await Context.Channel.SendGenericErrorResponseAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), errorMessages);
                }

                IEnumerable<ulong> rolesToPersist = validRoles.Select(role => role.Id).Union(phantomRoles);

                User serverUser = await server.GetUserAsync(userId);
                await serverUser.AddRolesPersistedAsync(rolesToPersist);

                if (validRoles.Count > 0) {
                    serverUser.IncrementRolesUpdated();
                    await guildUser.AddRolesAsync(validRoles);
                }

                IEnumerable<string> newRoleNames = validRoles.Select(role => role.Mention)
                    .Union(phantomRoles.Select(role => $"`{role}`"));

                string messageSuffix = $"{string.Join(", ", newRoleNames)} to {guildUser.Mention}.";

                if (errorMessages.Length > 0) {
                    await Context.Channel.SendGenericMixedResponseAsync(guildUser.Id, guildUser.GetAvatarUrl(size: 64), $"Gave {messageSuffix}", errorMessages);
                }

                else {
                    await Context.Channel.SendGenericSuccessResponseAsync(guildUser.Id, guildUser.GetAvatarUrl(size: 64), $"Gave {messageSuffix}");
                }

                if (server.HasLogChannel) {
                    await Context.Guild.GetTextChannel(server.LogChannelId)
                        .LogGenericSuccessAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), $"{socketGuildUser.Mention} gave {messageSuffix}");
                }
            }
        }
    }
}
