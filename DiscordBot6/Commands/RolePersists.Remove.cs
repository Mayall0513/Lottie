﻿using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Commands.Contexts;
using DiscordBot6.Helpers;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("rolepersist")]
    public sealed class RolePersists_Remove : ModuleBase<SocketGuildCommandContext> {
        [Command("remove")]
        public async Task Command(params string[] arguments) {
            ArgumentsHelper argumentsHelper = ArgumentsHelper.ExtractFromArguments(Context, arguments)
                .WithUser(out SocketGuildUser callee, out string[] calleeErrors)
                .WithRole(out SocketRole role, out string[] roleErrors);

            if (!await argumentsHelper.AssertArgumentAsync(callee, calleeErrors)) {
                return;
            }

            if (!await argumentsHelper.AssertArgumentAsync(role, roleErrors)) {
                return;
            }

            Server server = await Context.Guild.GetServerAsync();
            if (!await server.UserMatchesConstraints(Database.ConstraintIntents.ROLEPERSIST_REMOVE, null, Context.User.GetRoleIds(), Context.User.Id)) {
                await Context.Channel.CreateResponse()
                    .WithUserSubject(Context.User)
                    .SendNoPermissionsAsync();

                return;
            }

            User user = await server.GetUserAsync(callee.Id);
            bool removeRolePersist = await user.RemoveRolePersistedAsync(role.Id);

            if (removeRolePersist) {
                if (Context.Guild.MayEditRole(role, callee)) {
                    await callee.RemoveRoleAsync(role);
                }
                
                await AcknowledgeRoleUnpersist(Context.Channel, callee, role);
                if (server.HasLogChannel) {
                    await LogRoleUnpersist(Context.Guild.GetTextChannel(server.LogChannelId), Context.User, callee, role);
                }
            }

            else {
                await Context.Channel.CreateResponse()
                    .AsFailure()
                    .WithUserSubject(callee)
                    .WithText("Whoops!")
                    .WithErrors($"User doesn't have {CommandHelper.GetRoleIdentifier(role.Id, role)}")
                    .SendMessageAsync();
            }
        }

        private async Task AcknowledgeRoleUnpersist(ISocketMessageChannel messageChannel, SocketGuildUser callee, SocketRole role) {
            string calleeIdentifier = CommandHelper.GetUserIdentifier(callee.Id, callee);
            string roleIdentifier = CommandHelper.GetRoleIdentifier(role.Id, role);

            string message = $"Took {roleIdentifier} from {calleeIdentifier}";

            ResponseBuilder responseBuilder = messageChannel.CreateResponse();
            responseBuilder.AsSuccess();
            responseBuilder.WithText(message);
            responseBuilder.WithUserSubject(callee);

            await responseBuilder.SendMessageAsync();
        }

        private async Task LogRoleUnpersist(ISocketMessageChannel messageChannel, SocketGuildUser caller, SocketGuildUser callee, SocketRole role) {
            string callerIdentifier = CommandHelper.GetUserIdentifier(caller.Id, caller);
            string roleIdentifier = CommandHelper.GetRoleIdentifier(role.Id, role);
            string calleeIdentifier = CommandHelper.GetUserIdentifier(callee.Id, callee);

            string message = $"{callerIdentifier} took {roleIdentifier} from {calleeIdentifier}";

            ResponseBuilder responseBuilder = messageChannel.CreateResponse();
            responseBuilder.AsLog();
            responseBuilder.WithText(message);
            responseBuilder.WithUserSubject(callee);

            await responseBuilder.SendMessageAsync();
        }
    }
}
