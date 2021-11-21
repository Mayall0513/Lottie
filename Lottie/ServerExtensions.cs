using Discord.WebSocket;
using Lottie.Constraints;
using Lottie.ContingentRoles;
using Lottie.Database;
using Lottie.PhraseRules;
using Lottie.Timing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lottie {
    public static class ServerExtensions {
        public static async Task<Server> L_GetServerAsync(this SocketGuild socketGuild) {
            return await Server.GetServerAsync(socketGuild.Id);
        }


        public static async Task<IEnumerable<PhraseRule>> L_GetPhraseRuleSetsAsync(this SocketGuild socketGuild) {
            Server server = await socketGuild.L_GetServerAsync();
            return await server.GetPhraseRuleSetsAsync();
        }

        public static async Task<IEnumerable<ContingentRole>> L_GetContingentRolesAsync(this SocketGuild socketGuild) {
            Server server = await socketGuild.L_GetServerAsync();
            return await server.GetContingentRolesAsync();
        }


        public static async Task<User> L_GetUserAsync(this SocketGuild socketGuild, ulong id) {
            Server server = await socketGuild.L_GetServerAsync();
            return await server.GetUserAsync(id);
        }

        public static async Task L_SetUserAsync(this SocketGuild socketGuild, User user) {
            Server server = await socketGuild.L_GetServerAsync();
            await server.SetUserAsync(user);
        }

        public static async Task<bool> L_UserMatchesConstraintsAsync(this SocketGuild socketGuild, ConstraintIntents intent, ulong? channelId = null, IEnumerable<ulong> roleIds = null, ulong? userId = null) {
            Server server = await socketGuild.L_GetServerAsync();
            return await server.UserMatchesConstraintsAsync(intent, channelId, roleIds, userId);
        }

        public static async Task<string> L_GetPresetMessageAsync(this SocketGuild socketGuild, PresetMessageTypes messageType, ulong messageId) {
            Server server = await socketGuild.L_GetServerAsync();
            return server.GetPresetMessage(messageType, messageId);
        }


        public static async Task<bool> L_IsCommandChannelAsync(this SocketGuild socketGuild, ulong channelId) {
            Server server = await socketGuild.L_GetServerAsync();
            return server.IsCommandChannel(channelId);
        }

        public static async Task<string> L_GetCommandPrefixAsync(this SocketGuild socketGuild) {
            Server server = await socketGuild.L_GetServerAsync();
            return server.GetCommandPrefix();
        }


        public static async Task L_CacheRolePersistAsync(this SocketGuild socketGuild, RolePersist rolePersist) {
            Server server = await socketGuild.L_GetServerAsync();
            server.CacheRolePersist(rolePersist);
        }

        public static async Task L_UncacheRolePersistAsync(this SocketGuild socketGuild, RolePersist rolePersist) {
            Server server = await socketGuild.L_GetServerAsync();
            server.UncacheRolePersist(rolePersist);
        }

        public static async Task<RolePersist[]> L_GetRoleCacheAsync(this SocketGuild socketGuild, ulong roleId) {
            Server server = await socketGuild.L_GetServerAsync();
            return server.GetRoleCache(roleId);
        }


        public static async Task L_CacheMutePersistAsync(this SocketGuild socketGuild, MutePersist mutePersist) {
            Server server = await socketGuild.L_GetServerAsync();
            server.CacheMutePersist(mutePersist);
        }

        public static async Task L_UncacheMutePersistAsync(this SocketGuild socketGuild, MutePersist mutePersist) {
            Server server = await socketGuild.L_GetServerAsync();
            server.UncacheMutePersist(mutePersist);
        }

        public static async Task<MutePersist[]> L_GetMuteCacheAsync(this SocketGuild socketGuild, ulong channelId) {
            Server server = await socketGuild.L_GetServerAsync();
            return server.GetMuteCache(channelId);
        }
    }
}
