using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Commands.Contexts;
using DiscordBot6.Helpers;
using DiscordBot6.Timing;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("rolepersist")]
    public sealed class RolePersists_CheckUser : ModuleBase<SocketGuildCommandContext> {
        [Command("checkuser")]
        public async Task Command(params string[] arguments) {
            ArgumentsHelper argumentsHelper = ArgumentsHelper.ExtractFromArguments(Context, arguments)
                .WithUser(out SocketGuildUser callee, out string[] calleeErrors);

            if (!await argumentsHelper.AssertArgumentAsync(callee, calleeErrors)) {
                return;
            }

            Server server = await Context.Guild.GetServerAsync();
            if (!await server.UserMatchesConstraints(Database.ConstraintIntents.ROLEPERSIST_CHECK, null, Context.User.GetRoleIds(), Context.User.Id)) {
                await Context.Channel.CreateResponse()
                    .WithUserSubject(Context.User)
                    .SendNoPermissionsAsync();

                return;
            }

            ResponseBuilder responseBuilder = await CreateResponseAsync(Context.Guild, Context.User, callee, 0, Context.Channel);
            if (responseBuilder != null) {
                await responseBuilder.SendMessageAsync();
            }
        }

        public static async Task<ResponseBuilder> CreateResponseAsync(SocketGuild guild, SocketGuildUser caller, SocketGuildUser callee, int page, IMessageChannel messageChannel = null) {
            User user = await guild.GetUserAsync(callee.Id);
            RolePersist[] rolePersists = user.GetRolesPersisted();
            IEnumerable<RolePersist> pageContents = PaginationHelper.PerformPagination(rolePersists, page, out bool firstPage, out bool finalPage, out string pageDescriptor);

            string title = new StringBuilder().Append(CommandHelper.GetUserIdentifier(callee.Id, callee)).Append(" mute persists").Append(DiscordBot6.DiscordNewLine).ToString();
            if (rolePersists == null || rolePersists.Length == 0) {
                return messageChannel.CreateResponse()
                    .AsSuccess()
                    .WithCustomSubject($"Created by {caller.Username}")
                    .WithTimeStamp()
                    .WithButton(null, $"rolepersist_check@{page}|{caller.Id}&{callee.Id}", DiscordBot6.RefreshPageEmoji)
                    .WithText(title + DiscordBot6.DiscordNewLine + "User has no role persists");
            }

            StringBuilder rolePersistsBuilder = new StringBuilder().Append($"*Showing {pageDescriptor} of {rolePersists.Length}*").Append(DiscordBot6.DiscordNewLine).Append(DiscordBot6.DiscordNewLine);
            foreach (RolePersist rolePersist in pageContents) {
                SocketRole socketRole = guild.GetRole(rolePersist.RoleId);
                rolePersistsBuilder.Append(CommandHelper.GetRoleIdentifier(rolePersist.RoleId, socketRole));

                if (rolePersist.Expiry != null) {
                    rolePersistsBuilder.Append(" until ").Append(CommandHelper.GetResponseDateTime(rolePersist.Expiry.Value));
                }

                rolePersistsBuilder.Append(DiscordBot6.DiscordNewLine);
            }

            return messageChannel.CreateResponse()
                .AsSuccess()
                .WithCustomSubject($"Created by {caller.Username}")
                .WithTimeStamp()
                .WithText(title + rolePersistsBuilder.ToString())
                .WithButton(null, $"rolepersist_check@{page - 1}|{caller.Id} &{callee.Id}", DiscordBot6.PreviousPageEmoji, !firstPage)
                .WithButton(null, $"rolepersist_check@{page + 1}|{caller.Id}&{callee.Id}", DiscordBot6.NextPageEmoji, !finalPage)
                .WithButton(null, $"rolepersist_check@{page}|{caller.Id}&{callee.Id}", DiscordBot6.RefreshPageEmoji);
        }
    }
}
