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
        public async Task Command(IUser user, params string[] arguments) {
            if (Context.Message.Author is SocketGuildUser author) {
                SocketGuild guild = Context.Guild;
                SocketGuildUser guildUser = guild.GetUser(user.Id);

                if (guildUser != null && guildUser.VoiceChannel != null) {
                    int days = 0;
                    int hours = 0; 
                    int minutes = 0;


                    foreach (string argument in arguments) {
                        if (int.TryParse(argument.Substring(0, argument.Length - 1), out int numArgument)) {
                            if (numArgument <= 0) {
                                // error
                                continue;
                            }

                            char finalCharacter = argument[argument.Length - 1];

                            switch (finalCharacter) {
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
                    if (difference == TimeSpan.Zero) {
                        // error
                        return;
                    }

                    Server server = await Server.GetServerAsync(guild.Id);
                    User serverUser = await server.GetUserAsync(user.Id);
                    await serverUser.AddMutePersistedAsync(guildUser.VoiceChannel.Id, DateTime.UtcNow + difference);

                    if (!guildUser.IsMuted) {
                        server.TryAddVoiceStatusUpdated(guildUser.Id);
                        await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                    }
                }
            }
        }
    }
}
