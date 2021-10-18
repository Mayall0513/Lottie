using System.Collections.Generic;

namespace DiscordBot6.Database.Models {
    public sealed class PhraseRuleModel {
        public string Text { get; set; }
        public bool ManualPattern { get; set; }
        public string Pattern { get; set; }
        public long? PcreOptions { get; set; }

        public Dictionary<ulong, ServerRuleConstraintModel> ServerRules { get; set; } = new Dictionary<ulong, ServerRuleConstraintModel>();
        public Dictionary<ulong, PhraseRuleConstraintModel> PhraseRules { get; set; } = new Dictionary<ulong, PhraseRuleConstraintModel>();
        public Dictionary<ulong, PhraseHomographOverrideModel> HomographOverrides { get; set; } = new Dictionary<ulong, PhraseHomographOverrideModel>();
        public Dictionary<ulong, PhraseSubstringModifierModel> SubstringModifiers { get; set; } = new Dictionary<ulong, PhraseSubstringModifierModel>();

        public PhraseRuleModel(string text, bool manualPattern, string pattern, long? pcreOptions) {
            Text = text;
            ManualPattern = manualPattern;
            Pattern = pattern;
            PcreOptions = pcreOptions;
        }
    }
}
