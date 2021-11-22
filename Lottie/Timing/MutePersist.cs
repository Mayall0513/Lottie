using Discord.WebSocket;

namespace Lottie.Timing {
    public sealed class MutePersist : TimedObject {
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
        
        public override async void OnExpiry() {
            SocketGuild socketGuild = Lottie.Client.GetGuild(ServerId);
            if (socketGuild == null) {
                return;
            }

            SocketChannel socketChannel = socketGuild.GetChannel(ChannelId);
            if (socketChannel is SocketVoiceChannel socketVoiceChannel) {
                SocketUser socketUser = socketGuild.GetUser(UserId);

                
                if (socketUser is SocketGuildUser socketGuildUser && socketGuildUser.VoiceChannel?.Id == ChannelId && socketGuildUser.IsMuted) {
                    User user = await socketGuildUser.L_GetUserAsync();
                    await user.RemoveMutePersistedAsync(ChannelId);

                    user.AddVoiceStatusUpdate(-1, 0);
                    await socketGuildUser.ModifyAsync(userProperties => { userProperties.Mute = false; });
                }
            }
        }
    }
}
