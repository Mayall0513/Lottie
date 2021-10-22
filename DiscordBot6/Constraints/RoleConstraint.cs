using System.Collections.Generic;

namespace DiscordBot6.Constraints {
    public struct RoleConstraint {
        private readonly bool whitelistStrict;
        private readonly HashSet<ulong> whitelistRequirements;

        private readonly bool blacklistStrict;
        private readonly HashSet<ulong> blacklistRequirements;

        public RoleConstraint(bool whitelistStrict, IEnumerable<ulong> whitelistRequirements, bool blacklistStrict, IEnumerable<ulong> blacklistRequirements) {
            this.whitelistStrict = whitelistStrict;
            this.whitelistRequirements = new HashSet<ulong>(whitelistRequirements);

            this.blacklistStrict = blacklistStrict;
            this.blacklistRequirements = new HashSet<ulong>(blacklistRequirements);
        }

        public bool Matches(IEnumerable<ulong> roleIds) {
            if (whitelistRequirements != null) {
                if (whitelistStrict) { // everything inside of the requirements must be there (or not)
                    bool failed = false;

                    foreach (ulong roleId in roleIds) {
                        if (!whitelistRequirements.Contains(roleId)) {
                            failed = true;
                            break;
                        }
                    }

                    if (failed) {
                        return false;
                    }
                }

                else { // anything inside of the requirements must be there (or not)
                    foreach (ulong roleId in roleIds) {
                        if (whitelistRequirements.Contains(roleId)) {
                            return false;
                        }
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
