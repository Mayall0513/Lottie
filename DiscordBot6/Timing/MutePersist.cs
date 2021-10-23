using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot6.Timing {
    public sealed class MutePersist : TimedObject {
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
        
        public override async void OnExpiry(object state) {
            Server server = await Server.GetServerAsync(ServerId);
            User user = await server?.GetUserAsync(UserId);

            if (user != null) {
                await user.RemoveMutePersistedAsync(ChannelId);
            }

            SocketChannel socketChannel = DiscordBot6.Client.GetChannel(ChannelId);
            if (socketChannel is SocketVoiceChannel socketVoiceChannel) {
                SocketUser socketUser = DiscordBot6.Client.GetGuild(ServerId)?.GetUser(UserId);

                if (socketUser is SocketGuildUser socketGuildUser && socketGuildUser.VoiceChannel?.Id == ChannelId && socketGuildUser.IsMuted) {
                    server.TryAddVoiceStatusUpdated(socketUser.Id);
                    await socketGuildUser.ModifyAsync(userProperties => { userProperties.Mute = false; });
                }
            }
        }
    }
}
