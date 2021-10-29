using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("mute")]
    public sealed class Mute : ModuleBase<SocketCommandContext> {
        [Command]
        public async Task Command(IUser user) {
            await CommandImpl(user.Id);
        }

        [Command]
        public async Task Command(ulong userId) {
            await CommandImpl(userId);
        }

        public async Task CommandImpl(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();

                IEnumerable<ulong> userRoleIds = socketGuildUser.Roles.Select(role => role.Id);
                if (!await server.UserMayMute(socketGuildUser.Id, userRoleIds)) {
                    await Context.Channel.SendNoPermissionResponseAsync(socketGuildUser);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                serverUser.GlobalMutePersisted = true;
                await server.SetUserAsync(userId, serverUser);

                SocketGuildUser guildUser = Context.Guild.GetUser(userId);
                if (guildUser?.VoiceChannel != null && !guildUser.IsMuted) {
                    await guildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                }

                string messageSuffix = guildUser == null ? $"`{userId}`" : guildUser.Mention;
                await Context.Channel.SendGenericSuccessResponseAsync(userId, guildUser?.GetAvatarUrl(size: 64), $"Muted {messageSuffix}");

                if (server.HasLogChannel) {
                    await Context.Guild.GetTextChannel(server.LogChannelId)
                        .LogGenericSuccessAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), $"{socketGuildUser.Mention} muted {messageSuffix}");
                }
            }
        }
    }
}
