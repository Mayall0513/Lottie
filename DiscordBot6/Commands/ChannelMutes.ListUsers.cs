using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Commands.Contexts;
using DiscordBot6.Database;
using DiscordBot6.Helpers;
using DiscordBot6.Timing;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("cmute")]
    public sealed class ChannelMutePersists_ListUsers : ModuleBase<SocketGuildCommandContext> {
        [Command("listusers")]
        public async Task Command(params string[] arguments) {
            ArgumentsHelper argumentsHelper = ArgumentsHelper.ExtractFromArguments(Context, arguments)
                .WithVoiceChannel(out SocketVoiceChannel voiceChannel, out string[] voiceChannelErrors);

            if (!await argumentsHelper.AssertArgumentAsync(voiceChannel, voiceChannelErrors)) {
                return;
            }

            Server server = await Context.Guild.GetServerAsync();
            if (!await server.UserMatchesConstraints(ConstraintIntents.CHANNELMUTE_CHECK, null, Context.User.GetRoleIds(), Context.User.Id)) {
                await Context.Channel.CreateResponse()
                    .WithUserSubject(Context.User)
                    .SendNoPermissionsAsync();

                return;
            }

            ResponseBuilder responseBuilder = CreateResponse(Context.Guild, server, Context.User, voiceChannel, 0, Context.Channel);
            if (responseBuilder != null) {
                await responseBuilder.SendMessageAsync();
            }
        }

        public static ResponseBuilder CreateResponse(SocketGuild guild, Server server, SocketGuildUser caller, SocketVoiceChannel voiceChannel, int page, IMessageChannel messageChannel = null) {
            MutePersist[] mutePersists = server.GetMuteCache(voiceChannel.Id);
            IEnumerable<MutePersist> pageContents = PaginationHelper.PerformPagination(mutePersists, page, out bool firstPage, out bool finalPage, out string pageDescriptor);

            string title = new StringBuilder().Append(CommandHelper.GetChannelIdentifier(voiceChannel.Id, voiceChannel)).Append(" mute persists").Append(DiscordBot6.DiscordNewLine).ToString();
            if (mutePersists == null || mutePersists.Length == 0) {
                return messageChannel.CreateResponse()
                    .AsSuccess()
                    .WithCustomSubject($"Created by {caller.Username}")
                    .WithTimeStamp()
                    .WithButton(null, $"channelmutepersist_list@{page}|{caller.Id}&{voiceChannel.Id}", DiscordBot6.RefreshPageEmoji)
                    .WithText(title + DiscordBot6.DiscordNewLine + "No users have mutes persisted in this channel");
            }

            StringBuilder rolePersistsBuilder = new StringBuilder().Append($"*Showing {pageDescriptor} of {mutePersists.Length}*").Append(DiscordBot6.DiscordNewLine).Append(DiscordBot6.DiscordNewLine);
            foreach (MutePersist mutePersist in pageContents) {
                SocketGuildUser socketGuildUser = guild.GetUser(mutePersist.UserId);
                rolePersistsBuilder.Append(CommandHelper.GetUserIdentifier(mutePersist.UserId, socketGuildUser));

                if (mutePersist.Expiry != null) {
                    rolePersistsBuilder.Append(" until ").Append(CommandHelper.GetResponseDateTime(mutePersist.Expiry.Value));
                }

                rolePersistsBuilder.Append(DiscordBot6.DiscordNewLine);
            }

            return messageChannel.CreateResponse()
                .AsSuccess()
                .WithCustomSubject($"Created by {caller.Username}")
                .WithTimeStamp()
                .WithText(title + rolePersistsBuilder.ToString())
                .WithButton(null, $"channelmutepersist_list@{page - 1}|{caller.Id}&{voiceChannel.Id}", DiscordBot6.PreviousPageEmoji, !firstPage)
                .WithButton(null, $"channelmutepersist_list@{page + 1}|{caller.Id}&{voiceChannel.Id}", DiscordBot6.NextPageEmoji, !finalPage)
                .WithButton(null, $"channelmutepersist_list@{page}|{caller.Id}&{voiceChannel.Id}", DiscordBot6.RefreshPageEmoji);
        }
    }
}
