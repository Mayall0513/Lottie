using System.Threading.Tasks;

using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Updates;

namespace DiscordBot6.Proto {
    public sealed class ServerUpdateListener : ServerUpdates.ServerUpdatesBase {
        public override Task<Empty> NotifyServerUpdated(ServerUpdated request, ServerCallContext context) {
            Server.ResetServerCache(request.ServerId);
            return Task.FromResult(new Empty());
        }
    }
}
