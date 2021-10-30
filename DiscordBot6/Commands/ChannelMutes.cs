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
        public static readonly TimeSpan MinimumMuteTimeSpan = TimeSpan.FromSeconds(30);

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
                    await Context.Channel.SendNoPermissionAsync(socketGuildUser);
                    return;
                }

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser == null) { // the user whose id was given does not exist
                    await Context.Channel.SendUserNotFoundAsync(userId);
                    return;
                }

                if (guildUser.VoiceChannel == null) { // the user whose id was given is not in a voice chat
                    await Context.Channel.SendUserNotInVoiceChannelAsync(guildUser);
                    return;
                }

                bool parsedTimeSpan = CommandHelper.GetTimeSpan(arguments, out TimeSpan timeSpan, out string[] errors, MinimumMuteTimeSpan);

                if (parsedTimeSpan) {
                    DateTime start = DateTime.UtcNow;
                    User serverUser = await server.GetUserAsync(userId);
                    bool wasMutePersisted = serverUser.IsMutePersisted(guildUser.VoiceChannel.Id);
                    await serverUser.AddMutePersistedAsync(guildUser.VoiceChannel.Id, start + timeSpan);

                    if (!wasMutePersisted && !guildUser.IsMuted) {
                        serverUser.IncrementVoiceStatusUpdated();
                        await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                    }

                    string channelIdentifier = $"<#{guildUser.VoiceChannel.Id}> (`{guildUser.VoiceChannel.Name}`)";
                    string mutedIdentifier = $"{guildUser.Mention} (`{guildUser.Username}`)";

                    await Context.Channel.SendTimedChannelMuteSuccessAsync(guildUser.Id, guildUser.GetAvatarUrl(size: 64), $"Muted {mutedIdentifier}", channelIdentifier, start, timeSpan);
                    if (server.HasLogChannel) {
                        await Context.Guild.GetTextChannel(server.LogChannelId)
                            .LogTimedChannelMuteSuccessAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), $"{socketGuildUser.Mention} (`{socketGuildUser.Username}`) muted {mutedIdentifier}", channelIdentifier, start, timeSpan);
                    }
                }

                else {
                    await Context.Channel.SendGenericErrorAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), errors);
                }
            }
        }

        private async Task AddCommandImpl(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayMute(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.SendNoPermissionAsync(socketGuildUser);
                    return;
                }

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser == null) { // the user whose id was given does not exist
                    await Context.Channel.SendUserNotFoundAsync(userId);
                    return;
                }

                if (guildUser.VoiceChannel == null) { // the user whose id was given is not in a voice chat
                    await Context.Channel.SendUserNotInVoiceChannelAsync(guildUser);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                await serverUser.AddMutePersistedAsync(guildUser.VoiceChannel.Id, null);

                if (!guildUser.IsMuted) {
                    serverUser.IncrementVoiceStatusUpdated();
                    await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                }

                string channelIdentifier = $"<#{guildUser.VoiceChannel.Id}> (`{guildUser.VoiceChannel.Name}`)";
                string messageSuffix = $"{guildUser.Mention} (`{guildUser.Username}`) permanently";

                await Context.Channel.SendChannelMuteSuccessAsync(guildUser.Id, guildUser.GetAvatarUrl(size: 64), $"Muted {messageSuffix}", channelIdentifier);
                if (server.HasLogChannel) {
                    await Context.Guild.GetTextChannel(server.LogChannelId)
                        .LogChannelMuteSuccessAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), $"{socketGuildUser.Mention} muted {messageSuffix}", channelIdentifier);
                }
            }
        }

        private async Task CheckCommandImpl(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayCheckMutePersists(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.SendNoPermissionAsync(socketGuildUser);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                if (serverUser == null) {
                    await Context.Channel.SendUserNotFoundAsync(userId);
                    return;
                }

                IEnumerable<MutePersist> mutePersists = serverUser.GetMutesPersisted();
                SocketGuildUser guildUser = Context.Guild.GetUser(userId);

                if (!mutePersists.Any()) {
                    await Context.Channel.SendGenericSuccessAsync(userId, guildUser?.GetAvatarUrl(size: 64), "User has no channel mute persists");
                    return;
                }

                StringBuilder mutePersistsBuilder = new StringBuilder();
                foreach (MutePersist mutePersist in mutePersists) {
                    SocketVoiceChannel voiceChannel = Context.Guild.GetVoiceChannel(mutePersist.ChannelId);

                    if (voiceChannel == null) {
                        mutePersistsBuilder.Append("`").Append(mutePersist.ChannelId).Append("`");
                    }

                    else {
                        mutePersistsBuilder.Append("<#").Append(mutePersist.ChannelId).Append("> (`").Append(voiceChannel.Name).Append("`)");
                    }
                    

                    if (mutePersist.Expiry != null) {
                        mutePersistsBuilder.Append(" until ").Append(CommandHelper.GetResponseTimeStamp(mutePersist.Expiry.Value)).Append("");
                    }

                    mutePersistsBuilder.Append(DiscordBot6.DiscordNewLine);
                }

                await Context.Channel.SendGenericSuccessAsync(userId, guildUser?.GetAvatarUrl(size: 64), mutePersistsBuilder.ToString());
            }
        }
    }
}
