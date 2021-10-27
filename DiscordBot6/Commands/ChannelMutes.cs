using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Helpers;
using DiscordBot6.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("channelmutes")]
    public sealed class ChannelMutes : ModuleBase<SocketCommandContext> {
        [Command]
        public async Task Command(ulong id) {
            await CommandImpl(id);
        }

        [Command]
        public async Task Command(IUser user) {
            await CommandImpl(user.Id);
        }

        private async Task CommandImpl(ulong userId) {
            if (Context.User is SocketGuildUser socketGuildUser) {
                Server server = await Context.Guild.GetServerAsync();
                IEnumerable<ulong> userIds = socketGuildUser.Roles.Select(x => x.Id);

                if (!await server.UserMayCheckMutePersists(socketGuildUser.Id, userIds)) {
                    await ResponseHelper.SendNoPermissionsResponse(Context.Channel);
                    return;
                }

                User serverUser = await server.GetUserAsync(userId);

                if (serverUser == null) {
                    await ResponseHelper.SendUserNotFoundResponse(Context.Channel, userId);
                    return;
                }

                IEnumerable<MutePersist> mutePersists = serverUser.GetMutesPersisted();

                if (!mutePersists.Any()) {
                    await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateSimpleSuccessEmbed("No channel mute persists"));
                    return;
                }

                StringBuilder mutePersistsBuilder = new StringBuilder();
                foreach (MutePersist mutePersist in mutePersists) {
                    SocketVoiceChannel voiceChannel = Context.Guild.GetVoiceChannel(mutePersist.ChannelId);
                    if (voiceChannel == null) {
                        mutePersistsBuilder.Append("`").Append(mutePersist.ChannelId).Append("`");
                    }

                    else {
                        mutePersistsBuilder.Append("<#").Append(mutePersist.ChannelId).Append("> `").Append(voiceChannel.Name).Append("`");
                    }
                    

                    if (mutePersist.Expiry != null) {
                        mutePersistsBuilder.Append(" until `").Append(mutePersist.Expiry).Append("`");
                    }

                    mutePersistsBuilder.Append(DiscordBot6.DiscordNewLine);
                }

                await Context.Channel.SendMessageAsync(embed: MessageHelper.CreateSimpleSuccessEmbed(mutePersistsBuilder.ToString()));
            }
        }
    }
}
