using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("channelmute")]
    public class ChannelMute : ModuleBase<SocketCommandContext> {
        [Command]
        [Summary("Mutes a user in a specific voice channel. With an optional time limit.")]
        public async Task Command(IUser user, string[] arguments) {
            SocketGuild guild = Context.Guild;
            SocketGuildUser guildUser = guild.GetUser(user.Id);

            if (guildUser == null) { // the user whose id was given does not exist
                // error
                return;
            }

            if (guildUser.VoiceChannel == null) { // the user whose id was given is not in a vc
                // error
                return;
            }

            int days = 0;
            int hours = 0; 
            int minutes = 0;

            foreach (string argument in arguments) {
                if (int.TryParse(argument[0..^1], out int numArgument)) {
                    if (numArgument <= 0) { // a negative time span was given
                        // error
                        continue;
                    }

                    switch (argument[^1]) {
                        case 'w':
                            days += numArgument * 7;
                            break;

                        case 'd':
                            days += numArgument;
                            break;

                        case 'h':
                            hours += numArgument;
                            break;

                        case 'm':
                            minutes += numArgument;
                            break;
                    }
                }
            }

            TimeSpan difference = new TimeSpan(days, hours, minutes, 0);
            if (difference == TimeSpan.Zero) { // no time was given OR all time that was given was 0
                // error
                return;
            }

            Server server = await guild.GetServerAsync();
            User serverUser = await server.GetUserAsync(user.Id);
            await serverUser.AddMutePersistedAsync(guildUser.VoiceChannel.Id, DateTime.UtcNow + difference);

            if (!guildUser.IsMuted) {
                server.TryAddVoiceStatusUpdated(guildUser.Id);
                await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
            }
        }
    }
}
