using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("rolepersist")]
    public sealed class RolePersists : ModuleBase<SocketCommandContext> {
        [Command("add")]
        public async Task AddCommand(ulong userId, params string[] roles) {
            await AddCommandImpl(userId, roles);
        }

        [Command("add")]
        public async Task AddCommand(IUser user, params string[] roles) {
            await AddCommandImpl(user.Id, roles);
        }

        [Command("check")]
        public async Task CheckCommand(ulong id) {
            await CheckCommandImpl(id);
        }

        [Command("check")]
        public async Task CheckCommand(IUser user) {
            await CheckCommandImpl(user.Id);
        }


        public async Task AddCommandImpl(ulong userId, string[] roles) {
            if (roles.Length == 0) {
                await Context.Channel.SendGenericErrorAsync(Context.User.Id, Context.User.GetAvatarUrl(size: 64), "You must list roles after the user's ID");
                return;
            }

            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayGiveRoles(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.SendNoPermissionAsync(socketGuildUser);
                    return;
                }

                bool newRoles = CommandHelper.GetRoles(roles, Context.Guild, socketGuildUser, out HashSet<SocketRole> validRoles, out HashSet<SocketRole> lockedRoles, out HashSet<ulong> phantomRoles, out List<string> invalidRoles);
                string[] errorMessages = new string[lockedRoles.Count + invalidRoles.Count];
                int index = 0;

                foreach (SocketRole lockedRole in lockedRoles) {
                    string lockedRoleIdentifier = CommandHelper.GetRoleIdentifier(lockedRole.Id, lockedRole);

                    errorMessages[index++] = $"Could not give {lockedRoleIdentifier} since it is above you or me in the role hierarchy";
                }

                foreach (string invalidRole in invalidRoles) {
                    errorMessages[index++] = $"Could not find role with name `{invalidRole}`";
                }

                if (!newRoles) {
                    await Context.Channel.SendGenericErrorAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), errorMessages);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                await serverUser.AddRolesPersistedAsync(validRoles.Select(role => role.Id).Union(phantomRoles));

                SocketGuildUser socketUser = Context.Guild.GetUser(userId);
                if (socketUser != null && validRoles.Count > 0) {
                    await socketUser.AddRolesAsync(validRoles);
                }

                IEnumerable<string> newRoleIdentifiers = validRoles.Select(role => CommandHelper.GetRoleIdentifier(role.Id, role))
                    .Union(phantomRoles.Select(roleId => CommandHelper.GetRoleIdentifier(roleId, null)));

                string newRolesList = new StringBuilder()
                    .Append(DiscordBot6.DiscordNewLine).Append(DiscordBot6.DiscordNewLine)
                    .Append("**Roles:**").Append(DiscordBot6.DiscordNewLine)
                    .Append(string.Join(DiscordBot6.DiscordNewLine, newRoleIdentifiers)).ToString();

                string persistedIdentifier = CommandHelper.GetUserIdentifier(userId, socketUser);

                if (errorMessages.Length > 0) {
                    await Context.Channel.SendGenericMixedAsync(userId, socketUser?.GetAvatarUrl(size: 64), $"Gave roles to {persistedIdentifier}{newRolesList}", errorMessages);
                }

                else {
                    await Context.Channel.SendGenericSuccessAsync(userId, socketUser?.GetAvatarUrl(size: 64), $"Gave roles to {persistedIdentifier}{newRolesList}");
                }

                if (server.HasLogChannel) {
                    string persisterIdentifier = CommandHelper.GetUserIdentifier(socketGuildUser.Id, socketGuildUser);

                    await Context.Guild.GetTextChannel(server.LogChannelId)
                        .LogGenericSuccessAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), $"{persisterIdentifier} gave roles to {persistedIdentifier}{newRolesList}");
                }
            }
        }

        private async Task CheckCommandImpl(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayCheckRolePersists(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.SendNoPermissionAsync(socketGuildUser);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                if (serverUser == null) {
                    await Context.Channel.SendUserNotFoundAsync(userId);
                    return;
                }

                IEnumerable<ulong> persistedRoleIds = await serverUser.GetRolesPersistedAsync();
                SocketGuildUser socketUser = Context.Guild.GetUser(userId);

                if (!persistedRoleIds.Any()) {
                    await Context.Channel.SendGenericSuccessAsync(userId, socketUser?.GetAvatarUrl(size: 64), "User has no role persists");
                    return;
                }


                StringBuilder mutePersistsBuilder = new StringBuilder();
                foreach (ulong persistedRoleId in persistedRoleIds) {
                    SocketRole socketRole = Context.Guild.Roles.FirstOrDefault(role => role.Id == persistedRoleId);

                    string roleIdentifier = CommandHelper.GetRoleIdentifier(persistedRoleId, socketRole);
                    mutePersistsBuilder.Append(roleIdentifier).Append(DiscordBot6.DiscordNewLine);
                }

                await Context.Channel.SendGenericSuccessAsync(userId, socketUser?.GetAvatarUrl(size: 64), mutePersistsBuilder.ToString());
            }
        }
    }
}
