using System;
using System.Data;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;

using DiscordBot6.ServerRules;
using DiscordBot6.PhraseRules;
using DiscordBot6.Database.Models;

using MySqlConnector;

using Dapper;

using PCRE;

namespace DiscordBot6.Database {
    public static class Repository {
        public static async Task<PhraseRule[]> GetPhraseRules(ulong serverId) {
            List<PhraseRule> phraseRules = new List<PhraseRule>();

            using (MySqlConnection dbConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Database"].ConnectionString)) {
                Dictionary<ulong, PhraseRuleModel> phraseRuleModels = new Dictionary<ulong, PhraseRuleModel>();
                IEnumerable<dynamic> phraseRuleElements = await dbConnection.QueryAsync<dynamic>("sp_GetPhraseRules", new { serverId }, commandType: CommandType.StoredProcedure);

                foreach (dynamic phraseRuleElement in phraseRuleElements) {
                    phraseRuleModels.TryAdd(phraseRuleElement.PhraseRuleId, new PhraseRuleModel(phraseRuleElement.Text, phraseRuleElement.ManualPattern, phraseRuleElement.Pattern, phraseRuleElement.PcreOptions));
                    PhraseRuleModel phraseRuleModel = phraseRuleModels[phraseRuleElement.PhraseRuleId];

                    if (phraseRuleElement.ServerConstraintType != null) {
                        phraseRuleModel.ServerRules.TryAdd(phraseRuleElement.ServerConstraintId, new ServerRuleConstraintModel(phraseRuleElement.ServerConstraintType));
                        phraseRuleModel.ServerRules[phraseRuleElement.ServerConstraintId].Constraints.Add(phraseRuleElement.ServerConstraintData);
                    }

                    if (phraseRuleElement.PhraseConstraintType != null) {
                        phraseRuleModel.PhraseRules.TryAdd(phraseRuleElement.PhraseConstraintId, new PhraseRuleConstraintModel(phraseRuleElement.PhraseConstraintType));
                        phraseRuleModel.PhraseRules[phraseRuleElement.PhraseConstraintId].Data.Add(phraseRuleElement.PhraseConstraintData);
                    }

                    if (phraseRuleElement.OverrideType != null) {
                        phraseRuleModel.HomographOverrides.TryAdd(phraseRuleElement.HomographId, new PhraseHomographOverrideModel(phraseRuleElement.OverrideType, phraseRuleElement.Pattern));
                        phraseRuleModel.HomographOverrides[phraseRuleElement.HomographId].Homographs.Add(phraseRuleElement.HomographData);
                    }

                    if (phraseRuleElement.ModifierType != null) {
                        phraseRuleModel.SubstringModifiers.TryAdd(phraseRuleElement.SubstringId, new PhraseSubstringModifierModel(phraseRuleElement.ModifierType, phraseRuleElement.SubstringStart, phraseRuleElement.SubstringEnd));
                        phraseRuleModel.SubstringModifiers[phraseRuleElement.SubstringId].Data.Add(phraseRuleElement.SubstringData);
                    }
                }

                foreach (PhraseRuleModel phraseRuleModel in phraseRuleModels.Values) {
                    phraseRules.Add(CreatePhraseRuleFromModel(serverId, phraseRuleModel));
                }
            }

            return phraseRules.ToArray();
        }

        public static PhraseRule CreatePhraseRuleFromModel(ulong serverId, PhraseRuleModel ruleModel) {
            IEnumerable<ServerRuleConstraint> serverRuleConstraints = ConvertValues(ruleModel.ServerRules.Values, x => new ServerRuleConstraint((ServerRuleConstraintType) x.ConstraintType, x.Data));
            IEnumerable<PhraseRuleConstraint> phraseRuleConstraints = ConvertValues(ruleModel.PhraseRules.Values, x => new PhraseRuleConstraint((PhraseRuleConstraintType) x.ConstraintType, x.Data));

            if (ruleModel.ManualPattern) {
                return new PhraseRule(serverId, ruleModel.Pattern, (PcreOptions) (ruleModel.PcreOptions ?? 0), serverRuleConstraints, phraseRuleConstraints);
            }

            else {
                IEnumerable<PhraseHomographOverride> homographOverrides = ConvertValues(ruleModel.HomographOverrides.Values, x => new PhraseHomographOverride((HomographOverrideType) x.OverrideType, x.Pattern, x.Homographs));
                IEnumerable<PhraseSubstringModifier> substringModifiers = ConvertValues(ruleModel.SubstringModifiers.Values, x => new PhraseSubstringModifier((SubstringModifierType) x.ModifierType, x.SubstringStart, x.SubstringEnd, x.Data));

                return new PhraseRule(serverId, ruleModel.Text, serverRuleConstraints, phraseRuleConstraints, homographOverrides, substringModifiers);
            }
        }

        private static IEnumerable<O> ConvertValues<T, O>(ICollection<T> collection, Func<T, O> selector) { // we need this because the models are value types, we need to convert the models while also creating a shallow copy. no combination of LINQ statements can do this.. for some reason
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
