using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lottie.Commands.Contexts;
using Lottie.Database;
using Lottie.Helpers;
using Lottie.Timing;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lottie.Commands {
    [Group("cmute")]
    public sealed class ChannelMutePersists_CheckUser : ModuleBase<SocketGuildCommandContext> {
        [Command("checkuser")]
        public async Task Command(params string[] arguments) {
            ArgumentsHelper argumentsHelper = ArgumentsHelper.ExtractFromArguments(Context, arguments)
                .WithUser(out SocketGuildUser callee, out string[] errors);

            if (!await argumentsHelper.AssertArgumentAsync(callee, errors)) {
                return;
            }

            Server server = await Context.Guild.L_GetServerAsync();
            if (!await server.UserMatchesConstraintsAsync(ConstraintIntents.CHANNELMUTE_CHECK, null, Context.User.GetRoleIds(), Context.User.Id)) {
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
            User user = await guild.L_GetUserAsync(callee.Id);
            MutePersist[] mutePersists = user.GetMutesPersisted();
            IEnumerable<MutePersist> pageContents = PaginationHelper.PerformPagination(mutePersists, page, out bool firstPage, out bool finalPage, out string pageDescriptor);

            string title = new StringBuilder().Append(CommandHelper.GetUserIdentifier(callee.Id, callee)).Append(" mute persists").Append(Lottie.DiscordNewLine).ToString();
            if (mutePersists == null || mutePersists.Length == 0) {
                return messageChannel.CreateResponse()
                    .AsSuccess()
                    .WithCustomSubject($"Created by {caller.Username}")
                    .WithTimeStamp()
                    .WithButton(null, $"channelmutepersist_check@{page}|{caller.Id}&{callee.Id}", Lottie.RefreshPageEmoji)
                    .WithText(title + Lottie.DiscordNewLine + "User has no mute persists");
            }

            StringBuilder rolePersistsBuilder = new StringBuilder().Append($"*Showing {pageDescriptor} of {mutePersists.Length}*").Append(Lottie.DiscordNewLine).Append(Lottie.DiscordNewLine);
            foreach (MutePersist mutePersist in pageContents) {
                SocketVoiceChannel voiceChannel = guild.GetVoiceChannel(mutePersist.ChannelId);
                rolePersistsBuilder.Append(CommandHelper.GetChannelIdentifier(mutePersist.ChannelId, voiceChannel));

                if (mutePersist.Expiry != null) {
                    rolePersistsBuilder.Append(" until ").Append(CommandHelper.GetResponseDateTime(mutePersist.Expiry.Value));
                }

                rolePersistsBuilder.Append(Lottie.DiscordNewLine);
            }

            return messageChannel.CreateResponse()
                .AsSuccess()
                .WithCustomSubject($"Created by {caller.Username}")
                .WithTimeStamp()
                .WithText(title + rolePersistsBuilder.ToString())
                .WithButton(null, $"channelmutepersist_check@{page - 1}|{caller.Id}&{callee.Id}", Lottie.PreviousPageEmoji, !firstPage)
                .WithButton(null, $"channelmutepersist_check@{page + 1}|{caller.Id}&{callee.Id}", Lottie.NextPageEmoji, !finalPage)
                .WithButton(null, $"channelmutepersist_check@{page}|{caller.Id}&{callee.Id}", Lottie.RefreshPageEmoji);
        }
    }
}
