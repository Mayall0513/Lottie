using System;
using System.Linq;
using System.Collections.Generic;

namespace DiscordBot6.ServerRules {
    public class ServerRule {
        public ulong ServerId { get;  }
        public DateTime CreationTime { get; }

        public IReadOnlyCollection<ServerRuleConstraint> RuleConstriants { get; }

        protected IReadOnlyCollection<ulong> userConstraints = null;
        protected bool userWhitelist = false;

        protected IReadOnlyCollection<ulong> channelConstraints = null;
        protected bool channelWhitelist = false;

        protected IReadOnlyCollection<ulong> roleWhitelist = null;
        protected bool roleWhitelistAll = false;

        protected IReadOnlyCollection<ulong> roleBlacklist = null;
        protected bool roleBlacklistAll = false;

        public virtual bool CanApply(ulong userId, ulong channelId, IReadOnlyCollection<ulong> roles) {
            if (userConstraints != null) { // there are user constraints
                if (userWhitelist) { // user whitelist
                    if (!userConstraints.Contains(userId)) {
                        return false;
                    }
                }

                else { // user blacklist
                    if (userConstraints.Contains(userId)) {
                        return false;
                    }
                }
            }

            if (channelConstraints != null) { // there are channel constraints
                if (channelWhitelist) { // channel whitelist
                    if (!channelConstraints.Contains(userId)) {
                        return false;
                    }
                }

                else { // channel blacklist
                    if (channelConstraints.Contains(userId)) {
                        return false;
                    }
                }
            }

            if (roleWhitelist != null) { // there is a role whitelist
                if (roleWhitelistAll) { // the user must have all of the roles in the whitelist
                    foreach (ulong whitelistedRole in roleWhitelist) {
                        if (!roles.Contains(whitelistedRole)) {
                            return false;
                        }
                    }
                }

                else { // the user must have any of the roles in the whitelist
                    bool foundAny = false;

                    foreach (ulong whitelistedRole in roleWhitelist) {
                        if (roles.Contains(whitelistedRole)) {
                            foundAny = true;
                            break;
                        }
                    }

                    if (!foundAny) {
                        return false;
                    }
                }
            }

            if (roleBlacklist != null) { // there is a role blacklist
                if (roleBlacklistAll) { // the user not must have all of the roles in the blacklist
                    bool foundAll = true;

                    foreach (ulong blacklistedRole in roleBlacklist) {
                        if (!roles.Contains(blacklistedRole)) {
                            foundAll = false;
                            break;
                        }
                    }

                    if (foundAll) {
                        return foundAll;
                    }
                }

                else { // the user must not have any of the roles in the blacklist
                    foreach (ulong blacklistedRole in roleBlacklist) {
                        if (roles.Contains(blacklistedRole)) {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        protected ServerRule(ulong serverId, IEnumerable<ServerRuleConstraint> contraints) {
            ServerId = serverId;
            RuleConstriants = contraints as IReadOnlyCollection<ServerRuleConstraint>;

            DeriveMetaInformation();
        }

        private void DeriveMetaInformation() {
            foreach (ServerRuleConstraint constraint in RuleConstriants) {
                switch (constraint.ConstraintType) {
                    case ServerRuleConstraintType.CONSTRAINT_USER_WHITELIST:
                        userConstraints = constraint.Data;
                        userWhitelist = true;
                        break;

                    case ServerRuleConstraintType.CONSTRAINT_USER_BLACKLIST:
                        userConstraints = constraint.Data;
                        userWhitelist = false;
                        break;

                    case ServerRuleConstraintType.CONSTRAINT_CHANNEL_WHITELIST:
                        channelConstraints = constraint.Data;
                        channelWhitelist = true;
                        break;

                    case ServerRuleConstraintType.CONSTRAINT_CHANNEL_BLACKLIST:
                        channelConstraints = constraint.Data;
                        channelWhitelist = false;
                        break;

                    case ServerRuleConstraintType.CONSTRAINT_ROLE_WHITELIST_ALL:
                        roleWhitelist = constraint.Data;
                        roleWhitelistAll = true;
                        break;

                    case ServerRuleConstraintType.CONSTRAINT_ROLE_WHITELIST_ANY:
                        roleWhitelist = constraint.Data;
                        roleWhitelistAll = false;
                        break;

                    case ServerRuleConstraintType.CONSTRAINT_ROLE_BLACKLIST_ALL:
                        roleBlacklist = constraint.Data;
                        roleBlacklistAll = true;
                        break;

                    case ServerRuleConstraintType.CONSTRAINT_ROLE_BLACKLIST_ANY:
                        roleBlacklist = constraint.Data;
                        roleBlacklistAll = false;
                        break;
                }
            }
        }
    }
}
