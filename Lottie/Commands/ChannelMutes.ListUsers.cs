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
    public sealed class ChannelMutePersists_ListUsers : ModuleBase<SocketGuildCommandContext> {
        [Command("listusers")]
        public async Task Command(params string[] arguments) {
            ArgumentsHelper argumentsHelper = ArgumentsHelper.ExtractFromArguments(Context, arguments)
                .WithVoiceChannel(out SocketVoiceChannel voiceChannel, out string[] voiceChannelErrors);

            if (!await argumentsHelper.AssertArgumentAsync(voiceChannel, voiceChannelErrors)) {
                return;
            }

            Server server = await Context.Guild.L_GetServerAsync();
            if (!await server.UserMatchesConstraintsAsync(ConstraintIntents.CHANNELMUTE_CHECK, null, Context.User.GetRoleIds(), Context.User.Id)) {
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

            string title = new StringBuilder().Append(CommandHelper.GetChannelIdentifier(voiceChannel.Id, voiceChannel)).Append(" mute persists").Append(Lottie.DiscordNewLine).ToString();
            if (mutePersists == null || mutePersists.Length == 0) {
                return messageChannel.CreateResponse()
                    .AsSuccess()
                    .WithCustomSubject($"Created by {caller.Username}")
                    .WithTimeStamp()
                    .WithButton(null, $"channelmutepersist_list@{page}|{caller.Id}&{voiceChannel.Id}", Lottie.RefreshPageEmoji)
                    .WithText(title + Lottie.DiscordNewLine + "No users have mutes persisted in this channel");
            }

            StringBuilder rolePersistsBuilder = new StringBuilder().Append($"*Showing {pageDescriptor} of {mutePersists.Length}*").Append(Lottie.DiscordNewLine).Append(Lottie.DiscordNewLine);
            foreach (MutePersist mutePersist in pageContents) {
                SocketGuildUser socketGuildUser = guild.GetUser(mutePersist.UserId);
                rolePersistsBuilder.Append(CommandHelper.GetUserIdentifier(mutePersist.UserId, socketGuildUser));

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
                .WithButton(null, $"channelmutepersist_list@{page - 1}|{caller.Id}&{voiceChannel.Id}", Lottie.PreviousPageEmoji, !firstPage)
                .WithButton(null, $"channelmutepersist_list@{page + 1}|{caller.Id}&{voiceChannel.Id}", Lottie.NextPageEmoji, !finalPage)
                .WithButton(null, $"channelmutepersist_list@{page}|{caller.Id}&{voiceChannel.Id}", Lottie.RefreshPageEmoji);
        }
    }
}
