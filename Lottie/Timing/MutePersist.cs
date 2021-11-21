using Discord.WebSocket;

namespace Lottie.Timing {
    public sealed class MutePersist : TimedObject {
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
        
        public override async void OnExpiry() {
            SocketGuild socketGuild = DiscordBot6.Client.GetGuild(ServerId);
            if (socketGuild == null) {
                return;
            }

            Server server = await Server.GetServerAsync(ServerId);
            User user = await server?.GetUserAsync(UserId);

            await user.RemoveMutePersistedAsync(ChannelId);

            SocketChannel socketChannel = socketGuild.GetChannel(ChannelId);
            if (socketChannel is SocketVoiceChannel socketVoiceChannel) {
                SocketUser socketUser = socketGuild.GetUser(UserId);

                if (socketUser is SocketGuildUser socketGuildUser && socketGuildUser.VoiceChannel?.Id == ChannelId && socketGuildUser.IsMuted) {
                    user.AddVoiceStatusUpdate(-1, 0);
                    await socketGuildUser.ModifyAsync(userProperties => { userProperties.Mute = false; });
                }
            }
        }
    }
}
