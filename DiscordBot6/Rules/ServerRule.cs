using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBot6.Rules {
    public class ServerRule {
        public ulong ServerId { get; private set; }
        public IReadOnlyCollection<ServerRuleConstraint> Constriants { get; private set; }

        protected ulong[] userConstraints = null;
        protected bool userWhitelist = false;

        protected ulong[] channelConstraints = null;
        protected bool channelWhitelist = false;

        protected ulong[] roleWhitelist = null;
        protected bool roleWhitelistAll = false;

        protected ulong[] roleBlacklist = null;
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
                if (channelWhitelist) { // chanel whitelist
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

        protected ServerRule(ulong serverId, ServerRuleConstraint[] contraints) {
            ServerId = serverId;
            Constriants = contraints;

            DeriveMetaInformation();
        }

        private void DeriveMetaInformation() {
            foreach (ServerRuleConstraint constraint in Constriants) {
                switch (constraint.ConstraintType) {
                    case RuleConstraintType.CONSTRAINT_USER_WHITELIST:
                        userConstraints = constraint.Constraints;
                        userWhitelist = true;
                        break;

                    case RuleConstraintType.CONSTRAINT_USER_BLACKLIST:
                        userConstraints = constraint.Constraints;
                        userWhitelist = false;
                        break;

                    case RuleConstraintType.CONSTRAINT_CHANNEL_WHITELIST:
                        channelConstraints = constraint.Constraints;
                        channelWhitelist = true;
                        break;

                    case RuleConstraintType.CONSTRAINT_CHANNEL_BLACKLIST:
                        channelConstraints = constraint.Constraints;
                        channelWhitelist = false;
                        break;

                    case RuleConstraintType.CONSTRAINT_ROLE_WHITELIST_ALL:
                        roleWhitelist = constraint.Constraints;
                        roleWhitelistAll = true;
                        break;

                    case RuleConstraintType.CONSTRAINT_ROLE_WHITELIST_ANY:
                        roleWhitelist = constraint.Constraints;
                        roleWhitelistAll = false;
                        break;

                    case RuleConstraintType.CONSTRAINT_ROLE_BLACKLIST_ALL:
                        roleBlacklist = constraint.Constraints;
                        roleBlacklistAll = true;
                        break;

                    case RuleConstraintType.CONSTRAINT_ROLE_BLACKLIST_ANY:
                        roleBlacklist = constraint.Constraints;
                        roleBlacklistAll = false;
                        break;
                }
            }
        }
    }
}
