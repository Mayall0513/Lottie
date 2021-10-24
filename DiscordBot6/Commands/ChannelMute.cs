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
        public async Task Command(IUser user, params string[] arguments) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                SocketGuildUser guildUser = Context.Guild.GetUser(user.Id);
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userIds = socketGuildUser.Roles.Select(x => x.Id);

                if (arguments.Length == 0) { // no time constraints
                    if (!await server.UserMayMute(socketGuildUser.Id, userIds)) {
                        return;
                    }

                    if (guildUser == null) { // the user whose id was given does not exist
                        await Context.Channel.SendMessageAsync($"Could not find user with ID `{user.Id}`.");
                        return;
                    }

                    if (guildUser.VoiceChannel == null) { // the user whose id was given is not in a voice chat
                        await Context.Channel.SendMessageAsync($"User is not in a voice channel.");
                        return;
                    }

                    User serverUser = await server.GetUserAsync(user.Id);
                    await serverUser.AddMutePersistedAsync(guildUser.VoiceChannel.Id, null);

                    if (!guildUser.IsMuted) {
                        serverUser.IncrementVoiceStatusUpdated();
                        await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                    }
                }

                else { // this has time constraints
                    if (!await server.UserMayTempMute(socketGuildUser.Id, userIds)) {
                        return;
                    }

                    if (guildUser == null) { // the user whose id was given does not exist
                        await Context.Channel.SendMessageAsync($"Could not find user with ID `{user.Id}`.");
                        return;
                    }

                    if (guildUser.VoiceChannel == null) { // the user whose id was given is not in a voice chat
                        await Context.Channel.SendMessageAsync($"User is not in a voice channel.");
                        return;
                    }

                    bool parsedTimeSpan = CommandHelper.GetTimeSpan(arguments, out TimeSpan timeSpan, out string errors, MinimumMuteTimeSpan);

                    if (parsedTimeSpan) {
                        User serverUser = await server.GetUserAsync(user.Id);
                        await serverUser.AddMutePersistedAsync(guildUser.VoiceChannel.Id, DateTime.UtcNow + timeSpan);

                        if (!guildUser.IsMuted) {
                            serverUser.IncrementVoiceStatusUpdated();
                            await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                        }
                    }

                    else {
                        await Context.Channel.SendMessageAsync(errors);
                    }
                }
            }
        }
    }
}
