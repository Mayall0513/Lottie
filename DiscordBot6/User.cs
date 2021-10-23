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
        private HashSet<ulong> rolesPersisted; // stores role ids
        private ConcurrentDictionary<ulong, HashSet<ulong>> contingentRolesRemoved;


        private int voiceStatusUpdated = 0;
        private int rolesUpdated = 0;


        public User(ulong id, bool globalMutePersisted, bool globalDeafenPersist) {
            Id = id;
            GlobalMutePersisted = globalMutePersisted;
            GlobalDeafenPersisted = globalDeafenPersist;
        }



        public async Task AddRolePersistedAsync(ulong roleId) {
            if (rolesPersisted == null) {
                await CacheRolesPersistedAsync();
            }

            if (rolesPersisted.Add(roleId)) {
                await Repository.AddRolePersistedAsync(Parent.Id, Id, roleId);
            }
        }

        public async Task AddRolesPersistedAsync(IEnumerable<ulong> roleIds) {
            if (rolesPersisted == null) {
                await CacheRolesPersistedAsync();
            }

            ulong[] newRoles = roleIds.Except(rolesPersisted).ToArray();
            rolesPersisted.UnionWith(newRoles);

            if (newRoles.Length > 0) {
                await Repository.AddRolesPersistedAsync(Parent.Id, Id, newRoles);
            }
        }

        public async Task RemoveRolePersistedAsync(ulong roleId) {
            if (rolesPersisted == null) {
                await CacheRolesPersistedAsync();
            }

            if (rolesPersisted.Remove(roleId)) {
                await Repository.RemoveRolePersistedAsync(Parent.Id, Id, roleId);
            }
        }

        public async Task RemoveRolesPersistedAsync(IEnumerable<ulong> roleIds) {
            if (rolesPersisted == null) {
                await CacheRolesPersistedAsync();
            }

            ulong[] rolesToRemove = roleIds.Intersect(rolesPersisted).ToArray();
            rolesPersisted.ExceptWith(rolesToRemove);

            if (rolesToRemove.Length > 0) {
                await Repository.RemoveRolesPersistedAsync(Parent.Id, Id, rolesToRemove);
            }
        }

        public async Task<IEnumerable<ulong>> GetRolesPersistedAsync() {
            if (rolesPersisted == null) {
                await CacheRolesPersistedAsync();
            }

            return rolesPersisted;
        }


        public async Task AddMutePersistedAsync(ulong channelId, DateTime? expiry) {
            if (mutesPersisted == null) {
                await CacheMutesPersistedAsync();
            }
        
            if (mutesPersisted.ContainsKey(channelId)) {
                return;
            }

            MutePersist mutePersist = new MutePersist() { ServerId = Parent.Id, UserId = Id, ChannelId = channelId, Expiry = expiry };
            mutesPersisted.TryAdd(channelId, mutePersist);

            await Repository.AddMutePersistedAsync(Parent.Id, Id, channelId, expiry);
        }

        public async Task RemoveMutePersistedAsync(ulong channelId) {
            if (mutesPersisted == null) {
                await CacheMutesPersistedAsync();
            }

            if (mutesPersisted.TryRemove(channelId, out _)) {
                await Repository.RemoveMutePersistedAsync(Parent.Id, Id, channelId);
            }
        }

        public async Task<IEnumerable<ulong>> GetMutesPersistedAsync() {
            if (mutesPersisted == null) {
                await CacheMutesPersistedAsync();
            }

            return mutesPersisted.Keys;
        }


        public async Task AddContingentRoleRemovedAsync(ulong roleId, ulong contingentRoleId) {
            if (contingentRolesRemoved == null) {
                await CacheContingentRolesRemovedAsync();
            }

            if (!contingentRolesRemoved.ContainsKey(roleId)) {
                contingentRolesRemoved.TryAdd(roleId, new HashSet<ulong>());
            }

            if (contingentRolesRemoved[roleId].Add(contingentRoleId)) {
                await Repository.AddActiveContingentRoleAsync(Parent.Id, Id, roleId, contingentRoleId);
            }
        }

        public async Task AddContingentRolesRemovedAsync(ulong roleId, IEnumerable<ulong> contingentRoleIds) {
            if (contingentRolesRemoved == null) {
                await CacheContingentRolesRemovedAsync();
            }

            if (!contingentRolesRemoved.ContainsKey(roleId)) {
                contingentRolesRemoved.TryAdd(roleId, new HashSet<ulong>());
            }

            ulong[] newRoles = contingentRoleIds.Except(contingentRolesRemoved[roleId]).ToArray();
            contingentRolesRemoved[roleId].UnionWith(newRoles);

            if (newRoles.Length > 0) {
                await Repository.AddActiveContingentRolesAsync(Parent.Id, Id, roleId, newRoles);
            }
        }

        public async Task RemoveContingentRoleRemovedAsync(ulong roleId) {
            if (contingentRolesRemoved == null) {
                await CacheContingentRolesRemovedAsync();
            }

            if (contingentRolesRemoved.TryRemove(roleId, out _)) {
                await Repository.RemoveActiveContingentRolesAsync(Parent.Id, Id, roleId);
            }
        }

        public async Task<ConcurrentDictionary<ulong, HashSet<ulong>>> GetContingentRolesRemovedAsync() {
            if (contingentRolesRemoved == null) {
                await CacheContingentRolesRemovedAsync();
            }

            return contingentRolesRemoved;
        }


        private async Task CacheRolesPersistedAsync() {
            rolesPersisted = new HashSet<ulong>(await Repository.GetRolesPersistsAsync(Parent.Id, Id));
        }

        private async Task CacheMutesPersistedAsync() {
            IEnumerable<MutePersist> mutesPersisted = await Repository.GetMutePersistsAsync(Parent.Id, Id);
            this.mutesPersisted = new ConcurrentDictionary<ulong, MutePersist>();

            foreach (MutePersist mutePersisted in mutesPersisted) {
                this.mutesPersisted.TryAdd(mutePersisted.ChannelId, mutePersisted);
            }
        }

        private async Task CacheContingentRolesRemovedAsync() {
            contingentRolesRemoved = new ConcurrentDictionary<ulong, HashSet<ulong>>(await Repository.GetActiveContingentRolesAsync(Parent.Id, Id));
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
