using Discord;
using DiscordBot6.ContingentRoles;
using DiscordBot6.Database;
using DiscordBot6.PhraseRules;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot6 {
    public static class GuildExtensions {
        public static async Task<Server> GetServerAsync(this IGuild guild) {
            return await Server.GetServerAsync(guild.Id);
        }

        public static async Task<User> GetUserAsync(this IGuild guild, ulong id) {
            return await (await GetServerAsync(guild)).GetUserAsync(id);
        }
    }

    public sealed class Server {
        private static readonly ConcurrentDictionary<ulong, Server> serverCache = new ConcurrentDictionary<ulong, Server>();

        private readonly HashSet<ulong> userVoicesStatusUpdated = new HashSet<ulong>();
        private readonly HashSet<ulong> userRolesUpdated = new HashSet<ulong>();
        private readonly ConcurrentDictionary<ulong, User> users = new ConcurrentDictionary<ulong, User>();

        private IReadOnlyCollection<PhraseRule> phraseRules;
        private IReadOnlyCollection<ContingentRole> contingentRoles;

        public ulong Id { get; }

        public bool AutoMutePersist { get; }
        public bool AutoDeafenPersist { get; }
        public bool AutoRolePersist { get; }

        public Server(ulong id, bool autoMutePersist, bool autoDeafenPersist, bool autoRolePersist) {
            Id = id;

            AutoMutePersist = autoMutePersist;
            AutoDeafenPersist = autoDeafenPersist;
            AutoRolePersist = autoRolePersist;
        }

        public static async Task<Server> GetServerAsync(ulong id) {
            if (!serverCache.ContainsKey(id)) {
                Server newServer = await Repository.GetServerAsync(id);
                if (newServer == null) {
                    newServer = new Server(id, true, true, false);
                    await Repository.AddOrUpdateServerAsync(newServer);
                }

                serverCache.TryAdd(id, newServer);
                return newServer;
            }

            return serverCache[id];
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
            if (!users.ContainsKey(id)) {
                User user = await Repository.GetUserAsync(Id, id);

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

                return user;
            }

            return users[id];
        }

        public async Task SetUserAsync(ulong id, User user) {
            if (!users.ContainsKey(id)) {
                await GetUserAsync(id);
            }

            if (users.ContainsKey(id)) {
                users[id] = user;
                await Repository.AddOrUpdateUserAsync(user);
            }
        }

        public bool TryAddVoiceStatusUpdated(ulong userId) {
            return userVoicesStatusUpdated.Add(userId);
        }

        public bool CheckAndRemoveVoiceStatusUpdated(ulong userId) {
            if (userVoicesStatusUpdated.Contains(userId)) {
                userVoicesStatusUpdated.Remove(userId);
                return true;
            }

            return false;
        }

        public bool TryAddRoleUpdated(ulong userId) {
            return userRolesUpdated.Add(userId);
        }

        public bool CheckAndRemoveRoleUpdated(ulong userId) {
            if (userRolesUpdated.Contains(userId)) {
                userRolesUpdated.Remove(userId);
                return true;
            }

            return false;
        }
    }
}
