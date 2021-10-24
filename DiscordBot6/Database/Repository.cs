﻿using Dapper;
using Dapper.Transaction;
using DiscordBot6.Constraints;
using DiscordBot6.ContingentRoles;
using DiscordBot6.Database.Models;
using DiscordBot6.Database.Models.Constraints;
using DiscordBot6.Database.Models.ContingentRoles;
using DiscordBot6.Database.Models.PhraseRules;
using DiscordBot6.PhraseRules;
using DiscordBot6.Timing;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6.Database {
    public static class Repository {
        public enum GenericConstraintTypes : uint {
            USER,
            CHANNEL
        }

        public enum ConstraintIntents : uint {
            TEMPMUTE,
            MUTE,
            GIVEROLES
        }

        public static async Task<Server> GetServerAsync(ulong id) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            ServerModel serverModel = await connection.QuerySingleOrDefaultAsync<ServerModel>("sp_Get_Server", new { id }, commandType: CommandType.StoredProcedure);

            return serverModel?.CreateConcrete();
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


        public static async Task AddRolePersistedAsync(ulong serverId, ulong userId, ulong roleId) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_Add_RolePersist", new { serverId, userId, roleId }, commandType: CommandType.StoredProcedure);
        }

        public static async Task AddRolesPersistedAsync(ulong serverId, ulong userId, IEnumerable<ulong> roleIds) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.OpenAsync();

            using MySqlTransaction transaction = await connection.BeginTransactionAsync();

            foreach (ulong roleId in roleIds) {
                await transaction.ExecuteAsync("sp_Add_RolePersist", new { serverId, userId, roleId }, commandType: CommandType.StoredProcedure);
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

        public static async Task<IEnumerable<ulong>> GetRolesPersistsAsync(ulong serverId, ulong userId) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            return await connection.QueryAsync<ulong>("sp_Get_RolePersists", new { serverId, userId }, commandType: CommandType.StoredProcedure);
        }


        public static async Task AddMutePersistedAsync(ulong serverId, ulong userId, ulong channelId, DateTime? expiry) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_Add_MutePersist", new { serverId, userId, channelId, expiry }, commandType: CommandType.StoredProcedure);
        }

        public static async Task RemoveMutePersistedAsync(ulong serverId, ulong userId, ulong channelId) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await connection.ExecuteAsync("sp_Remove_MutePersist", new { serverId, userId, channelId }, commandType: CommandType.StoredProcedure);
        }

        public static async Task<IEnumerable<MutePersist>> GetMutePersistsAsync(ulong serverId, ulong userId) {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            return await connection.QueryAsync<MutePersist>("sp_Get_MutePersists", new { serverId, userId }, commandType: CommandType.StoredProcedure);
        }
        
        public static async Task<IEnumerable<MutePersist>> GetMutePersistsAllAsync() {
            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            return await connection.QueryAsync<MutePersist>("sp_Get_MutePersists_All", null, commandType: CommandType.StoredProcedure);
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
            CRUConstraintsModel crucConstraintsModel = new CRUConstraintsModel();

            using MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            IEnumerable<dynamic> genericConstraintElements = await connection.QueryAsync<dynamic>("sp_Get_Constraints_Generic", new { serverId, intent }, commandType: CommandType.StoredProcedure);
            IEnumerable<dynamic> roleConstraintElements = await connection.QueryAsync<dynamic>("sp_Get_Constraints_Roles", new { serverId, intent }, commandType: CommandType.StoredProcedure);

            foreach (dynamic genericConstraintElement in genericConstraintElements) {
                switch (genericConstraintElement.ConstraintType) {
                    case GenericConstraintTypes.USER:
                        crucConstraintsModel.UserConstraintModel.Whitelist = genericConstraintElement.Whitelist;
                        crucConstraintsModel.UserConstraintModel.Requirements.Add(genericConstraintElement.Data);
                        break;

                    case GenericConstraintTypes.CHANNEL:
                        crucConstraintsModel.ChannelConstraintModel.Whitelist = genericConstraintElement.Whitelist;
                        crucConstraintsModel.ChannelConstraintModel.Requirements.Add(genericConstraintElement.Data);
                        break;
                }
            }

            foreach (dynamic roleConstraintElement in roleConstraintElements) {
                crucConstraintsModel.RoleConstraintModel.WhitelistStrict = roleConstraintElement.WhitelistStrict;
                crucConstraintsModel.RoleConstraintModel.BlacklistStrict = roleConstraintElement.BlacklistStrict;

                if (roleConstraintElement.Whitelist) {
                    crucConstraintsModel.RoleConstraintModel.WhitelistRequirements.Add(roleConstraintElement.RoleId);
                }

                else {
                    crucConstraintsModel.RoleConstraintModel.BlacklistRequirements.Add(roleConstraintElement.RoleId);
                }
            }

            return crucConstraintsModel.CreateConcrete();
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
