using DiscordBot6.Constraints;
using DiscordBot6.ContingentRoles;
using DiscordBot6.Database;
using DiscordBot6.PhraseRules;
using DiscordBot6.Timing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6 {
    public sealed class Server {
        private static readonly ConcurrentDictionary<ulong, Server> serverCache = new ConcurrentDictionary<ulong, Server>();
        private readonly ConcurrentDictionary<ulong, User> users = new ConcurrentDictionary<ulong, User>();

        private IReadOnlyCollection<PhraseRule> phraseRules;
        private IReadOnlyCollection<ContingentRole> contingentRoles;

        public ulong Id { get; }

        private readonly string commandPrefix;

        public bool HasLogChannel => logChannelId.HasValue;
        public ulong LogChannelId => logChannelId.Value;

        public bool HasJailRole => jailRoleId.HasValue;
        public ulong JailRoleId => jailRoleId.Value;
        
        private readonly ulong? logChannelId;
        private readonly ulong? jailRoleId;

        public bool AutoMutePersist { get; }
        public bool AutoDeafenPersist { get; }
        public bool AutoRolePersist { get; }
        
        private readonly HashSet<ulong> commandChannels = new HashSet<ulong>();

        private readonly ConcurrentDictionary<PresetMessageTypes, string[]> customMessages = new ConcurrentDictionary<PresetMessageTypes, string[]>();
        private readonly ConcurrentDictionary<ConstraintIntents, CRUConstraints> constraints = new ConcurrentDictionary<ConstraintIntents, CRUConstraints>();
        private readonly ConcurrentDictionary<ulong, List<RolePersist>> rolePersistsCache = new ConcurrentDictionary<ulong, List<RolePersist>>();
        private readonly ConcurrentDictionary<ulong, List<MutePersist>> mutePersistsCache = new ConcurrentDictionary<ulong, List<MutePersist>>();

        public Server(ulong id, string commandPrefix, ulong? logChannelId, ulong? jailRoleId, bool autoMutePersist, bool autoDeafenPersist, bool autoRolePersist, IEnumerable<ulong> commandChannels, ConcurrentDictionary<PresetMessageTypes, string[]> customMesages) {
            Id = id;

            this.commandPrefix = commandPrefix;
            this.logChannelId = logChannelId;
            this.jailRoleId = jailRoleId;

            AutoMutePersist = autoMutePersist;
            AutoDeafenPersist = autoDeafenPersist;
            AutoRolePersist = autoRolePersist;

            this.commandChannels = new HashSet<ulong>(commandChannels);
            this.customMessages = customMesages;
        }

        public static async Task<Server> GetServerAsync(ulong id) {
            if (!serverCache.TryGetValue(id, out Server server)) {
                server = await Repository.GetServerAsync(id);
                if (server == null) {
                    server = new Server(id, null, null, null, true, true, false, Enumerable.Empty<ulong>(), new ConcurrentDictionary<PresetMessageTypes, string[]>());
                    await Repository.AddOrUpdateServerAsync(server);
                }

                serverCache.TryAdd(id, server);
            }

            return server;
        }

        public static bool ResetServerCache(ulong id) {
            return serverCache.TryRemove(id, out _);
        }


        public async Task<IEnumerable<PhraseRule>> GetPhraseRuleSetsAsync() {
            if (phraseRules == null) {
                phraseRules = await Repository.GetPhraseRulesAsync(Id) as IReadOnlyCollection<PhraseRule>;
            }

            return phraseRules;
        }

        public async Task<IEnumerable<ContingentRole>> GetContingentRolesAsync() {
            if (contingentRoles == null) {
                contingentRoles = await Repository.GetContingentRulesAsync(Id) as IReadOnlyCollection<ContingentRole>;
            }

            return contingentRoles;
        }


        public async Task<User> GetUserAsync(ulong id) {
            if (!users.TryGetValue(id, out User user)) {
                user = await Repository.GetUserAsync(Id, id);

                if (user == null) {
                    user = new User(id, false, false) {
                        Parent = this
                    };

                    await Repository.AddOrUpdateUserAsync(user);
                }

                else {
                    user.Parent = this;
                }

                users.TryAdd(id, user);
            }

            return user;
        }

        public async Task SetUserAsync(ulong id, User user) {
            users.AddOrUpdate(id,
                (id) => user,
                (id, oldUser) => user
            );

            await Repository.AddOrUpdateUserAsync(user);
        }

        public async Task<bool> UserMatchesConstraints(ConstraintIntents intent, ulong? channelId = null, IEnumerable<ulong> roleIds = null, ulong? userId = null) {
            if (!constraints.TryGetValue(intent, out CRUConstraints intentConstraints)) {
                intentConstraints = await Repository.GetConstraints(Id, intent);
                constraints.TryAdd(intent, intentConstraints);
            }

            if (intentConstraints != null) {
                return true;
            }

            return intentConstraints.Matches(channelId, roleIds, userId);
        }

        public string GetPresetMessage(PresetMessageTypes messageType, ulong messageId) {
            if (!customMessages.TryGetValue(messageType, out string[] messages)) {
                return null;
            }

            messageId--;
            if (messageId >= (ulong) messages.Length) {
                return null;
            }

            return messages[messageId];
        }


        public bool IsCommandChannel(ulong channelId) {
            return commandChannels.Count == 0 || commandChannels.Contains(channelId);
        }

        public string GetCommandPrefix() {
            return commandPrefix ?? DiscordBot6.DefaultCommandPrefix;
        }
    
    
        public void CacheRolePersist(RolePersist rolePersist) {
            List<RolePersist> rolePersists = rolePersistsCache.GetOrAdd(rolePersist.RoleId, new List<RolePersist>());
            lock (rolePersists) {
                rolePersists.Add(rolePersist);
            }
        }

        public void UncacheRolePersist(RolePersist rolePersist) {
            if (rolePersistsCache.TryGetValue(rolePersist.RoleId, out List<RolePersist> rolePersists)) {
                lock (rolePersists) {
                    rolePersists.Remove(rolePersist);
                }
            }
        }

        public RolePersist[] GetRoleCache(ulong roleId) {
            if (rolePersistsCache.TryGetValue(roleId, out List<RolePersist> rolePersists)) {
                lock (rolePersists) {
                    return rolePersists.ToArray();
                }
            }

            return null;
        }


        public void CacheMutePersist(MutePersist mutePersist) {
            List<MutePersist> mutePersists = mutePersistsCache.GetOrAdd(mutePersist.ChannelId, new List<MutePersist>());
            lock (mutePersists) {
                mutePersists.Add(mutePersist);
            }
        }

        public void UncacheMutePersist(MutePersist mutePersist) {
            if (mutePersistsCache.TryGetValue(mutePersist.ChannelId, out List<MutePersist> mutePersists)) {
                lock (mutePersists) {
                    mutePersists.Remove(mutePersist);
                }
            }
        }

        public MutePersist[] GetMuteCache(ulong channelId) {
            if (mutePersistsCache.TryGetValue(channelId, out List<MutePersist> mutePersists)) {
                lock (mutePersists) {
                    return mutePersists.ToArray();
                }
            }

            return null;
        }
    }
}
