using Lottie.Constraints;
using System.Collections.Generic;

namespace Lottie.Database.Models.Constraints {
    public sealed class GenericConstraintModel : IModelFor<GenericConstraint> {
        public bool Whitelist { get; set; }
        public HashSet<ulong> Requirements { get; } = new HashSet<ulong>();

        public GenericConstraint CreateConcrete() {
            return new GenericConstraint(Whitelist, Requirements);
        }
    }
}
