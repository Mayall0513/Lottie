using Discord.WebSocket;
using Lottie.Database;
using Lottie.Timing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lottie {
    public static class UserExtensions {
        public static async Task<User> L_GetUserAsync(this SocketGuildUser socketGuildUser) {
            Server server = await socketGuildUser.Guild.L_GetServerAsync();
            return await server.GetUserAsync(socketGuildUser.Id);
        }

        public static async Task L_AddRolesPersistedAsync(this SocketGuildUser socketGuildUser, IEnumerable<ulong> roleIds, DateTime? expiry) {
            User user = await socketGuildUser.L_GetUserAsync();
            await user.AddRolesPersistedAsync(roleIds, expiry);
        }

        public static async Task L_PrecacheRolePersistedAsync(this SocketGuildUser socketGuildUser, RolePersist rolePersist) {
            User user = await socketGuildUser.L_GetUserAsync();
            user.PrecacheRolePersisted(rolePersist);
        }

        public static async Task<bool> L_RemoveRolePersistedAsync(this SocketGuildUser socketGuildUser, ulong roleId) {
            User user = await socketGuildUser.L_GetUserAsync();
            return await user.RemoveRolePersistedAsync(roleId);
        }

        public static async Task<bool> L_RemoveRolesPersistedAsync(this SocketGuildUser socketGuildUser, IEnumerable<ulong> roleIds) {
            User user = await socketGuildUser.L_GetUserAsync();
            return await user.RemoveRolesPersistedAsync(roleIds);
        }

        public static async Task<RolePersist[]> L_GetRolesPersistedAsync(this SocketGuildUser socketGuildUser) {
            User user = await socketGuildUser.L_GetUserAsync();
            return user.GetRolesPersisted();
        }

        public static async Task<ulong[]> L_GetRolesPersistedIdsAsync(this SocketGuildUser socketGuildUser) {
            User user = await socketGuildUser.L_GetUserAsync();
            return user.GetRolesPersistedIds();
        }


        public static async Task L_AddMutePersistedAsync(this SocketGuildUser socketGuildUser, ulong channelId, DateTime? expiry) {
            User user = await socketGuildUser.L_GetUserAsync();
            await user.AddMutePersistedAsync(channelId, expiry);
        }

        public static async Task L_PrecacheMutePersisted(this SocketGuildUser socketGuildUser, MutePersist mutePersist) {
            User user = await socketGuildUser.L_GetUserAsync();
            user.PrecacheMutePersisted(mutePersist);
        }

        public static async Task<bool> L_RemoveMutePersistedAsync(this SocketGuildUser socketGuildUser, ulong channelId) {
            User user = await socketGuildUser.L_GetUserAsync();
            return await user.RemoveMutePersistedAsync(channelId);
        }

        public static async Task<MutePersist[]> L_GetMutesPersistedAsync(this SocketGuildUser socketGuildUser) {
            User user = await socketGuildUser.L_GetUserAsync();
            return user.GetMutesPersisted();
        }

        public static async Task<ulong[]> L_GetMutesPersistedIdsAsync(this SocketGuildUser socketGuildUser) {
            User user = await socketGuildUser.L_GetUserAsync();
            return user.GetMutesPersistedIds();
        }


        public static async Task L_AddActiveContingentRoleAsync(this SocketGuildUser socketGuildUser, ulong roleId, IEnumerable<ulong> contingentRoleIds) {
            User user = await socketGuildUser.L_GetUserAsync();
            await user.AddActiveContingentRoleAsync(roleId, contingentRoleIds);
        }

        public static async Task<bool> L_RemoveActiveContingentRoleAsync(this SocketGuildUser socketGuildUser, ulong roleId) {
            User user = await socketGuildUser.L_GetUserAsync();
            return await user.RemoveActiveContingentRoleAsync(roleId);
        }

        public static async Task<ulong[]> L_GetActiveContingentRoleIdsAsync(this SocketGuildUser socketGuildUser) {
            User user = await socketGuildUser.L_GetUserAsync();
            return await user.GetActiveContingentRoleIdsAsync();
        }

        public static async Task<ulong[]> L_GetContingentRolesRemovedAsync(this SocketGuildUser socketGuildUser) {
            User user = await socketGuildUser.L_GetUserAsync();
            return await user.GetContingentRolesRemovedAsync();
        }

        public static async Task<ulong[]> L_GetContingentRolesRemovedAsync(this SocketGuildUser socketGuildUser, ulong roleId) {
            User user = await socketGuildUser.L_GetUserAsync();
            return await user.GetContingentRolesRemovedAsync(roleId);
        }


        public static async Task L_AddVoiceStatusUpdateAsync(this SocketGuildUser socketGuildUser, int muteChange, int deafenChange) {
            User user = await socketGuildUser.L_GetUserAsync();
            user.AddVoiceStatusUpdate(muteChange, deafenChange);
        }

        public static async Task<bool> L_CheckVoiceStatusUpdateAsync(this SocketGuildUser socketGuildUser, VoiceStatusUpdate voiceStatusUpdate) {
            User user = await socketGuildUser.L_GetUserAsync();
            return user.CheckVoiceStatusUpdate(voiceStatusUpdate);
        }


        public static async Task L_AddMemberStatusUpdateAsync(this SocketGuildUser socketGuildUser, ICollection<ulong> rolesAdded, ICollection<ulong> rolesRemoved) {
            User user = await socketGuildUser.L_GetUserAsync();
            user.AddMemberStatusUpdate(rolesAdded, rolesRemoved);
        }

        public static async Task<bool> L_CheckMemberStatusUpdateAsync(this SocketGuildUser socketGuildUser, MemberStatusUpdate memberStatusUpdate) {
            User user = await socketGuildUser.L_GetUserAsync();
            return user.CheckMemberStatusUpdate(memberStatusUpdate);
        }
    }
}
