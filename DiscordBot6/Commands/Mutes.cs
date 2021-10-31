﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("mute")]
    public sealed class Mutes : ModuleBase<SocketCommandContext> {
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
                    await Context.Channel.SendNoPermissionAsync(socketGuildUser);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);
                serverUser.GlobalMutePersisted = true;
                await server.SetUserAsync(userId, serverUser);

                SocketGuildUser socketUser = Context.Guild.GetUser(userId);
                if (socketUser?.VoiceChannel != null && !socketUser.IsMuted) { 
                    await socketUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                }

                string mutedIdentifier = CommandHelper.GetUserIdentifier(userId, socketUser);
                await Context.Channel.SendGenericSuccessAsync(userId, socketUser?.GetAvatarUrl(size: 64), $"Muted {mutedIdentifier}");

                if (server.HasLogChannel) {
                    string muterIdentifier = CommandHelper.GetUserIdentifier(socketGuildUser.Id, socketGuildUser);

                    await Context.Guild.GetTextChannel(server.LogChannelId)
                        .LogGenericSuccessAsync(socketGuildUser.Id, socketGuildUser.GetAvatarUrl(size: 64), $"{muterIdentifier} muted {mutedIdentifier}");
                }
            }
        }
    }
}
