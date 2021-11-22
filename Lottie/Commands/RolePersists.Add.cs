using Discord.Commands;
using Discord.WebSocket;
using Lottie.Commands.Contexts;
using Lottie.Database;
using Lottie.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lottie.Commands {
    [Group("rolepersist")]
    public sealed class RolePersists_Add : ModuleBase<SocketGuildCommandContext> {
        private static readonly TimeSpan minimumTimeSpan = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan maximumTimeSpan = TimeSpan.FromHours(1);

        [Command("add")]
        public async Task Command(params string[] arguments) {
            ArgumentsHelper argumentsHelper = ArgumentsHelper.ExtractFromArguments(Context, arguments)
                .WithUser(out SocketGuildUser callee, out string[] calleeErrors)
                .WithRoles(out SocketRole[] roles, out string[] rolesErrors)
                .WithTimeSpan(out TimeSpan? timeSpan, out string[] _);

            if (!await argumentsHelper.AssertArgumentAsync(callee, calleeErrors)) {
                return;
            }

            if (!await argumentsHelper.AssertArgumentAsync(roles, rolesErrors)) {
                return;
            }

            bool hasTimeSpan = argumentsHelper.AssertOptionalArgument(timeSpan);
            if (hasTimeSpan && !await argumentsHelper.AssertTimeSpanRangeAsync(timeSpan.Value, minimumTimeSpan, maximumTimeSpan)) {
                return;
            }

            ConstraintIntents userConstraints = hasTimeSpan ? ConstraintIntents.ROLEPERSIST_GIVE_TEMPORARY : ConstraintIntents.ROLEPERSIST_GIVE_PERMANENT;
            Server server = await Context.Guild.L_GetServerAsync();

            if (!await server.UserMatchesConstraintsAsync(userConstraints, null, Context.User.GetRoleIds(), Context.Channel.Id)) {
                await Context.Channel.CreateResponse()
                    .WithUserSubject(Context.User)
                    .SendNoPermissionsAsync();

                return;
            }

            IEnumerable<SocketRole> validRoles = roles.Where(role => Context.Guild.MayEditRole(role, Context.User));
            if (!validRoles.Any()) {
                await Context.Channel.CreateResponse()
                    .AsFailure()
                    .WithText("Whoops!")
                    .WithErrors("Could not give any of the roles listed because they are all above you or I in the server's role hierarchy")
                    .SendMessageAsync();

                return;
            }

            User user = await server.GetUserAsync(callee.Id);
            HashSet<ulong> validRoleIds = new HashSet<ulong>(validRoles.Select(role => role.Id));

            user.AddMemberStatusUpdate(validRoleIds, null);
            await user.ApplyContingentRolesAsync(callee, callee.GetRoleIds(), callee.GetRoleIds().Union(validRoleIds));

            await user.AddRolesPersistedAsync(validRoleIds, hasTimeSpan ? Context.CommandTime + timeSpan.Value : (DateTime?)null);
            await callee.AddRolesAsync(validRoles);

            string validRolesSuffix = new StringBuilder().Append(Lottie.DiscordNewLine).Append(Lottie.DiscordNewLine)
                .Append("**Roles:**").Append(Lottie.DiscordNewLine)
                .AppendJoin(Lottie.DiscordNewLine, validRoles.Select(role => CommandHelper.GetRoleIdentifier(role.Id, role))).ToString();

            IEnumerable<SocketRole> invalidRoles = roles.Where(role => !Context.Guild.MayEditRole(role, Context.User));
            string invalidRolesMessage = invalidRoles.Any() ? new StringBuilder().Append("Could not give the following roles because they are above you or I in the server's rike hierarchy:")
                .Append(Lottie.DiscordNewLine).Append(Lottie.DiscordNewLine)
                .AppendJoin(Lottie.DiscordNewLine, invalidRoles.Select(role => CommandHelper.GetRoleIdentifier(role.Id, role))).ToString() : null;

            await AcknowledgeRolePersist(Context.Channel, callee, validRolesSuffix, invalidRolesMessage, Context.CommandTime, timeSpan);
            if (server.HasLogChannel) {
                await LogRolePersist(Context.Guild.GetTextChannel(server.LogChannelId), Context.User, callee, validRolesSuffix, Context.CommandTime, timeSpan);
            }
        }

        private async Task AcknowledgeRolePersist(ISocketMessageChannel messageChannel, SocketGuildUser callee, string validRolesSuffix, string invalidRolesMessage, DateTime start, TimeSpan? timeSpan) {
            string calleeIdentifier = CommandHelper.GetUserIdentifier(callee.Id, callee);
            string validRolesPrefix = timeSpan.HasValue ? $"Gave roles to {calleeIdentifier}" : $"Gave roles to {calleeIdentifier} permanently";

            ResponseBuilder responseBuilder = messageChannel.CreateResponse();
            if (invalidRolesMessage != null) {
                responseBuilder.AsMixed();
                responseBuilder.WithErrors(invalidRolesMessage);
            }

            else {
                responseBuilder.AsSuccess();
            }
            
            responseBuilder.WithText(validRolesPrefix + validRolesSuffix);
            responseBuilder.WithUserSubject(callee);

            if (timeSpan.HasValue) {
                responseBuilder.WithTimeSpan(start, timeSpan.Value);
            }

            await responseBuilder.SendMessageAsync();
        }

        private async Task LogRolePersist(ISocketMessageChannel messageChannel, SocketGuildUser caller, SocketGuildUser callee, string validRolesSuffix, DateTime start, TimeSpan? timeSpan) {
            string callerIdentifier = CommandHelper.GetUserIdentifier(caller.Id, caller);
            string calleeIdentifier = CommandHelper.GetUserIdentifier(callee.Id, callee);

            string validRolesPrefix = timeSpan.HasValue ? $"{callerIdentifier} gave roles to {calleeIdentifier}" : $"{callerIdentifier} gave roles to {calleeIdentifier} permanently";

            ResponseBuilder responseBuilder = messageChannel.CreateResponse();

            responseBuilder.AsLog();
            responseBuilder.WithText(validRolesPrefix + validRolesSuffix);
            responseBuilder.WithUserSubject(callee);

            if (timeSpan != null) {
                responseBuilder.WithTimeSpan(start, timeSpan.Value);
            }

            await responseBuilder.SendMessageAsync();
        }
    }
}
