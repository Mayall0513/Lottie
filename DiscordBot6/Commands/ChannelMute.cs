﻿using Discord;
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
                IEnumerable<ulong> userIds = socketGuildUser.Roles.Select(x => x.Id);

                if (!await server.UserMayTempMute(socketGuildUser.Id, userIds)) {
                    await ResponseHelper.SendNoPermissionsResponse(Context.Channel);
                    return;
                }

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser == null) { // the user whose id was given does not exist
                    await ResponseHelper.SendUserNotFoundResponse(Context.Channel, userId);
                    return;
                }

                if (guildUser.VoiceChannel == null) { // the user whose id was given is not in a voice chat
                    await ResponseHelper.SendUserNotInVoiceChannelResponse(Context.Channel, userId);
                    return;
                }

                bool parsedTimeSpan = CommandHelper.GetTimeSpan(arguments, out TimeSpan timeSpan, out string[] errors, MinimumMuteTimeSpan);

                if (parsedTimeSpan) {
                    DateTime start = DateTime.UtcNow;
                    User serverUser = await server.GetUserAsync(userId);

                    if (await serverUser.AddMutePersistedAsync(guildUser.VoiceChannel.Id, start + timeSpan)) {
                        if (!guildUser.IsMuted) {
                            serverUser.IncrementVoiceStatusUpdated();
                            await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                        }

                        await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateTimeSpanSimpleSuccessEmbed($"Muted {guildUser.Mention} in `{guildUser.VoiceChannel.Name}`", start, timeSpan));
                        if (server.HasLogChannel) {
                            await Context.Guild.GetTextChannel(server.LogChannelId).SendMessageAsync(embed: MessageHelper.CreateTimeSpanSimpleInfoEmbed($"{socketGuildUser.Mention} muted {guildUser.Mention}", start, timeSpan));
                        }
                    }

                    else {
                        await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateTimeSpanSimpleSuccessEmbed($"Updated {guildUser.Mention}'s mute in `{guildUser.VoiceChannel.Name}`", start, timeSpan));
                        if (server.HasLogChannel) {
                            await Context.Guild.GetTextChannel(server.LogChannelId).SendMessageAsync(embed: MessageHelper.CreateTimeSpanSimpleInfoEmbed($"{socketGuildUser.Mention} updated {guildUser.Mention}'s mute", start, timeSpan));
                        }
                    }
                }

                else {
                    await ResponseHelper.SendTimeSpanFormatResponse(Context.Channel, errors);
                }
            }
        }

        private async Task CommandImpl(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();
                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(x => x.Id);

                if (!await server.UserMayMute(socketGuildUser.Id, userRoleIds)) {
                    await ResponseHelper.SendNoPermissionsResponse(Context.Channel);
                    return;
                }

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser == null) { // the user whose id was given does not exist
                    await ResponseHelper.SendUserNotFoundResponse(Context.Channel, userId);
                    return;
                }

                if (guildUser.VoiceChannel == null) { // the user whose id was given is not in a voice chat
                    await ResponseHelper.SendUserNotInVoiceChannelResponse(Context.Channel, userId);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                await serverUser.AddMutePersistedAsync(guildUser.VoiceChannel.Id, null);

                if (!guildUser.IsMuted) {
                    serverUser.IncrementVoiceStatusUpdated();
                    await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                }

                await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateSimpleSuccessEmbed($"Muted {guildUser.Mention} in `{guildUser.VoiceChannel.Name}` permanently"));
            }
        }
    }
}
