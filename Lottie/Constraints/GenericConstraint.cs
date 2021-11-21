using System.Collections.Generic;

namespace Lottie.Constraints {
    public sealed class GenericConstraint {
        private readonly bool whitelist;
        private readonly HashSet<ulong> requirements;

        public GenericConstraint(bool whitelist, IEnumerable<ulong> requirements) {
            this.whitelist = whitelist;
            this.requirements = new HashSet<ulong>(requirements);
        }

        public bool Matches(ulong id) {
            if (requirements.Contains(id)) {
                return whitelist;
            }

            return !whitelist;
        }
    }
}
