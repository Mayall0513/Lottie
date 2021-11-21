using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lottie.Commands.Contexts;
using Lottie.Database;
using Lottie.Helpers;
using System;
using System.Threading.Tasks;

namespace Lottie.Commands {
    [Group("cmute")]
    public sealed class ChannelMutePersists_Add : ModuleBase<SocketGuildCommandContext> {
        private static readonly TimeSpan minimumTimeSpan = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan maximumTimeSpan = TimeSpan.FromHours(1);

        [Command("add")]
        public async Task Command(params string[] arguments) {
            ArgumentsHelper argumentsHelper = ArgumentsHelper.ExtractFromArguments(Context, arguments)
                .WithUser(out SocketGuildUser callee, out string[] calleeErrors)
                .WithTimeSpan(out TimeSpan? timeSpan, out string[] _)
                .WithPresetMessage(out ulong? presetMessageId, out string[] presetMessageErrors)
                .WithCustomReason(out string customMessage, out string[] _);

            if (!await argumentsHelper.AssertArgumentAsync(callee, calleeErrors)) {
                return;
            }

            if (callee.VoiceChannel == null) {
                await Context.Channel.CreateResponse()
                    .AsFailure()
                    .SendUserNotInVoiceChannelAsync(callee);

                return;
            }

            if (!await argumentsHelper.AssertArgumentAsync(presetMessageId, presetMessageErrors)) {
                return;
            }

            bool hasTimeSpan = argumentsHelper.AssertOptionalArgument(timeSpan);
            if (hasTimeSpan && !await argumentsHelper.AssertTimeSpanRangeAsync(timeSpan.Value, minimumTimeSpan, maximumTimeSpan)) {
                return;
            }

            ConstraintIntents userConstraints = hasTimeSpan ? ConstraintIntents.CHANNELMUTE_GIVE_TEMPORARY : ConstraintIntents.CHANNELMUTE_GIVE_PERMANENT;
            Server server = await Context.Guild.L_GetServerAsync();

            if (!await server.UserMatchesConstraintsAsync(userConstraints, null, Context.User.GetRoleIds(), Context.Channel.Id)) {
                await Context.Channel.CreateResponse()
                    .WithUserSubject(Context.User)
                    .SendNoPermissionsAsync();

                return;
            }

            PresetMessageTypes presetMessageType = hasTimeSpan ? PresetMessageTypes.CHANNELMUTE_TEMPORARY : PresetMessageTypes.CHANNELMUTE_PERMANENT;
            (bool presetAssertion, string presetMessage) = await argumentsHelper.AssertPresetReasonAsync(server, presetMessageType, presetMessageId.Value);
            if (!presetAssertion) {
                return;
            }

            if (hasTimeSpan) {
                presetMessage = string.Format(presetMessage, CommandHelper.GetResponseTimeSpan(timeSpan.Value));
            }

            User user = await server.GetUserAsync(callee.Id);
            await user.AddMutePersistedAsync(callee.VoiceChannel.Id, hasTimeSpan ? Context.CommandTime + timeSpan.Value : (DateTime?) null);

            if (!callee.IsMuted) {
                user.AddVoiceStatusUpdate(1, 0);
                await callee.ModifyAsync(properties => properties.Mute = true); 
            }

            await AcknowledgeMute(Context.Channel, callee, callee.VoiceChannel, Context.CommandTime, timeSpan);
            await SendReasonDM(presetMessage, callee);

            if (server.HasLogChannel) {
                await LogMute(Context.Guild.GetTextChannel(server.LogChannelId), Context.User, callee, callee.VoiceChannel, Context.CommandTime, timeSpan, presetMessageId.Value, customMessage);
            }
        }

        private async Task AcknowledgeMute(ISocketMessageChannel messageChannel, SocketGuildUser callee, IVoiceChannel voiceChannel, DateTime start, TimeSpan? timeSpan) {
            string calleeIdentifier = CommandHelper.GetUserIdentifier(callee.Id, callee);
            string channelIdentifier = CommandHelper.GetChannelIdentifier(voiceChannel.Id, voiceChannel);

            string message = timeSpan.HasValue ? $"Muted {calleeIdentifier}" : $"Muted {calleeIdentifier} permanently";

            ResponseBuilder responseBuilder = messageChannel.CreateResponse();

            responseBuilder.AsSuccess();
            responseBuilder.WithText(message);
            responseBuilder.WithUserSubject(callee);
            responseBuilder.WithField("Channel", channelIdentifier);

            if (timeSpan.HasValue) {
                responseBuilder.WithTimeSpan(start, timeSpan.Value);
            }

            await responseBuilder.SendMessageAsync();
        }

        private async Task LogMute(ISocketMessageChannel messageChannel, SocketGuildUser caller, SocketGuildUser callee, IVoiceChannel voiceChannel, DateTime start, TimeSpan? timeSpan, ulong presetMessageId, string customMessage) {
            string callerIdentifier = CommandHelper.GetUserIdentifier(caller.Id, caller);
            string calleeIdentifier = CommandHelper.GetUserIdentifier(callee.Id, callee);
            string channelIdentifier = CommandHelper.GetChannelIdentifier(voiceChannel.Id, voiceChannel);

            string message = timeSpan.HasValue ? $"{callerIdentifier} muted {calleeIdentifier}" : $"{callerIdentifier} muted {calleeIdentifier} permanently";

            ResponseBuilder responseBuilder = messageChannel.CreateResponse();
            
            responseBuilder.AsLog();
            responseBuilder.WithText(message);
            responseBuilder.WithUserSubject(callee);
            responseBuilder.WithField("Channel", channelIdentifier);

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
