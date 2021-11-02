using DiscordBot6.Database;
using DiscordBot6.Timing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot6 {
    public sealed class User {
        public ulong Id { get; }

        public bool GlobalMutePersisted { get; set; }
        public bool GlobalDeafenPersisted { get; set; }

        public Server Parent { get; set; }

        private ConcurrentDictionary<ulong, MutePersist> mutesPersisted; 
        private ConcurrentDictionary<ulong, RolePersist> rolesPersisted; 
        private ConcurrentDictionary<ulong, HashSet<ulong>> activeContingentRoles;


        private int voiceStatusUpdated = 0;
        private int rolesUpdated = 0;


        public User(ulong id, bool globalMutePersisted, bool globalDeafenPersist) {
            Id = id;
            GlobalMutePersisted = globalMutePersisted;
            GlobalDeafenPersisted = globalDeafenPersist;
        }


        public async Task AddRolePersistedAsync(ulong roleId, DateTime? expiry) {
            if (rolesPersisted == null) {
                rolesPersisted = new ConcurrentDictionary<ulong, RolePersist>();
            }

            if (rolesPersisted.ContainsKey(roleId)) {
                rolesPersisted[roleId].Expiry = expiry;
            }

            else {
                RolePersist rolePersist = new RolePersist() { ServerId = Parent.Id, UserId = Id, RoleId = roleId, Expiry = expiry };
                rolesPersisted.TryAdd(roleId, rolePersist);
            }

            await Repository.AddOrUpdateRolePersistedAsync(Parent.Id, Id, roleId, expiry);
        }

        public async Task AddRolesPersistedAsync(IEnumerable<ulong> roleIds, DateTime? expiry) {
            if (rolesPersisted == null) {
                rolesPersisted = new ConcurrentDictionary<ulong, RolePersist>();
            }

            foreach(ulong roleId in roleIds) {
                if (rolesPersisted.ContainsKey(roleId)) {
                    rolesPersisted[roleId].Expiry = expiry;
                }

                else {
                    RolePersist rolePersist = new RolePersist() { ServerId = Parent.Id, UserId = Id, RoleId = roleId, Expiry = expiry };
                    rolesPersisted.TryAdd(roleId, rolePersist);
                }
            }

            await Repository.AddOrUpdateRolesPersistedAsync(Parent.Id, Id, roleIds, expiry);
        }

        public bool PrecacheRolePersisted(RolePersist rolePersist) {
            if (rolesPersisted == null) {
                rolesPersisted = new ConcurrentDictionary<ulong, RolePersist>();
            }

            return rolesPersisted.TryAdd(rolePersist.RoleId, rolePersist);
        }

        public async Task RemoveRolePersistedAsync(ulong roleId) {
            if (rolesPersisted == null) {
                rolesPersisted = new ConcurrentDictionary<ulong, RolePersist>();
            }

            else if (rolesPersisted.TryRemove(roleId, out _)) {
                await Repository.RemoveRolePersistedAsync(Parent.Id, Id, roleId);
            }
        }

        public async Task RemoveRolesPersistedAsync(IEnumerable<ulong> roleIds) {
            if (rolesPersisted == null) {
                rolesPersisted = new ConcurrentDictionary<ulong, RolePersist>();
            }

            ulong[] rolesToRemove = roleIds.Intersect(rolesPersisted.Keys).ToArray();

            if (rolesToRemove.Length > 0) {
                foreach (ulong roleToRemove in rolesToRemove) {
                    rolesPersisted.TryRemove(roleToRemove, out _);
                }

                await Repository.RemoveRolesPersistedAsync(Parent.Id, Id, rolesToRemove);
            }
        }

        public bool IsRolePersisted(ulong roleId) {
            if (rolesPersisted == null) {
                return false;
            }

            return rolesPersisted.ContainsKey(roleId);
        }

        public IEnumerable<RolePersist> GetRolesPersisted() {
            if (rolesPersisted == null) {
                rolesPersisted = new ConcurrentDictionary<ulong, RolePersist>();
            }

            return rolesPersisted.Values;
        }

        public IEnumerable<ulong> GetRolesPersistedIds() {
            if (rolesPersisted == null) {
                rolesPersisted = new ConcurrentDictionary<ulong, RolePersist>();
            }

            return rolesPersisted.Keys;
        }


        public async Task AddMutePersistedAsync(ulong channelId, DateTime? expiry) {
            if (mutesPersisted == null) {
                mutesPersisted = new ConcurrentDictionary<ulong, MutePersist>();
            }
        
            else if (mutesPersisted.ContainsKey(channelId)) {
                mutesPersisted[channelId].Expiry = expiry;
            }

            else {
                MutePersist mutePersist = new MutePersist() { ServerId = Parent.Id, UserId = Id, ChannelId = channelId, Expiry = expiry };
                mutesPersisted.TryAdd(channelId, mutePersist);
            }

            await Repository.AddOrUpdateMutePersistedAsync(Parent.Id, Id, channelId, expiry);
        }

        public bool PrecacheMutePersisted(MutePersist mutePersist) { // this may render CacheMutesPersistedAsync entirely unneeded?
            if (mutesPersisted == null) {
                mutesPersisted = new ConcurrentDictionary<ulong, MutePersist>();
            }

            return mutesPersisted.TryAdd(mutePersist.ChannelId, mutePersist);
        }

        public async Task RemoveMutePersistedAsync(ulong channelId) {
            if (mutesPersisted == null) {
                mutesPersisted = new ConcurrentDictionary<ulong, MutePersist>();
            }

            else if (mutesPersisted.TryRemove(channelId, out _)) {
                await Repository.RemoveMutePersistedAsync(Parent.Id, Id, channelId);
            }
        }

        public bool IsMutePersisted(ulong channelId) {
            if (mutesPersisted == null) {
                return false;
            }

            return mutesPersisted.ContainsKey(channelId);
        }

        public IEnumerable<MutePersist> GetMutesPersisted() {
            if (mutesPersisted == null) {
                mutesPersisted = new ConcurrentDictionary<ulong, MutePersist>();
            }

            return mutesPersisted.Values;
        }

        public IEnumerable<ulong> GetMutesPersistedIds() {
            if (mutesPersisted == null) {
                mutesPersisted = new ConcurrentDictionary<ulong, MutePersist>();
            }

            return mutesPersisted.Keys;
        }


        public async Task AddActiveContingentRoleAsync(ulong roleId, ulong contingentRoleId) {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            if (!activeContingentRoles.ContainsKey(roleId)) {
                activeContingentRoles.TryAdd(roleId, new HashSet<ulong>());
            }

            if (activeContingentRoles[roleId].Add(contingentRoleId)) {
                await Repository.AddActiveContingentRoleAsync(Parent.Id, Id, roleId, contingentRoleId);
            }
        }

        public async Task AddActiveContingentRoleAsync(ulong roleId, IEnumerable<ulong> contingentRoleIds) {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            if (!activeContingentRoles.ContainsKey(roleId)) {
                activeContingentRoles.TryAdd(roleId, new HashSet<ulong>());
            }

            ulong[] newRoles = contingentRoleIds.Except(activeContingentRoles[roleId]).ToArray();
            activeContingentRoles[roleId].UnionWith(newRoles);

            if (newRoles.Length > 0) {
                await Repository.AddActiveContingentRolesAsync(Parent.Id, Id, roleId, newRoles);
            }
        }

        public async Task RemoveActiveContingentRoleAsync(ulong roleId) {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            if (activeContingentRoles.TryRemove(roleId, out _)) {
                await Repository.RemoveActiveContingentRolesAsync(Parent.Id, Id, roleId);
            }
        }

        public async Task<IEnumerable<ulong>> GetActiveContingentRoleIds() {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            return activeContingentRoles.Keys;
        }

        public async Task<IEnumerable<ulong>> GetContingentRolesRemoved() {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            return activeContingentRoles.Values.SelectMany(roles => roles).Distinct();
        }

        public async Task<IEnumerable<ulong>> GetContingentRolesRemoved(ulong roleId) {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            if (!activeContingentRoles.ContainsKey(roleId)) {
                return null;
            }

            return activeContingentRoles[roleId];
        }


        private async Task CacheContingentRolesRemovedAsync() {
            activeContingentRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>(await Repository.GetActiveContingentRolesAsync(Parent.Id, Id));
        }


        public void IncrementVoiceStatusUpdated() {
            Interlocked.Increment(ref voiceStatusUpdated);
        }

        public bool DecrementVoiceStatusUpdated() {
            if (voiceStatusUpdated > 0) {
                Interlocked.Decrement(ref voiceStatusUpdated);
                return true;
            }

            return false;
        }


        public void IncrementRolesUpdated() {
            Interlocked.Increment(ref rolesUpdated);
        }

        public bool DecrementRolesUpdated() {
            if (rolesUpdated > 0) {
                Interlocked.Decrement(ref rolesUpdated);
                return true;
            }

            return false;
        }
    }
}
