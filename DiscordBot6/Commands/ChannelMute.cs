using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("channelmute")]
    public sealed class ChannelMute : ModuleBase<SocketCommandContext> {
        public static readonly TimeSpan MinimumMuteTimeSpan = TimeSpan.FromSeconds(30);

        [Command]
        public async Task Command(ulong id) {
            await CommandImpl(id);
        }

        [Command]
        public async Task Command(IUser user) {
            await CommandImpl(user.Id);
        }

        [Command]
        public async Task Command(ulong id, params string[] arguments) {
            await CommandImpl(id, arguments);
        }

        [Command]
        public async Task Command(IUser user, params string[] arguments) {
            await CommandImpl(user.Id, arguments); 
        }

        private async Task CommandImpl(ulong userId, string[] arguments) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayTempMute(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.SendNoPermissionResponseAsync(socketGuildUser);
                    return;
                }

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser == null) { // the user whose id was given does not exist
                    await Context.Channel.SendUserNotFoundResponseAsync(userId);
                    return;
                }

                if (guildUser.VoiceChannel == null) { // the user whose id was given is not in a voice chat
                    await Context.Channel.SendUserNotInVoiceChannelResponseAsync(guildUser);
                    return;
                }

                bool parsedTimeSpan = CommandHelper.GetTimeSpan(arguments, out TimeSpan timeSpan, out string[] errors, MinimumMuteTimeSpan);

                if (parsedTimeSpan) {
                    DateTime start = DateTime.UtcNow;
                    User serverUser = await server.GetUserAsync(userId);
                    bool wasMutePersisted = serverUser.IsMutePersisted(guildUser.VoiceChannel.Id);
                    await serverUser.AddMutePersistedAsync(guildUser.VoiceChannel.Id, start + timeSpan);

                    if (!wasMutePersisted && !guildUser.IsMuted) {
                        await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                    }

                    string messageSuffix = $"{guildUser.Mention} in <#{guildUser.VoiceChannel.Id}> for `{timeSpan}`";

                    await Context.Channel.SendTimeFrameSuccessResponseAsync(guildUser.Id, guildUser.GetAvatarUrl(size: 64), $"Muted {messageSuffix}", start, timeSpan);
                    if (server.HasLogChannel) {
                        await Context.Guild.GetTextChannel(server.LogChannelId)
                            .LogTimeFrameSuccessResponseAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), $"{socketGuildUser.Mention} muted {messageSuffix}", start, timeSpan);
                    }
                }

                else {
                    await Context.Channel.SendGenericErrorResponseAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), errors);
                }
            }
        }

        private async Task CommandImpl(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayMute(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.SendNoPermissionResponseAsync(socketGuildUser);
                    return;
                }

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser == null) { // the user whose id was given does not exist
                    await Context.Channel.SendUserNotFoundResponseAsync(userId);
                    return;
                }

                if (guildUser.VoiceChannel == null) { // the user whose id was given is not in a voice chat
                    await Context.Channel.SendUserNotInVoiceChannelResponseAsync(guildUser);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                await serverUser.AddMutePersistedAsync(guildUser.VoiceChannel.Id, null);

                if (!guildUser.IsMuted) {
                    serverUser.IncrementVoiceStatusUpdated();
                    await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                }

                string messageSuffix = $"{guildUser.Mention} in <#{guildUser.VoiceChannel.Id}> permanently";

                await Context.Channel.SendGenericSuccessResponseAsync(guildUser.Id, guildUser.GetAvatarUrl(size: 64), $"Muted {messageSuffix}");
                if(server.HasLogChannel) {
                    await Context.Guild.GetTextChannel(server.LogChannelId)
                        .LogGenericSuccessAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), $"{socketGuildUser.Mention} muted {messageSuffix}");
                }
            }
        }
    }
}
