using System.Linq;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

using Dapper;

using PCRE;

using DiscordBot6.Phrases;
using DiscordBot6.Rules;

using MySqlConnector;
using System;

namespace DiscordBot6 {
    public sealed class ServerInformation {
        public ulong Id { get; set; }

        private PhraseRule[] phraseRules;

        public ServerInformation(ulong id) {
            Id = id;
        }

        public async Task<PhraseRule[]> GetPhraseRuleSetsAsync() {
            if (phraseRules == null) {
                List<PhraseRule> phraseRules = new List<PhraseRule>();

                using (MySqlConnection dbConnection = new MySqlConnection("Server=SG-discordbot6testcluster-5123-mysql-master.servers.mongodirector.com; Database=discordbot6; Uid=sgroot; Pwd=a^p5UUfYK0graKyO; SslMode=None;")) {
                    Dictionary<ulong, PhraseRuleModel> phraseRuleModels = new Dictionary<ulong, PhraseRuleModel>();
                    IEnumerable<dynamic> phraseRuleElements = await dbConnection.QueryAsync<dynamic>("sp_GetPhraseRules", new { serverId = Id }, commandType: CommandType.StoredProcedure);

                    foreach (dynamic phraseRuleElement in phraseRuleElements) {
                        phraseRuleModels.TryAdd(phraseRuleElement.PhraseRuleId, new PhraseRuleModel(phraseRuleElement.Text, phraseRuleElement.ManualPattern, phraseRuleElement.Pattern, phraseRuleElement.PcreOptions ?? 0, phraseRuleElement.BotDelete, phraseRuleElement.SelfDelete));
                        PhraseRuleModel phraseRuleModel = phraseRuleModels[phraseRuleElement.PhraseRuleId];

                        if (phraseRuleElement.ConstraintType != null) {
                            phraseRuleModel.ServerRules.TryAdd(phraseRuleElement.ServerRuleId, new ServerRuleConstraint((RuleConstraintType) phraseRuleElement.ConstraintType));
                            phraseRuleModel.ServerRules[phraseRuleElement.ServerRuleId].Constraints.Add(phraseRuleElement.ServerRuleData);
                        }

                        if (phraseRuleElement.RequirementType != null) {
                            phraseRuleModel.Constraints.TryAdd(phraseRuleElement.RuleId, new PhraseRuleConstraint((RuleRequirementType) phraseRuleElement.RequirementType));
                            phraseRuleModel.Constraints[phraseRuleElement.RuleId].Data.Add(phraseRuleElement.RuleData);
                        }

                        if (phraseRuleElement.Pattern != null) {
                            phraseRuleModel.HomographOverrides.TryAdd(phraseRuleElement.HomographId, new PhraseHomographOverride(phraseRuleElement.Pattern, (HomographOverrideType) phraseRuleElement.OverrideType));
                            phraseRuleModel.HomographOverrides[phraseRuleElement.HomographId].Homographs.Add(phraseRuleElement.HomographData);
                        }

                        if (phraseRuleElement.SubstringStart != null) {
                            phraseRuleModel.SubstringModifiers.TryAdd(phraseRuleElement.SubstringId, new PhraseSubstringModifier(phraseRuleElement.SubstringStart, phraseRuleElement.SubstringEnd, (SubstringModifierType) phraseRuleElement.ModifierType));
                            phraseRuleModel.SubstringModifiers[phraseRuleElement.SubstringId].Data.Add(phraseRuleElement.SubstringData);
                        }
                    }

                    foreach (PhraseRuleModel phraseRuleModel in phraseRuleModels.Values) {
                        if (phraseRuleModel.ManualPattern) {
                            phraseRules.Add(new PhraseRule(Id, phraseRuleModel.Pattern, (PcreOptions) (phraseRuleModel.PcreOptions ?? 0), phraseRuleModel.ServerRules.Values.ToArray(), phraseRuleModel.Constraints.Values.ToArray()));
                        }

                        else {
                            phraseRules.Add(new PhraseRule(Id, phraseRuleModel.Text, phraseRuleModel.ServerRules.Values.ToArray(), phraseRuleModel.Constraints.Values.ToArray(), phraseRuleModel.HomographOverrides.Values.ToArray(), phraseRuleModel.SubstringModifiers.Values.ToArray()));
                        }

                        Console.WriteLine(phraseRules.Last().Pattern);
                    }
                }

                this.phraseRules = phraseRules.ToArray();
            }

            return phraseRules;
        }
    }
}
