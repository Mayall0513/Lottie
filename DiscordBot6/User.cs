using DiscordBot6.Database;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6 {
    public sealed class User {
        public ulong Id { get; }

        public Server Parent { get; set; }

        public bool GlobalMutePersisted { get; set; }
        public bool GlobalDeafenPersisted { get; set; }

        private HashSet<ulong> mutesPersisted; // stores channel ids
        private HashSet<ulong> rolesPersisted; // stores role ids
        private ConcurrentDictionary<ulong, HashSet<ulong>> contingentRolesRemoved;

        public User(ulong id, bool mutePersisted, bool deafenPersisted) {
            Id = id;
            GlobalMutePersisted = mutePersisted;
            GlobalDeafenPersisted = deafenPersisted;
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


        public async Task AddMutePersistedAsync(ulong channelId) {
            if (mutesPersisted == null) {
                await CacheMutesPersistedAsync();
            }

            if (mutesPersisted.Add(channelId)) {
                // add to database
            }
        }

        public async Task RemoveMutePersistedAsync(ulong channelId) {
            if (mutesPersisted == null) {
                await CacheMutesPersistedAsync();
            }

            if (mutesPersisted.Remove(channelId)) {
                // remove from database
            }
        }

        public async Task<IEnumerable<ulong>> GetMutesPersistedAsync() {
            if (mutesPersisted == null) {
                await CacheMutesPersistedAsync();
            }

            return mutesPersisted;
        }


        public async Task AddContingentRoleRemovedAsync(ulong roleId, ulong contingentRoleId) {
            if (contingentRolesRemoved == null) {
                await CacheContingentRolesRemoved();
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
                await CacheContingentRolesRemoved();
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
                await CacheContingentRolesRemoved();
            }

            if (contingentRolesRemoved.TryRemove(roleId, out _)) {
                await Repository.RemoveActiveContingentRolesAsync(Parent.Id, Id, roleId);
            }
        }

        public async Task<ConcurrentDictionary<ulong, HashSet<ulong>>> GetContingentRolesRemoved() {
            if (contingentRolesRemoved == null) {
                await CacheContingentRolesRemoved();
            }

            return contingentRolesRemoved;
        }


        private async Task CacheRolesPersistedAsync() {
            rolesPersisted = new HashSet<ulong>(await Repository.GetRolesPersistsAsync(Parent.Id, Id));
        }

        private async Task CacheMutesPersistedAsync() {
            mutesPersisted = new HashSet<ulong>(await Repository.GetMutePersistsAsync(Parent.Id, Id));
        }

        private async Task CacheContingentRolesRemoved() {
            contingentRolesRemoved = new ConcurrentDictionary<ulong, HashSet<ulong>>(await Repository.GetActiveContingentRolesAsync(Parent.Id, Id));
        }
    }
}
