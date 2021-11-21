using System.Collections.Generic;
using System.Linq;

namespace Lottie.Constraints {
    public sealed class RoleConstraint {
        private readonly bool whitelistStrict;
        private readonly HashSet<ulong> whitelistRequirements;

        private readonly bool blacklistStrict;
        private readonly HashSet<ulong> blacklistRequirements;

        public RoleConstraint(bool whitelistStrict, IEnumerable<ulong> whitelistRequirements, bool blacklistStrict, IEnumerable<ulong> blacklistRequirements) {
            this.whitelistStrict = whitelistStrict;
            if (whitelistRequirements.Any()) {
                this.whitelistRequirements = new HashSet<ulong>(whitelistRequirements);
            }

            this.blacklistStrict = blacklistStrict;
            if (blacklistRequirements.Any()) {
                this.blacklistRequirements = new HashSet<ulong>(blacklistRequirements);
            }
        }

        public bool Matches(IEnumerable<ulong> roleIds) {
            if (whitelistRequirements != null) {
                if (whitelistStrict) { // everything inside of the requirements must be there (or not)
                    foreach (ulong roleId in roleIds) {
                        if (!whitelistRequirements.Contains(roleId)) {
                            return false;
                        }
                    }
                }

                else { // anything inside of the requirements must be there (or not)
                    bool failed = true;

                    foreach (ulong roleId in roleIds) {
                        if (whitelistRequirements.Contains(roleId)) {
                            failed = false;
                            break;
                        }
                    }

                    if (failed) {
                        return false;
                    }
                }
            }

            if (blacklistRequirements != null) {
                if (blacklistStrict) { // everything inside of the requirements must not be there (or not)
                    foreach (ulong roleId in roleIds) {
                        if (blacklistRequirements.Contains(roleId)) {
                            return false;
                        }
                    }
                }

                else { // anything inside of the requirements must not be there (or not)
                    bool failed = false;

                    foreach (ulong roleId in roleIds) {
                        if (!blacklistRequirements.Contains(roleId)) {
                            failed = true;
                            break;
                        }
                    }

                    if (failed) {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
