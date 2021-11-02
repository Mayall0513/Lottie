﻿using Discord.WebSocket;
using DiscordBot6.Helpers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot6.Timing {
    public sealed class RolePersist : TimedObject {
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public ulong RoleId { get; set; }

        public override async void OnExpiry(object state) {
            SocketGuild socketGuild = DiscordBot6.Client.GetGuild(ServerId);
            if (socketGuild == null) {
                return;
            }

            Server server = await Server.GetServerAsync(ServerId);
            User user = await server?.GetUserAsync(UserId);
            await user.RemoveRolePersistedAsync(RoleId);

            SocketUser socketUser = socketGuild.GetUser(UserId);
            if (socketUser is SocketGuildUser socketGuildUser) {
                SocketRole socketRole = socketGuildUser.Roles.FirstOrDefault(role => RoleId == role.Id && socketGuild.MayEditRole(role));

                if (socketRole != null) {
                    user.IncrementRolesUpdated();
                    await socketGuildUser.RemoveRoleAsync(socketRole);
                }
            }
        }
    }
}