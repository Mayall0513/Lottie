using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lottie.Commands.Contexts;
using Lottie.Database;
using Lottie.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lottie.Commands {
    [Group("jail")]
    public sealed class Jail : ModuleBase<SocketGuildCommandContext> {
        private static readonly TimeSpan minimumTimeSpan = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan maximumTimeSpan = TimeSpan.FromHours(4);

        [Command]
        public async Task Command(params string[] arguments) {
            ArgumentsHelper argumentsHelper = ArgumentsHelper.ExtractFromArguments(Context, arguments)
                .WithUser(out SocketGuildUser callee, out string[] calleeErrors)
                .WithTimeSpan(out TimeSpan? timeSpan, out string[] _)
                .WithPresetMessage(out ulong? presetMessageId, out string[] presetMessageErrors)
                .WithCustomReason(out string customMessage, out string[] _);

            if (!await argumentsHelper.AssertArgumentAsync(callee, calleeErrors)) {
                return;
            }

            if (!await argumentsHelper.AssertArgumentAsync(presetMessageId, presetMessageErrors)) {
                return;
            }

            bool hasTimeSpan = argumentsHelper.AssertOptionalArgument(timeSpan);
            if (hasTimeSpan && !await argumentsHelper.AssertTimeSpanRangeAsync(timeSpan.Value, minimumTimeSpan, maximumTimeSpan)) {
                return;
            }

            ConstraintIntents userConstraints = hasTimeSpan ? ConstraintIntents.JAIL_TEMPORARY : ConstraintIntents.JAIL_PERMANENT;
            Server server = await Context.Guild.L_GetServerAsync();

            if (!await server.UserMatchesConstraintsAsync(userConstraints, null, Context.User.GetRoleIds(), Context.Channel.Id)) {
                await Context.Channel.CreateResponse()
                    .WithUserSubject(Context.User)
                    .SendNoPermissionsAsync();

                return;
            }

            PresetMessageTypes presetMessageType = hasTimeSpan ? PresetMessageTypes.JAIL_TEMPORARY : PresetMessageTypes.JAIL_PERMANENT;
            (bool presetAssertion, string presetMessage) = await argumentsHelper.AssertPresetReasonAsync(server, presetMessageType, presetMessageId.Value);

            if (!presetAssertion) {
                return;
            }

            if (hasTimeSpan) {
                presetMessage = string.Format(presetMessage, CommandHelper.GetResponseTimeSpan(timeSpan.Value));
            }

            if (!server.HasJailRole) {
                await Context.Channel.CreateResponse()
                    .AsFailure()
                    .WithUserSubject(Context.User)
                    .WithText("Whoops!")
                    .WithErrors("This server has no jail role configured")
                    .SendMessageAsync();

                return;
            }

            SocketRole jailRole = Context.Guild.GetRole(server.JailRoleId);
            if (jailRole == null) {
                await Context.Channel.CreateResponse()
                    .AsFailure()
                    .WithUserSubject(Context.User)
                    .WithText("Whoops!")
                    .WithErrors($"Could not find role on server with id `{server.JailRoleId}`. Was it deleted?")
                    .SendMessageAsync();

                return;
            }
            if (!Context.Guild.MayEditRole(jailRole, Context.User)) {
                await Context.Channel.CreateResponse()
                    .AsFailure()
                    .WithUserSubject(Context.User)
                    .WithText("Whoops!")
                    .WithErrors($"Could not give `{CommandHelper.GetRoleIdentifier(jailRole.Id, jailRole)}` as it's above you or I in the server's role hierarchy")
                    .SendMessageAsync();

                return;
            }

            ulong[] roleId = new ulong[1] { jailRole.Id };
            User user = await server.GetUserAsync(callee.Id);

            user.AddMemberStatusUpdate(roleId, null);
            await user.ApplyContingentRolesAsync(callee, callee.GetRoleIds(), callee.GetRoleIds().Union(roleId));

            await user.AddRolesPersistedAsync(roleId, hasTimeSpan ? Context.CommandTime + timeSpan.Value : (DateTime?) null);
            await callee.AddRolesAsync(roleId);

            await AcknowledgeJail(Context.Channel, callee, jailRole, Context.CommandTime, timeSpan);
            await SendReasonDM(presetMessage, callee);

            if (server.HasLogChannel) {
                await LogJail(Context.Guild.GetTextChannel(server.LogChannelId), Context.User, callee, jailRole, Context.CommandTime, timeSpan, presetMessageId.Value, customMessage);
            }
        }

        private async Task AcknowledgeJail(ISocketMessageChannel messageChannel, SocketGuildUser callee, SocketRole role, DateTime start, TimeSpan? timeSpan) {
            string calleeIdentifier = CommandHelper.GetUserIdentifier(callee.Id, callee);
            string roleIdentifier = CommandHelper.GetRoleIdentifier(role.Id, role);

            string message = timeSpan.HasValue ? $"Jailed {calleeIdentifier}" : $"Jailed {calleeIdentifier} permanently";

            ResponseBuilder responseBuilder = messageChannel.CreateResponse();

            responseBuilder.AsSuccess();
            responseBuilder.WithText(message);
            responseBuilder.WithUserSubject(callee);
            responseBuilder.WithField("Role", roleIdentifier);

            if (timeSpan.HasValue) {
                responseBuilder.WithTimeSpan(start, timeSpan.Value);
            }

            await responseBuilder.SendMessageAsync();
        }

        private async Task LogJail(ISocketMessageChannel messageChannel, SocketGuildUser caller, SocketGuildUser callee, SocketRole role, DateTime start, TimeSpan? timeSpan, ulong presetMessageId, string customMessage) {
            string callerIdentifier = CommandHelper.GetUserIdentifier(caller.Id, caller);
            string calleeIdentifier = CommandHelper.GetUserIdentifier(callee.Id, callee);
            string roleIdentifier = CommandHelper.GetRoleIdentifier(role.Id, role);

            string message = timeSpan.HasValue ? $"{callerIdentifier} jailed {calleeIdentifier}" : $"{callerIdentifier} jailed {calleeIdentifier} permanently";

            ResponseBuilder responseBuilder = messageChannel.CreateResponse();

            responseBuilder.AsLog();
            responseBuilder.WithText(message);
            responseBuilder.WithUserSubject(callee);
            responseBuilder.WithField("Role", roleIdentifier);

            if (timeSpan != null) {
                responseBuilder.WithTimeSpan(start, timeSpan.Value);
            }

            responseBuilder.WithField("Preset Reason", string.Empty + presetMessageId, true);

            if (customMessage != null) {
                responseBuilder.WithField("Internal Reason", customMessage);
            }

            await responseBuilder.SendMessageAsync();
        }

        private async Task SendReasonDM(string presetMessage, SocketGuildUser callee) {
            await (await callee.CreateDMChannelAsync()).CreateResponse()
                .WithUserSubject(callee)
                .WithText(presetMessage)
                .WithColor(Color.LightOrange)
                .SendMessageAsync();
        }
    }
}
