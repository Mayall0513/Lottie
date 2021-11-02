using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Commands.Contexts;
using DiscordBot6.Helpers;
using DiscordBot6.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("rolepersist")]
    public sealed class RolePersists : ModuleBase<SocketGuildCommandContext> {
        private static readonly TimeSpan minimumRolePersistTimeSpan = TimeSpan.FromMinutes(2);

        [Command("add")]
        public async Task AddCommand(ulong userId, params string[] arguments) {
            int timeSpanIndex = CommandHelper.SplitTimeSpan(arguments);

            if (timeSpanIndex < arguments.Length) {
                string[] roles = arguments[..timeSpanIndex];
                string[] timeSpan = arguments[timeSpanIndex..];

                await AddTempImpl(userId, roles, timeSpan);
            }

            else {
                if (arguments.Length == 0) {
                    await Context.Channel.CreateResponse()
                        .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                        .SendGenericErrorsAsync("You must list roles after the user's ID");

                    return;
                }

                await AddCommandImpl(userId, arguments);
            }
        }

        [Command("add")]
        public async Task AddCommand(IUser user, params string[] arguments) {
            await AddCommand(user.Id, arguments);
        }

        [Command("check")]
        public async Task CheckCommand(ulong id) {
            await CheckCommandImpl(id);
        }

        [Command("check")]
        public async Task CheckCommand(IUser user) {
            await CheckCommandImpl(user.Id);
        }


        private async Task AddCommandImpl(ulong userId, string[] roles) {
            Server server = await Context.Guild.GetServerAsync();
            IEnumerable<ulong> userRoleIds = Context.User.Roles.Select(role => role.Id);

            if (!await server.UserMayGiveRoles(Context.User.Id, userRoleIds)) {
                await Context.Channel.CreateResponse()
                    .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                    .SendNoPermissionAsync();

                return;
            }

            int newRoleCount = CommandHelper.GetRoles(roles, Context.Guild, Context.User, out HashSet<SocketRole> validRoles, out HashSet<SocketRole> lockedRoles, out HashSet<ulong> phantomRoles, out List<string> invalidRoles);

            string[] errors = new string[lockedRoles.Count + invalidRoles.Count];
            int index = 0;

            foreach (SocketRole lockedRole in lockedRoles) {
                string lockedRoleIdentifier = CommandHelper.GetRoleIdentifier(lockedRole.Id, lockedRole);

                errors[index++] = $"Could not give {lockedRoleIdentifier} since it is above you or me in the role hierarchy";
            }

            foreach (string invalidRole in invalidRoles) {
                errors[index++] = $"Could not find role with name `{invalidRole}`";
            }

            if (newRoleCount == 0) {
                await Context.Channel.CreateResponse()
                    .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                    .SendGenericErrorsAsync(errors);

                return;
            }

            User serverUser = await server.GetUserAsync(userId);
            await serverUser.AddRolesPersistedAsync(validRoles.Select(role => role.Id).Union(phantomRoles), null);

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

            if (errors.Length > 0) {
                await Context.Channel.CreateResponse()
                    .WithSubject(userId, socketUser?.GetAvatarUrl(size: 64))
                    .SendGenericMixedAsync($"Gave roles to {persistedIdentifier} permanently{newRolesList}", errors);
            }

            else {
                await Context.Channel.CreateResponse()
                    .WithSubject(userId, socketUser?.GetAvatarUrl(size: 64))
                    .SendGenericSuccessAsync($"Gave roles to {persistedIdentifier} permanently{newRolesList}");
            }

            if (server.HasLogChannel) {
                string persisterIdentifier = CommandHelper.GetUserIdentifier(Context.User.Id, Context.User);

                await Context.Guild.GetTextChannel(server.LogChannelId).CreateResponse()
                    .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                    .LogGenericSuccessAsync($"{persisterIdentifier} gave roles to {persistedIdentifier} permanently{newRolesList}");
            }
        }

        private async Task AddTempImpl(ulong userId, string[] roles, string[] rawTimeSpan) {
            DateTime start = DateTime.UtcNow;

            Server server = await Context.Guild.GetServerAsync();
            IEnumerable<ulong> userRoleIds = Context.User.Roles.Select(role => role.Id);

            if (!await server.UserMayGiveTempRoles(Context.User.Id, userRoleIds)) {
                await Context.Channel.CreateResponse()
                    .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                    .SendNoPermissionAsync();

                return;
            }

            bool parsedTimeSpan = CommandHelper.GetTimeSpan(rawTimeSpan, out TimeSpan timeSpan, out string[] timeSpanErrors, minimumRolePersistTimeSpan);
            if (!parsedTimeSpan) {
                await Context.Channel.CreateResponse()
                    .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                    .SendGenericErrorsAsync(timeSpanErrors);

                return;
            }

            int newRoleCount = CommandHelper.GetRoles(roles, Context.Guild, Context.User, out HashSet<SocketRole> validRoles, out HashSet<SocketRole> lockedRoles, out HashSet<ulong> phantomRoles, out List<string> invalidRoles);

            string[] roleErrors = new string[lockedRoles.Count + invalidRoles.Count];
            int index = 0;

            foreach (SocketRole lockedRole in lockedRoles) {
                string lockedRoleIdentifier = CommandHelper.GetRoleIdentifier(lockedRole.Id, lockedRole);

                roleErrors[index++] = $"Could not give {lockedRoleIdentifier} since it is above you or me in the role hierarchy";
            }

            foreach (string invalidRole in invalidRoles) {
                roleErrors[index++] = $"Could not find role with name `{invalidRole}`";
            }

            if (newRoleCount == 0) {
                await Context.Channel.CreateResponse()
                    .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                    .SendGenericErrorsAsync(roleErrors);

                return;
            }

            string[] errors = roleErrors.Concat(timeSpanErrors).ToArray();

            User serverUser = await server.GetUserAsync(userId);
            await serverUser.AddRolesPersistedAsync(validRoles.Select(role => role.Id).Union(phantomRoles), start + timeSpan);

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

            if (errors.Length > 0) {
                await Context.Channel.CreateResponse()
                    .WithSubject(userId, socketUser?.GetAvatarUrl(size: 64))
                    .SendGenericTimedMixedAsync($"Gave roles to {persistedIdentifier}{newRolesList}", start, timeSpan, errors);
            }

            else {
                await Context.Channel.CreateResponse()
                    .WithSubject(userId, socketUser?.GetAvatarUrl(size: 64))
                    .SendGenericTimedSuccessAsync($"Gave roles to {persistedIdentifier}{newRolesList}", start, timeSpan);
            }

            if (server.HasLogChannel) {
                string persisterIdentifier = CommandHelper.GetUserIdentifier(Context.User.Id, Context.User);

                await Context.Guild.GetTextChannel(server.LogChannelId).CreateResponse()
                    .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                    .LogGenericTimedSuccessAsync($"{persisterIdentifier} gave roles to {persistedIdentifier}{newRolesList}", start, timeSpan);
            }
        }

        private async Task CheckCommandImpl(ulong userId) {
            Server server = await Context.Guild.GetServerAsync();
            IEnumerable<ulong> userRoleIds = Context.User.Roles.Select(role => role.Id);

            if (!await server.UserMayCheckRolePersists(Context.User.Id, userRoleIds)) {
                await Context.Channel.CreateResponse()
                    .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                    .SendNoPermissionAsync();

                return;
            }

            User serverUser = await server.GetUserAsync(userId);
            if (serverUser == null) {
                await Context.Channel.CreateResponse()
                    .WithSubject(userId, null)
                    .SendUserNotFoundAsync(userId);

                return;
            }

            IEnumerable<RolePersist> rolePersists = serverUser.GetRolesPersisted();
            SocketGuildUser socketUser = Context.Guild.GetUser(userId);

            if (!rolePersists.Any()) {
                await Context.Channel.CreateResponse()
                    .WithSubject(userId, socketUser?.GetAvatarUrl(size: 64))
                    .SendGenericSuccessAsync("User has no role persists");

                return;
            }

            StringBuilder rolePersistsBuilder = new StringBuilder();
            foreach (RolePersist rolePersist in rolePersists) {
                SocketRole socketRole = Context.Guild.Roles.FirstOrDefault(role => role.Id == rolePersist.RoleId);

                string roleIdentifier = CommandHelper.GetRoleIdentifier(rolePersist.RoleId, socketRole);
                rolePersistsBuilder.Append(roleIdentifier);

                if (rolePersist.Expiry != null) {
                    string timestamp = CommandHelper.GetResponseTimeStamp(rolePersist.Expiry.Value);
                    rolePersistsBuilder.Append(" until ").Append(timestamp);
                }

                rolePersistsBuilder.Append(DiscordBot6.DiscordNewLine);
            }

            await Context.Channel.CreateResponse()
                .WithSubject(userId, socketUser?.GetAvatarUrl(size: 64))
                .SendGenericSuccessAsync(rolePersistsBuilder.ToString());
        }
    }
}
