﻿using Discord;
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
    public sealed class RolePersists_ListUsers : ModuleBase<SocketGuildCommandContext> {
        [Command("listusers")]
        public async Task Command(params string[] arguments) {
            ArgumentsHelper argumentsHelper = ArgumentsHelper.ExtractFromArguments(Context, arguments)
                .WithRole(out SocketRole role, out string[] roleErrors);

            if (!await argumentsHelper.AssertArgumentAsync(role, roleErrors)) {
                return;
            }

            Server server = await Context.Guild.GetServerAsync();
            if (!await server.UserMatchesConstraints(Database.ConstraintIntents.ROLEPERSIST_CHECK, null, Context.User.GetRoleIds(), Context.User.Id)) {
                await Context.Channel.CreateResponse()
                    .WithUserSubject(Context.User)
                    .SendNoPermissionsAsync();

                return;
            }

            ResponseBuilder responseBuilder = CreateResponse(Context.Guild, server, Context.User, role, 0, Context.Channel);
            if (responseBuilder != null) {
                await responseBuilder.SendMessageAsync();
            }
        }

        public static ResponseBuilder CreateResponse(SocketGuild guild, Server server, SocketGuildUser caller, SocketRole role, int page, IMessageChannel messageChannel = null) {
            RolePersist[] rolePersists = server.GetRoleCache(role.Id);
            IEnumerable<RolePersist> pageContents = PaginationHelper.PerformPagination(rolePersists, page, out bool firstPage, out bool finalPage, out string pageDescriptor);

            string title = new StringBuilder().Append(CommandHelper.GetRoleIdentifier(role.Id, role)).Append(" persists").Append(DiscordBot6.DiscordNewLine).ToString();
            if (pageContents == null) {
                return messageChannel.CreateResponse()
                    .AsSuccess()
                    .WithCustomSubject($"Created by {caller.Username}")
                    .WithTimeStamp()
                    .WithButton(null, $"rolepersist_list@{page}|{caller.Id}&{role.Id}", DiscordBot6.RefreshPageEmoji)
                    .WithText(title + DiscordBot6.DiscordNewLine + "No users have this role persisted");
            }

            StringBuilder rolePersistsBuilder = new StringBuilder().Append($"*Showing {pageDescriptor} of {rolePersists.Length}*").Append(DiscordBot6.DiscordNewLine).Append(DiscordBot6.DiscordNewLine);

            foreach (RolePersist rolePersist in pageContents) {
                SocketGuildUser socketGuildUser = guild.GetUser(rolePersist.UserId);
                rolePersistsBuilder.Append(CommandHelper.GetUserIdentifier(rolePersist.UserId, socketGuildUser));

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
                .WithButton(null, $"rolepersist_list@{page - 1}|{caller.Id}&{role.Id}", DiscordBot6.PreviousPageEmoji, !firstPage)
                .WithButton(null, $"rolepersist_list@{page + 1}|{caller.Id}&{role.Id}", DiscordBot6.NextPageEmoji, !finalPage)
                .WithButton(null, $"rolepersist_list@{page}|{caller.Id}&{role.Id}", DiscordBot6.RefreshPageEmoji);
        }
    }
}
