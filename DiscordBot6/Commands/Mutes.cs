using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Commands.Contexts;
using DiscordBot6.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6.Commands {
    [Group("mute")]
    public sealed class Mutes : ModuleBase<SocketGuildCommandContext> {
        [Command]
        public async Task Command(IUser user) {
            await CommandImpl(user.Id);
        }

        [Command]
        public async Task Command(ulong userId) {
            await CommandImpl(userId);
        }
        

        public async Task CommandImpl(ulong userId) {
            Server server = await Context.Guild.GetServerAsync();
            IEnumerable<ulong> userRoleIds = Context.User.Roles.Select(role => role.Id);

            if (!await server.UserMayMute(Context.User.Id, userRoleIds)) {
                await Context.Channel.CreateResponse()
                    .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                    .SendNoPermissionAsync();

                return;
            }

            User serverUser = await server.GetUserAsync(userId);
            serverUser.GlobalMutePersisted = true;
            await server.SetUserAsync(userId, serverUser);

            SocketGuildUser socketUser = Context.Guild.GetUser(userId);
            if (socketUser?.VoiceChannel != null && !socketUser.IsMuted) { 
                await socketUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
            }

            string mutedIdentifier = CommandHelper.GetUserIdentifier(userId, socketUser);

            await Context.Channel.CreateResponse()
                .WithSubject(userId, socketUser?.GetAvatarUrl(size: 64))
                .SendGenericSuccessAsync($"Muted {mutedIdentifier}");

            if (server.HasLogChannel) {
                string muterIdentifier = CommandHelper.GetUserIdentifier(Context.User.Id, Context.User);

                await Context.Guild.GetTextChannel(server.LogChannelId).CreateResponse()
                    .WithSubject(Context.User.Id, Context.User.GetAvatarUrl(size: 64))
                    .LogGenericSuccessAsync($"{muterIdentifier} muted {mutedIdentifier}");
            }
        }
    }
}
