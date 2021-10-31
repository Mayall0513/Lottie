using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Helpers;
using DiscordBot6.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("channelmute")]
    public sealed class ChannelMutes : ModuleBase<SocketCommandContext> {
        private static readonly TimeSpan minimumMuteTimeSpan = TimeSpan.FromSeconds(30);

        [Command("add")]
        public async Task AddCommand(ulong id) {
            await AddCommandImpl(id);
        }

        [Command("add")]
        public async Task AddCommand(IUser user) {
            await AddCommandImpl(user.Id);
        }

        [Command("add")]
        public async Task AddCommand(ulong id, params string[] arguments) {
            await AddCommandImpl(id, arguments);
        }

        [Command("add")]
        public async Task AddCommand(IUser user, params string[] arguments) {
            await AddCommandImpl(user.Id, arguments);
        }

        [Command("check")]
        public async Task CheckCommand(ulong id) {
            await CheckCommandImpl(id);
        }

        [Command("check")]
        public async Task CheckCommand(IUser user) {
            await CheckCommandImpl(user.Id);
        }


        private async Task AddCommandImpl(ulong userId, string[] arguments) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayTempMute(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.CreateResponse()
                        .WithSubject(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64))
                        .SendNoPermissionAsync();

                    return;
                }

                SocketGuildUser socketUser = Context.Guild.GetUser(userId);
                if (socketUser == null) { // the user whose id was given does not exist
                    await Context.Channel.CreateResponse()
                        .WithSubject(userId, null)
                        .SendUserNotFoundAsync(userId);

                    return;
                }

                if (socketUser.VoiceChannel == null) { // the user whose id was given is not in a voice chat
                    await Context.Channel.CreateResponse()
                        .WithSubject(socketUser.Id, socketUser.GetAvatarUrl(size: 64))
                        .SendUserNotInVoiceChannelAsync(socketUser);

                    return;
                }

                bool parsedTimeSpan = CommandHelper.GetTimeSpan(arguments, out TimeSpan timeSpan, out string[] errors, minimumMuteTimeSpan);

                if (parsedTimeSpan) {
                    DateTime start = DateTime.UtcNow;
                    User serverUser = await server.GetUserAsync(userId);
                    await serverUser.AddMutePersistedAsync(socketUser.VoiceChannel.Id, start + timeSpan);

                    if (!socketUser.IsMuted) {
                        serverUser.IncrementVoiceStatusUpdated();
                        await socketUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                    }

                    string mutedIdentifier = CommandHelper.GetUserIdentifier(userId, socketUser);
                    string channelIdentifier = CommandHelper.GetChannelIdentifier(socketUser.VoiceChannel.Id, socketUser.VoiceChannel);

                    await Context.Channel.CreateResponse()
                        .WithSubject(userId, socketUser.GetAvatarUrl(size: 64))
                        .SendTimedChannelMuteSuccessAsync($"Muted {mutedIdentifier}", channelIdentifier, start, timeSpan);

                    if (server.HasLogChannel) {
                        string muterIdentifier = CommandHelper.GetUserIdentifier(socketGuildUser.Id, socketGuildUser);

                        await Context.Guild.GetTextChannel(server.LogChannelId).CreateResponse()
                            .WithSubject(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64))
                            .LogTimedChannelMuteSuccessAsync($"{muterIdentifier} muted {mutedIdentifier}", channelIdentifier, start, timeSpan);
                    }
                }

                else {
                    await Context.Channel.CreateResponse()
                        .WithSubject(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64))
                        .SendGenericErrorsAsync(errors);
                }
            }
        }

        private async Task AddCommandImpl(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayMute(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.CreateResponse()
                        .WithSubject(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64))
                        .SendNoPermissionAsync();

                    return;
                }

                SocketGuildUser socketUser = Context.Guild.GetUser(userId);
                if (socketUser == null) { // the user whose id was given does not exist
                    await Context.Channel.CreateResponse()
                        .WithSubject(userId, null)
                        .SendUserNotFoundAsync(userId);

                    return;
                }

                if (socketUser.VoiceChannel == null) { // the user whose id was given is not in a voice chat
                    await Context.Channel.CreateResponse()
                        .WithSubject(socketUser.Id, socketUser.GetAvatarUrl(size: 64))
                        .SendUserNotInVoiceChannelAsync(socketUser);

                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                await serverUser.AddMutePersistedAsync(socketUser.VoiceChannel.Id, null);

                if (!socketUser.IsMuted) {
                    serverUser.IncrementVoiceStatusUpdated();
                    await socketUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                }

                string mutedIdentifier = CommandHelper.GetUserIdentifier(userId, socketUser);
                string channelIdentifier = CommandHelper.GetChannelIdentifier(socketUser.VoiceChannel.Id, socketUser.VoiceChannel);

                await Context.Channel.CreateResponse()
                    .WithSubject(userId, socketUser.GetAvatarUrl(size: 64))
                    .SendChannelMuteSuccessAsync($"Muted {mutedIdentifier} permanently", channelIdentifier);

                if (server.HasLogChannel) {
                    string muterIdentifier = CommandHelper.GetUserIdentifier(socketGuildUser.Id, socketGuildUser);

                    await Context.Guild.GetTextChannel(server.LogChannelId).CreateResponse()
                        .WithSubject(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64))
                        .LogChannelMuteSuccessAsync($"{muterIdentifier} muted {mutedIdentifier} permanently", channelIdentifier);
                }
            }
        }

        private async Task CheckCommandImpl(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayCheckMutePersists(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.CreateResponse()
                         .WithSubject(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64))
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

                IEnumerable<MutePersist> mutePersists = serverUser.GetMutesPersisted();
                SocketGuildUser socketUser = Context.Guild.GetUser(userId);

                if (!mutePersists.Any()) { 
                    await Context.Channel.CreateResponse()
                        .WithSubject(userId, socketUser?.GetAvatarUrl(size: 64))
                        .SendGenericSuccessAsync("User has no channel mute persists");

                    return;
                }

                StringBuilder mutePersistsBuilder = new StringBuilder();
                foreach (MutePersist mutePersist in mutePersists) {
                    SocketVoiceChannel voiceChannel = Context.Guild.GetVoiceChannel(mutePersist.ChannelId);

                    string channelIdentifier = CommandHelper.GetChannelIdentifier(voiceChannel.Id, voiceChannel);
                    mutePersistsBuilder.Append(channelIdentifier);

                    if (mutePersist.Expiry != null) {
                        string timestamp = CommandHelper.GetResponseTimeStamp(mutePersist.Expiry.Value);
                        mutePersistsBuilder.Append(" until ").Append(timestamp);
                    }

                    mutePersistsBuilder.Append(DiscordBot6.DiscordNewLine);
                }

                await Context.Channel.CreateResponse()
                    .WithSubject(userId, socketUser?.GetAvatarUrl(size: 64))
                    .SendGenericSuccessAsync(mutePersistsBuilder.ToString());
            }
        }
    }
}
