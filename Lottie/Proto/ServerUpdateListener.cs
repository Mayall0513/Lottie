using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Threading.Tasks;
using Updates;

namespace Lottie.Proto {
    public sealed class ServerUpdateListener : ServerUpdates.ServerUpdatesBase {
        public override Task<Empty> NotifyServerUpdated(ServerUpdated request, ServerCallContext context) {
            Server.ResetServerCache(request.ServerId);
            return Task.FromResult(new Empty());
        }
    }
}
