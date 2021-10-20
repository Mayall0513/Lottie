using Dapper;
using DiscordBot6.Database.Models;
using DiscordBot6.Database.Models.PhraseRules;
using DiscordBot6.Database.Models.ServerRules;
using DiscordBot6.PhraseRules;
using DiscordBot6.Users;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;


namespace DiscordBot6.Database {
    public static class Repository {
        public static async Task<Server> GetServerAsync(ulong id) {
            using MySqlConnection dbConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            ServerModel serverModel = await dbConnection.QuerySingleOrDefaultAsync<ServerModel>("sp_Get_Server", new { id }, commandType: CommandType.StoredProcedure);

            if (serverModel == null) {
                return null;
            }

            else {
                return serverModel.CreateConcrete();
            }
        }

        public static async Task AddOrUpdateServerAsync(Server server) {
            using MySqlConnection dbConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await dbConnection.ExecuteAsync("sp_AddOrUpdate_Server", new { server.Id, server.AutoMutePersist, server.AutoDeafenPersist, server.AutoRolePersist }, commandType: CommandType.StoredProcedure);
        }

        public static async Task<PhraseRule[]> GetPhraseRulesAsync(ulong serverId) {
            List<PhraseRule> phraseRules = new List<PhraseRule>();
            Dictionary<ulong, PhraseRuleModel> phraseRuleModels = new Dictionary<ulong, PhraseRuleModel>();

            using MySqlConnection dbConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);          
            IEnumerable<dynamic> phraseRuleElements = await dbConnection.QueryAsync<dynamic>("sp_Get_PhraseRules", new { serverId }, commandType: CommandType.StoredProcedure);

            foreach (dynamic phraseRuleElement in phraseRuleElements) {
                PhraseRuleModel phraseRuleModel = new PhraseRuleModel() { Id = phraseRuleElement.Id, ServerId = phraseRuleElement.ServerId, Text = phraseRuleElement.Text, ManualPattern = phraseRuleElement.ManualPattern, Pattern = phraseRuleElement.PhrasePattern, PcreOptions = phraseRuleElement.PcreOptions };
                if (!phraseRuleModels.TryAdd(phraseRuleElement.Id, phraseRuleModel)) {
                    phraseRuleModel = phraseRuleModels[phraseRuleElement.Id];
                }
                
                if (phraseRuleElement.ServerConstraintType != null) {
                    ServerRuleConstraintModel serverRuleConstraintModel = new ServerRuleConstraintModel() { Id = phraseRuleElement.ServerConstraintId, ConstraintType = phraseRuleElement.ServerConstraintType };

                    phraseRuleModel.ServerRules.TryAdd(serverRuleConstraintModel.Id, serverRuleConstraintModel);
                    phraseRuleModel.ServerRules[serverRuleConstraintModel.Id].Data.Add(phraseRuleElement.ServerConstraintData);
                }

                if (phraseRuleElement.PhraseConstraintType != null) {
                    PhraseRuleConstraintModel phraseRuleConstraintModel = new PhraseRuleConstraintModel() { Id = phraseRuleElement.PhraseConstraintId, ConstraintType = phraseRuleElement.PhraseConstraintType };

                    phraseRuleModel.PhraseRules.TryAdd(phraseRuleConstraintModel.Id, phraseRuleConstraintModel);
                    phraseRuleModel.PhraseRules[phraseRuleConstraintModel.Id].Data.Add(phraseRuleElement.PhraseConstraintData);
                }

                if (phraseRuleElement.OverrideType != null) {
                    PhraseHomographOverrideModel phraseHomographOverrideModel = new PhraseHomographOverrideModel() { Id = phraseRuleElement.HomographId, OverrideType = phraseRuleElement.OverrideType, Pattern = phraseRuleElement.HomographPattern };
                    
                    phraseRuleModel.HomographOverrides.TryAdd(phraseHomographOverrideModel.Id, phraseHomographOverrideModel);
                    phraseRuleModel.HomographOverrides[phraseHomographOverrideModel.Id].Homographs.Add(phraseRuleElement.HomographData);
                }

                if (phraseRuleElement.ModifierType != null) {
                    PhraseSubstringModifierModel phraseSubstringModifierModel = new PhraseSubstringModifierModel() { Id = phraseRuleElement.SubstringId, ModifierType = phraseRuleElement.ModifierType, SubstringStart = phraseRuleElement.SubstringStart, SubstringEnd = phraseRuleElement.SubstringEnd };

                    phraseRuleModel.SubstringModifiers.TryAdd(phraseSubstringModifierModel.Id, phraseSubstringModifierModel);
                    phraseRuleModel.SubstringModifiers[phraseSubstringModifierModel.Id].Data.Add(phraseRuleElement.SubstringData);
                }
            }

            foreach (PhraseRuleModel phraseRuleModel in phraseRuleModels.Values) {
                phraseRules.Add(phraseRuleModel.CreateConcrete());
            }
            

            return phraseRules.ToArray();
        }

        public static async Task AddOrUpdateUserSettingsAsync(ulong serverId, ulong userId, UserSettings userSettings) {
            using MySqlConnection dbConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString); 
            await dbConnection.ExecuteAsync("sp_AddOrUpdate_UserSettings", new { serverId, userId, userSettings.MutePersisted, userSettings.DeafenPersisted }, commandType: CommandType.StoredProcedure);
        }

        public static async Task<UserSettings> GetUserSettingsAsync(ulong serverId, ulong userId) {
            using MySqlConnection dbConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            UserSettingsModel userSettingsModel = await dbConnection.QuerySingleOrDefaultAsync<UserSettingsModel>("sp_Get_UserSettings", new { serverId, userId }, commandType: CommandType.StoredProcedure);

            if (userSettingsModel == null) {
                return null;
            }

            else {
                return userSettingsModel.CreateConcrete();
            }
        }

        public static async Task AddRolePersistAsync(ulong serverId, ulong userId, ulong roleId) {
            using MySqlConnection dbConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            await dbConnection.ExecuteAsync("sp_Add_RolePersist", new { serverId, userId, roleId }, commandType: CommandType.StoredProcedure);
        }

        public static async Task<ulong[]> GetRolePersistsAsync(ulong serverId, ulong userId) {
            using MySqlConnection dbConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString);
            return (await dbConnection.QueryAsync<ulong>("sp_Get_RolePersists", new { serverId, userId }, commandType: CommandType.StoredProcedure)).ToArray();
        }

        public static async Task RemoveRolePersistAsync(ulong serverId, ulong userId, ulong roleId) {
            using MySqlConnection dbConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString); 
            await dbConnection.ExecuteAsync("sp_Remove_RolePersist", new { serverId, userId, roleId }, commandType: CommandType.StoredProcedure);
        }

        // we need this because the models are value types, we need to convert the models while also creating a shallow copy. no combination of LINQ statements can do this.. for some reason
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
