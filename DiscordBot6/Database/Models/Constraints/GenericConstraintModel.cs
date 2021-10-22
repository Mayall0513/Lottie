using DiscordBot6.Constraints;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot6.Database.Models.Constraints {
    public sealed class GenericConstraintModel : IModelFor<GenericConstraint> {
        public bool Whitelist { get; set; }
        public List<ulong> Requirements { get; } = new List<ulong>();

        public GenericConstraint CreateConcrete() {
            return new GenericConstraint(Whitelist, Requirements);
        }
    }
}
