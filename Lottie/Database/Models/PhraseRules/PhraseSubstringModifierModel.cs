using Lottie.PhraseRules;
using System.Collections.Generic;

namespace Lottie.Database.Models.PhraseRules {
    public sealed class PhraseSubstringModifierModel : IModelFor<PhraseSubstringModifier> {
        public ulong Id { get; set; }

        public int ModifierType { get; set; }
        public int SubstringStart { get; set; }
        public int SubstringEnd { get; set; }

        public List<string> Data { get; } = new List<string>();

        public PhraseSubstringModifier CreateConcrete() {
            return new PhraseSubstringModifier((SubstringModifierType)ModifierType, SubstringStart, SubstringEnd, Data);
        }
    }
}
