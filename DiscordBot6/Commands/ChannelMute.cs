using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Helpers;
using System;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("channelmute")]
    public class ChannelMute : ModuleBase<SocketCommandContext> {
        public static readonly TimeSpan MinimumMuteTimeSpan = TimeSpan.FromSeconds(30);

        [Command]
        [Summary("Mutes a user in a specific voice channel. With an optional time limit.")]
        public async Task Command(IUser user, params string[] arguments) {
            SocketGuildUser guildUser = Context.Guild.GetUser(user.Id);

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
                User serverUser = await Context.Guild.GetUserAsync(user.Id);
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
