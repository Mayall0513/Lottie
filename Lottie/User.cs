using Lottie.Database;
using Lottie.Timing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lottie {
    public sealed class VoiceStatusUpdates {
        public List<int> MuteChanges { get; set; } = new List<int>();
        public List<int> DeafenChanges { get; set; } = new List<int>();

        public bool ContainsUpdate(VoiceStatusUpdate voiceStatusUpdate) {
            if (voiceStatusUpdate.MuteChange == 0 && voiceStatusUpdate.DeafenChange == 0) {
                return false;
            }

            if (voiceStatusUpdate.MuteChange != 0) {
                int muteChangeIndex = MuteChanges.LastIndexOf(voiceStatusUpdate.MuteChange);
                if (muteChangeIndex == -1) {
                    return false;
                }

                MuteChanges.RemoveAt(muteChangeIndex);
            }

            if (voiceStatusUpdate.DeafenChange != 0) {
                int deafenChangeIndex = MuteChanges.LastIndexOf(voiceStatusUpdate.DeafenChange);
                if (deafenChangeIndex == -1) {
                    return false;
                }

                DeafenChanges.RemoveAt(deafenChangeIndex);
            }

            return true;
        } 
    }

    public struct VoiceStatusUpdate {
        public int MuteChange { get; set; }
        public int DeafenChange { get; set; }

        public VoiceStatusUpdate(int muteChange, int deafenChange) {
            MuteChange = muteChange;
            DeafenChange = deafenChange;
        }
    }

    public sealed class MemberStatusUpdates {
        public List<ulong> RolesAdded { get; set; } = new List<ulong>();
        public List<ulong> RolesRemoved { get; set; } = new List<ulong>();

        public bool ContainsUpdate(MemberStatusUpdate memberStatusUpdate) {
            if (ContainsRoleUpdate(RolesAdded, memberStatusUpdate.RolesAdded, out HashSet<int> rolesAddIndices) && ContainsRoleUpdate(RolesRemoved, memberStatusUpdate.RolesRemoved, out HashSet<int> rolesRemovedIndices)) {
                foreach (int roleAddIndex in rolesAddIndices) {
                    RolesAdded.RemoveAt(roleAddIndex);
                }

                foreach (int roleRemoveIndex in rolesRemovedIndices) {
                    RolesRemoved.RemoveAt(roleRemoveIndex);
                }

                return true;
            }

            return false;
        }

        private bool ContainsRoleUpdate(List<ulong> roleList, ICollection<ulong> roleUpdates, out HashSet<int> indices) {
            indices = new HashSet<int>(roleUpdates.Count);

            if (roleList.Count == 0 && roleUpdates.Count == 0) {
                return true;
            }

            if (roleUpdates != null && roleUpdates.Count > 0) {
                foreach (ulong roleUpdate in roleUpdates) {
                    int searchIndex = roleList.Count - 1;

                    do {
                        searchIndex = roleList.LastIndexOf(roleUpdate, searchIndex);
 
                    }

                    while (searchIndex > 0 || indices.Contains(searchIndex));
                    if (searchIndex == -1) {
                        return false;
                    }

                    indices.Add(searchIndex);
                }
            }

            return true;
        }
    }

    public struct MemberStatusUpdate {
        public ICollection<ulong> RolesAdded { get; set; }
        public ICollection<ulong> RolesRemoved { get; set; }

        public MemberStatusUpdate(ICollection<ulong> rolesAdded, ICollection<ulong> rolesRemoved) {
            RolesAdded = rolesAdded;
            RolesRemoved = rolesRemoved;
        }
    }

    public sealed class User {
        public ulong Id { get; }

        public bool GlobalMutePersisted { get; set; }
        public bool GlobalDeafenPersisted { get; set; }

        public Server Parent { get; set; }

        public readonly ConcurrentDictionary<ulong, MutePersist> mutesPersisted = new ConcurrentDictionary<ulong, MutePersist>();
        public readonly ConcurrentDictionary<ulong, RolePersist> rolesPersisted = new ConcurrentDictionary<ulong, RolePersist>();
        public ConcurrentDictionary<ulong, HashSet<ulong>> activeContingentRoles;

        public readonly VoiceStatusUpdates voiceStatusUpdates = new VoiceStatusUpdates();
        public readonly MemberStatusUpdates memberStatusUpdates = new MemberStatusUpdates();


        public User(ulong id, bool globalMutePersisted, bool globalDeafenPersist) {
            Id = id;
            GlobalMutePersisted = globalMutePersisted;
            GlobalDeafenPersisted = globalDeafenPersist;
        }

        public async Task AddRolesPersistedAsync(IEnumerable<ulong> roleIds, DateTime? expiry) {
            foreach (ulong roleId in roleIds) {
                rolesPersisted.AddOrUpdate(roleId,
                    (roleId) => {
                        RolePersist rolePersist = new RolePersist() { ServerId = Parent.Id, UserId = Id, RoleId = roleId, Expiry = expiry };
                        Parent.CacheRolePersist(rolePersist);

                        return rolePersist;

                    },

                    (roleId, rolePersist) => {
                        rolePersist.Expiry = expiry;
                        return rolePersist;
                    }
                );
            }

            await Repository.AddOrUpdateRolesPersistedAsync(Parent.Id, Id, roleIds, expiry);
        }

        public void PrecacheRolePersisted(RolePersist rolePersist) {
            if (rolesPersisted.TryAdd(rolePersist.RoleId, rolePersist)) {
                Parent.CacheRolePersist(rolePersist);
            }
        }

        public async Task<bool> RemoveRolePersistedAsync(ulong roleId) {
            if (rolesPersisted.TryRemove(roleId, out RolePersist rolePersist)) {
                await Repository.RemoveRolePersistedAsync(Parent.Id, Id, roleId);
                Parent.UncacheRolePersist(rolePersist);

                rolePersist.Dispose();
                return true;
            }

            return false;
        }

        public async Task<bool> RemoveRolesPersistedAsync(IEnumerable<ulong> roleIds) {
            bool any = false;
            foreach (ulong roleId in roleIds) {
                if (rolesPersisted.TryRemove(roleId, out RolePersist rolePersist)) {
                    Parent.UncacheRolePersist(rolePersist);
                    any = true;
                }
            }

            await Repository.RemoveRolesPersistedAsync(Parent.Id, Id, roleIds);
            return any;
        }

        public RolePersist[] GetRolesPersisted() {
            return rolesPersisted.Values.ToArray();
        }

        public ulong[] GetRolesPersistedIds() {
            return rolesPersisted.Keys.ToArray();
        }


        public async Task AddMutePersistedAsync(ulong channelId, DateTime? expiry) {
            mutesPersisted.AddOrUpdate(channelId, 
                (channelId) => {
                    MutePersist mutePersist = new MutePersist() { ServerId = Parent.Id, UserId = Id, ChannelId = channelId, Expiry = expiry };
                    Parent.CacheMutePersist(mutePersist);

                    return mutePersist;
                },

                (channelId, mutePersist) => {
                    mutePersist.Expiry = expiry;
                    return mutePersist;
                }
            );

            await Repository.AddOrUpdateMutePersistedAsync(Parent.Id, Id, channelId, expiry);
        }

        public void PrecacheMutePersisted(MutePersist mutePersist) {
            if (mutesPersisted.TryAdd(mutePersist.ChannelId, mutePersist)) {
                Parent.CacheMutePersist(mutePersist);
            }
        }

        public async Task<bool> RemoveMutePersistedAsync(ulong channelId) {
            if (mutesPersisted.TryRemove(channelId, out MutePersist mutePersist)) {
                await Repository.RemoveMutePersistedAsync(Parent.Id, Id, channelId);
                Parent.UncacheMutePersist(mutePersist);

                mutePersist.Dispose();
                return true;
            }

            return false;
        }

        public MutePersist[] GetMutesPersisted() {
            return mutesPersisted.Values.ToArray();
        }

        public ulong[] GetMutesPersistedIds() {
            return mutesPersisted.Keys.ToArray();
        }


        public async Task AddActiveContingentRoleAsync(ulong roleId, IEnumerable<ulong> contingentRoleIds) {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            HashSet<ulong> contingentRoles = activeContingentRoles.GetOrAdd(roleId, new HashSet<ulong>());
            IEnumerable<ulong> newRoles;

            lock (contingentRoles) {
                newRoles = contingentRoleIds.Except(contingentRoles);
                contingentRoles.UnionWith(newRoles);
            }

            if (!newRoles.Any()) {
                await Repository.AddActiveContingentRolesAsync(Parent.Id, Id, roleId, newRoles);
            }
        }

        public async Task<bool> RemoveActiveContingentRoleAsync(ulong roleId) {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            if (activeContingentRoles.TryRemove(roleId, out _)) {
                await Repository.RemoveActiveContingentRolesAsync(Parent.Id, Id, roleId);
                return true;
            }

            return false;
        }

        public async Task<ulong[]> GetActiveContingentRoleIdsAsync() {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            return activeContingentRoles.Keys.ToArray();
        }

        public async Task<ulong[]> GetContingentRolesRemovedAsync() {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            return activeContingentRoles.Values.SelectMany(roles => roles).Distinct().ToArray();
        }

        public async Task<ulong[]> GetContingentRolesRemovedAsync(ulong roleId) {
            if (activeContingentRoles == null) {
                await CacheContingentRolesRemovedAsync();
            }

            if (activeContingentRoles.TryGetValue(roleId, out HashSet<ulong> contingentRoles)) {
                lock (contingentRoles) {
                    return contingentRoles.ToArray();
                }
            }

            return null;
        }

        public async Task CacheContingentRolesRemovedAsync() {
            activeContingentRoles = new ConcurrentDictionary<ulong, HashSet<ulong>>(await Repository.GetActiveContingentRolesAsync(Parent.Id, Id));
        }


        public void AddVoiceStatusUpdate(int muteChange, int deafenChange) {
            if (muteChange != 0) {
                voiceStatusUpdates.MuteChanges.Add(muteChange);
            }

            if (deafenChange != 0) {
                voiceStatusUpdates.DeafenChanges.Add(muteChange);
            }
        }

        public bool CheckVoiceStatusUpdate(VoiceStatusUpdate voiceStatusUpdate) {
            return voiceStatusUpdates.ContainsUpdate(voiceStatusUpdate);
        }


        public void AddMemberStatusUpdate(ICollection<ulong> rolesAdded, ICollection<ulong> rolesRemoved) {
            if (rolesAdded != null) {
                memberStatusUpdates.RolesAdded.AddRange(rolesAdded);
            }

            if (rolesRemoved != null) {
                memberStatusUpdates.RolesRemoved.AddRange(rolesRemoved);
            }
        }

        public bool CheckMemberStatusUpdate(MemberStatusUpdate memberStatusUpdate) {
            return memberStatusUpdates.ContainsUpdate(memberStatusUpdate);
        }
    }
}
