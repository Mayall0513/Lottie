using Dapper;
using Dapper.Transaction;
using Lottie.Constraints;
using Lottie.ContingentRoles;
using Lottie.Database.Models;
using Lottie.Database.Models.Constraints;
using Lottie.Database.Models.ContingentRoles;
using Lottie.Database.Models.PhraseRules;
using Lottie.PhraseRules;
using Lottie.Timing;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Lottie.Database {
    public static class Repository {
        public static async Task<Server> GetServerAsync(ulong id) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            IEnumerable<dynamic> serverElements = await connection.QueryAsync<dynamic>("sp_Get_Server", new { id }, commandType: CommandType.StoredProcedure);

            if (serverElements.Any()) {
                ServerModel serverModel = new ServerModel();

                foreach (dynamic serverElement in serverElements) {
                    serverModel.Id = serverElement.Id;
                    serverModel.CommandPrefix = serverElement.CommandPrefix;
                    serverModel.LogChannelId = serverElement.LogChannelId;
                    serverModel.JailRoleId = serverElement.JailRoleId;
                    serverModel.AutoMutePersist = serverElement.AutoMutePersist;
                    serverModel.AutoDeafenPersist = serverElement.AutoDeafenPersist;
                    serverModel.AutoRolePersist = serverElement.AutoRolePersist;

                    if (serverElement.ChannelId != null) {
                        serverModel.CommandChannels.Add(serverElement.ChannelId);
                    }

                    if (serverElement.MessageType != null) {
                        serverModel.CustomMessages.TryAdd(serverElement.MessageType, new List<string>());
                        serverModel.CustomMessages[serverElement.MessageType].Add(serverElement.MessageText);
                    }
                }

                return serverModel.CreateConcrete();
            }

            return null;
        }

        public static async Task AddOrUpdateServerAsync(Server server) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_AddOrUpdate_Server", new { server.Id, server.AutoMutePersist, server.AutoDeafenPersist, server.AutoRolePersist }, commandType: CommandType.StoredProcedure);
        }


        public static async Task<IEnumerable<PhraseRule>> GetPhraseRulesAsync(ulong serverId) {
            List<PhraseRule> phraseRules = new List<PhraseRule>();
            Dictionary<ulong, PhraseRuleModel> phraseRuleModels = new Dictionary<ulong, PhraseRuleModel>();

            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            IEnumerable<dynamic> phraseRuleElements = await connection.QueryAsync<dynamic>("sp_Get_PhraseRules", new { serverId }, commandType: CommandType.StoredProcedure);

            foreach (dynamic phraseRuleElement in phraseRuleElements) {
                PhraseRuleModel phraseRuleModel = new PhraseRuleModel() { Id = phraseRuleElement.Id, ServerId = phraseRuleElement.ServerId, Text = phraseRuleElement.Text, ManualPattern = phraseRuleElement.ManualPattern, Pattern = phraseRuleElement.PhrasePattern, PcreOptions = phraseRuleElement.PcreOptions };
                if (!phraseRuleModels.TryAdd(phraseRuleModel.Id, phraseRuleModel)) {
                    phraseRuleModel = phraseRuleModels[phraseRuleModel.Id];
                }

                if (phraseRuleElement.PhraseConstraintType != null) {
                    PhraseRuleModifierModel phraseRuleConstraintModel = new PhraseRuleModifierModel() { Id = phraseRuleElement.ModifierId, ConstraintType = phraseRuleElement.ModifierType };

                    phraseRuleModel.PhraseRules.TryAdd(phraseRuleConstraintModel.Id, phraseRuleConstraintModel);
                    phraseRuleModel.PhraseRules[phraseRuleConstraintModel.Id].Data.Add(phraseRuleElement.PhraseConstraintData);
                }

                if (phraseRuleElement.OverrideType != null) {
                    PhraseHomographOverrideModel phraseHomographOverrideModel = new PhraseHomographOverrideModel() { Id = phraseRuleElement.HomographId, OverrideType = phraseRuleElement.OverrideType, Pattern = phraseRuleElement.HomographPattern };

                    phraseRuleModel.HomographOverrides.TryAdd(phraseHomographOverrideModel.Id, phraseHomographOverrideModel);
                    phraseRuleModel.HomographOverrides[phraseHomographOverrideModel.Id].Homographs.Add(phraseRuleElement.HomographData);
                }

                if (phraseRuleElement.ModifierType != null) {
                    PhraseSubstringModifierModel phraseSubstringModifierModel = new PhraseSubstringModifierModel() { Id = phraseRuleElement.SubstringId, ModifierType = phraseRuleElement.SubstringModifierType, SubstringStart = phraseRuleElement.SubstringStart, SubstringEnd = phraseRuleElement.SubstringEnd };

                    phraseRuleModel.SubstringModifiers.TryAdd(phraseSubstringModifierModel.Id, phraseSubstringModifierModel);
                    phraseRuleModel.SubstringModifiers[phraseSubstringModifierModel.Id].Data.Add(phraseRuleElement.SubstringData);
                }
            }

            foreach (PhraseRuleModel phraseRuleModel in phraseRuleModels.Values) {
                phraseRules.Add(phraseRuleModel.CreateConcrete());
            }

            return phraseRules;
        }


        public static async Task AddOrUpdateUserAsync(User user) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_AddOrUpdate_User", new { ServerId = user.Parent.Id, UserId = user.Id, user.GlobalMutePersisted, user.GlobalDeafenPersisted }, commandType: CommandType.StoredProcedure);
        }

        public static async Task<User> GetUserAsync(ulong serverId, ulong userId) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            UserModel userModel = await connection.QuerySingleOrDefaultAsync<UserModel>("sp_Get_User", new { serverId, userId }, commandType: CommandType.StoredProcedure);

            return userModel?.CreateConcrete();
        }


        public static async Task AddOrUpdateRolePersistedAsync(ulong serverId, ulong userId, ulong roleId, DateTime? expiry) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_AddOrUpdate_RolePersist", new { serverId, userId, roleId, expiry }, commandType: CommandType.StoredProcedure);
        }

        public static async Task AddOrUpdateRolesPersistedAsync(ulong serverId, ulong userId, IEnumerable<ulong> roleIds, DateTime? expiry) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.OpenAsync();

            using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            foreach (ulong roleId in roleIds) {
                await transaction.ExecuteAsync("sp_AddOrUpdate_RolePersist", new { serverId, userId, roleId, expiry }, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync();
        }

        public static async Task RemoveRolePersistedAsync(ulong serverId, ulong userId, ulong roleId) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_Remove_RolePersist", new { serverId, userId, roleId }, commandType: CommandType.StoredProcedure);
        }

        public static async Task RemoveRolesPersistedAsync(ulong serverId, ulong userId, IEnumerable<ulong> roleIds) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.OpenAsync();

            using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            foreach (ulong roleId in roleIds) {
                await transaction.ExecuteAsync("sp_Remove_RolePersist", new { serverId, userId, roleId }, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync();
        }

        public static async IAsyncEnumerable<RolePersist> GetRolePersistsAllAsync(IEnumerable<ulong> serverIds) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);

            foreach (ulong serverId in serverIds) {
                IEnumerable<RolePersist> rolePersists = await connection.QueryAsync<RolePersist>("sp_Get_RolePersists_All", new { serverId }, commandType: CommandType.StoredProcedure);

                foreach (RolePersist rolePersist in rolePersists) {
                    yield return rolePersist;
                }
            }
        }


        public static async Task AddOrUpdateMutePersistedAsync(ulong serverId, ulong userId, ulong channelId, DateTime? expiry) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_AddOrUpdate_MutePersist", new { serverId, userId, channelId, expiry }, commandType: CommandType.StoredProcedure);
        }

        public static async Task RemoveMutePersistedAsync(ulong serverId, ulong userId, ulong channelId) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_Remove_MutePersist", new { serverId, userId, channelId }, commandType: CommandType.StoredProcedure);
        }

        public static async IAsyncEnumerable<MutePersist> GetMutePersistsAllAsync(IEnumerable<ulong> serverIds) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);

            foreach (ulong serverId in serverIds) {
                IEnumerable<MutePersist> mutePersists = await connection.QueryAsync<MutePersist>("sp_Get_MutePersists_All", new { serverId }, commandType: CommandType.StoredProcedure);

                foreach (MutePersist mutePersist in mutePersists) {
                    yield return mutePersist;
                }
            }
        }


        public static async Task<IEnumerable<ContingentRole>> GetContingentRulesAsync(ulong serverId) {
            List<ContingentRole> contingentRoles = new List<ContingentRole>();
            Dictionary<ulong, ContingentRoleModel> contingentRoleModels = new Dictionary<ulong, ContingentRoleModel>();

            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            IEnumerable<dynamic> contingentRoleElements = await connection.QueryAsync<dynamic>("sp_Get_ContingentRoles", new { serverId }, commandType: CommandType.StoredProcedure);

            foreach (dynamic contingentRoleElement in contingentRoleElements) {
                ContingentRoleModel contingentRoleModel = new ContingentRoleModel() { Id = contingentRoleElement.Id, ServerId = contingentRoleElement.ServerId, RoleId = contingentRoleElement.RoleId };
                if (!contingentRoleModels.TryAdd(contingentRoleModel.Id, contingentRoleModel)) {
                    contingentRoleModel = contingentRoleModels[contingentRoleModel.Id];
                }

                if (contingentRoleElement.ContingentRoleId != null) {
                    contingentRoleModel.ContingentRoles.Add(contingentRoleElement.ContingentRoleId);
                }
            }

            foreach (ContingentRoleModel contingentRoleModel in contingentRoleModels.Values) {
                contingentRoles.Add(contingentRoleModel.CreateConcrete());
            }

            return contingentRoles;
        }


        public static async Task AddActiveContingentRoleAsync(ulong serverId, ulong userId, ulong roleId, ulong contingentRoleId) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_Add_ContingentRoles_Active", new { serverId, userId, roleId, contingentRoleId }, commandType: CommandType.StoredProcedure);
        }

        public static async Task AddActiveContingentRolesAsync(ulong serverId, ulong userId, ulong roleId, IEnumerable<ulong> contingentRoleIds) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.OpenAsync();

            using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            foreach (ulong contingentRoleId in contingentRoleIds) {
                await transaction.ExecuteAsync("sp_Add_ContingentRoles_Active", new { serverId, userId, roleId, contingentRoleId }, commandType: CommandType.StoredProcedure);
            }

            await transaction.CommitAsync();
        }

        public static async Task RemoveActiveContingentRolesAsync(ulong serverId, ulong userId, ulong roleId) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_Remove_ContingentRoles_Active", new { serverId, userId, roleId }, commandType: CommandType.StoredProcedure);
        }

        public static async Task<Dictionary<ulong, HashSet<ulong>>> GetActiveContingentRolesAsync(ulong serverId, ulong userId) {
            Dictionary<ulong, HashSet<ulong>> activeContingentRoles = new Dictionary<ulong, HashSet<ulong>>();

            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            IEnumerable<dynamic> activeContingentRoleElements = await connection.QueryAsync<dynamic>("sp_Get_ContingentRoles_Active", new { serverId, userId }, commandType: CommandType.StoredProcedure);

            foreach (dynamic activeContingentRoleElement in activeContingentRoleElements) {
                if (!activeContingentRoles.ContainsKey(activeContingentRoleElement.RoleId)) {
                    activeContingentRoles.TryAdd(activeContingentRoleElement.RoleId, new HashSet<ulong>());
                }

                if (activeContingentRoleElement.ContingentRoleId != null) {
                    activeContingentRoles[activeContingentRoleElement.RoleId].Add(activeContingentRoleElement.ContingentRoleId);
                }
            }

            return activeContingentRoles;
        }


        public static async Task<CRUConstraints> GetConstraints(ulong serverId, ConstraintIntents intent) {
            CRUConstraintsModel cruConstraintsModel = new CRUConstraintsModel();

            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            IEnumerable<dynamic> genericConstraintElements = await connection.QueryAsync<dynamic>("sp_Get_Constraints_Generic", new { serverId, intent }, commandType: CommandType.StoredProcedure);
            IEnumerable<dynamic> roleConstraintElements = await connection.QueryAsync<dynamic>("sp_Get_Constraints_Roles", new { serverId, intent }, commandType: CommandType.StoredProcedure);

            foreach (dynamic genericConstraintElement in genericConstraintElements) {
                switch (genericConstraintElement.ConstraintType) {
                    case GenericConstraintTypes.USER:
                        cruConstraintsModel.UserConstraintModel.Whitelist = genericConstraintElement.Whitelist;
                        cruConstraintsModel.UserConstraintModel.Requirements.Add(genericConstraintElement.Data);
                        break;

                    case GenericConstraintTypes.CHANNEL:
                        cruConstraintsModel.ChannelConstraintModel.Whitelist = genericConstraintElement.Whitelist;
                        cruConstraintsModel.ChannelConstraintModel.Requirements.Add(genericConstraintElement.Data);
                        break;
                }
            }

            foreach (dynamic roleConstraintElement in roleConstraintElements) {
                cruConstraintsModel.RoleConstraintModel.WhitelistStrict = roleConstraintElement.WhitelistStrict;
                cruConstraintsModel.RoleConstraintModel.BlacklistStrict = roleConstraintElement.BlacklistStrict;

                if (roleConstraintElement.Whitelist) {
                    cruConstraintsModel.RoleConstraintModel.WhitelistRequirements.Add(roleConstraintElement.RoleId);
                }

                else {
                    cruConstraintsModel.RoleConstraintModel.BlacklistRequirements.Add(roleConstraintElement.RoleId);
                }
            }

            return cruConstraintsModel.CreateConcrete();
        }

        // we need this because the models are value types. due to this we need to convert the models while also creating a shallow copy. no combination of LINQ statements can do this.. for some reason
        public static IEnumerable<O> ConvertValues<T, O>(ICollection<T> collection, Func<T, O> selector) {
            O[] outputArray = new O[collection.Count];
            int index = 0;

            foreach (T element in collection) {
                outputArray[index] = selector(element);
                index++;
            }

            return outputArray;
        }
    }
}
