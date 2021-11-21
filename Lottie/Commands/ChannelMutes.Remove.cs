using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lottie.Commands.Contexts;
using Lottie.Database;
using Lottie.Helpers;
using System.Threading.Tasks;

namespace Lottie.Commands {
    [Group("cmute")]
    public sealed class ChannelMutePersists_Remove : ModuleBase<SocketGuildCommandContext> {
        [Command("remove")]
        public async Task Command(params string[] arguments) {
            ArgumentsHelper argumentsHelper = ArgumentsHelper.ExtractFromArguments(Context, arguments)
                .WithUser(out SocketGuildUser callee, out string[] calleeErrors)
                .WithVoiceChannel(out SocketVoiceChannel voiceChannel, out string[] voiceChannelErrors);

            if (!await argumentsHelper.AssertArgumentAsync(callee, calleeErrors)) {
                return;
            }

            if (callee.VoiceChannel == null && !await argumentsHelper.AssertArgumentAsync(voiceChannel, voiceChannelErrors)) {
                return;
            }

            if (!argumentsHelper.AssertOptionalArgument(voiceChannel)) {
                voiceChannel = callee.VoiceChannel;
            }

            Server server = await Context.Guild.L_GetServerAsync();
            if (!await server.UserMatchesConstraintsAsync(ConstraintIntents.CHANNELMUTE_REMOVE, null, Context.User.GetRoleIds(), Context.User.Id)) {
                await Context.Channel.CreateResponse()
                    .WithUserSubject(Context.User)
                    .SendNoPermissionsAsync();

                return;
            }
                      
            User user = await server.GetUserAsync(callee.Id);
            bool removedMutePersist = await user.RemoveMutePersistedAsync(voiceChannel.Id);

            if (removedMutePersist) {
                if (callee.IsMuted) {
                    user.AddVoiceStatusUpdate(-1, 0);
                    await callee.ModifyAsync(properties => properties.Mute = false);
                }

                await AcknowledgeUnmute(Context.Channel, callee, voiceChannel);
                if (server.HasLogChannel) {
                    await LogUnmute(Context.Guild.GetTextChannel(server.LogChannelId), Context.User, callee, voiceChannel);
                }
            }

            else {
                await Context.Channel.CreateResponse()
                    .AsFailure()
                    .WithUserSubject(callee)
                    .WithText("Whoops!")
                    .WithErrors($"User is not mute persisted in {CommandHelper.GetChannelIdentifier(voiceChannel.Id, voiceChannel)}")
                    .SendMessageAsync();
            }
        }

        private async Task AcknowledgeUnmute(ISocketMessageChannel messageChannel, SocketGuildUser callee, IVoiceChannel voiceChannel) {
            string calleeIdentifier = CommandHelper.GetUserIdentifier(callee.Id, callee);
            string channelIdentifier = CommandHelper.GetChannelIdentifier(voiceChannel.Id, voiceChannel);

            string message = $"Unmuted {calleeIdentifier}";

            ResponseBuilder responseBuilder = messageChannel.CreateResponse();

            responseBuilder.AsSuccess();
            responseBuilder.WithText(message);
            responseBuilder.WithUserSubject(callee);
            responseBuilder.WithField("Channel", channelIdentifier);

            await responseBuilder.SendMessageAsync();
        }

        private async Task LogUnmute(ISocketMessageChannel messageChannel, SocketGuildUser caller, SocketGuildUser callee, IVoiceChannel voiceChannel) {
            string callerIdentifier = CommandHelper.GetUserIdentifier(caller.Id, caller);
            string calleeIdentifier = CommandHelper.GetUserIdentifier(callee.Id, callee);
            string channelIdentifier = CommandHelper.GetChannelIdentifier(voiceChannel.Id, voiceChannel);

            string message = $"{callerIdentifier} unmuted {calleeIdentifier}";

            ResponseBuilder responseBuilder = messageChannel.CreateResponse();
            
            responseBuilder.AsLog();
            responseBuilder.WithText(message);
            responseBuilder.WithUserSubject(callee);
            responseBuilder.WithField("Channel", channelIdentifier);

            await responseBuilder.SendMessageAsync();
        }
    }
}
