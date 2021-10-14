using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

using DiscordBot6.Phrases;
using DiscordBot6.Rules;
using MySqlConnector;

namespace DiscordBot6 {
    public sealed class ServerInformation {
        public ulong Id { get; set; }

        private PhraseRule[] phraseRuleSets;

        public ServerInformation(ulong id) {
            Id = id;
        }

        public async Task<PhraseRule[]> GetPhraseRuleSetsAsync() {
            if (phraseRuleSets == null) {
                List<PhraseRule> phraseRules = new List<PhraseRule>();

                using (MySqlConnection dbConnection = new MySqlConnection("Server=SG-discordbot6testcluster-5123-mysql-master.servers.mongodirector.com; Database=discordbot6; Uid=sgroot; Pwd=a^p5UUfYK0graKyO; SslMode=None;")) {
                    IEnumerable<PhraseRuleModel> phraseRuleModels = await dbConnection.QueryAsync<PhraseRuleModel>($"SELECT * FROM phraserules WHERE ServerId = {Id} ORDER BY CreationTime");

                    foreach (PhraseRuleModel phraseRuleModel in phraseRuleModels) {
                        IEnumerable<ServerRuleConstraintModel> serverRuleConstraints = await dbConnection.QueryAsync<ServerRuleConstraintModel, ulong, ServerRuleConstraintModel>(
                            $"SELECT * FROM phraserules_constraints_serverrules AS parent" +
                            $"WHERE PhraseRuleId = {phraseRuleModel.Id} " +
                            $"INNER JOIN phraserules_constraints_serverrules_data AS child ON parent.Id = child.ServerRuleId", 

                            (serverRuleConstraintModel, constraintData) => {
                                serverRuleConstraintModel.Constraints.Add(constraintData);
                                return serverRuleConstraintModel;
                            }
                        );

                        foreach(ServerRuleConstraintModel blah in serverRuleConstraints) {
                            Console.WriteLine("!");
                        }

                        List <PhraseRuleConstraint> phraseRuleConstraints = new List<PhraseRuleConstraint>();
                        List<PhraseHomographOverride> homographOverrides = new List<PhraseHomographOverride>();
                        List<PhraseSubstringModifier> substringModifiers = new List<PhraseSubstringModifier>();
                    }
                }

                phraseRuleSets = phraseRules.ToArray();
            }

            return phraseRuleSets;
        }
    }
}
