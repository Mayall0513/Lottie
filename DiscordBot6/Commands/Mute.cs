using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("mute")]
    public sealed class Mute : ModuleBase<SocketCommandContext> {
        [Command]
        public async Task Command(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();
                IEnumerable<ulong> userIds = socketGuildUser.Roles.Select(x => x.Id);

                if (!await server.UserMayGiveRoles(socketGuildUser.Id, userIds)) {
                    return;
                }

                
                User serverUser = await server.GetUserAsync(userId);
                serverUser.GlobalMutePersisted = true;
                await server.SetUserAsync(userId, serverUser);

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser?.VoiceChannel != null && !guildUser.IsMuted) {
                    serverUser.IncrementVoiceStatusUpdated();
                    await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                }
            }
        }
    }
}
