namespace DiscordBot6.Database.Models {
    public sealed class ServerModel : IModelFor<Server> {
        public ulong Id { get; set; }
        public bool AutoMutePersist { get; set; }
        public bool AutoDeafenPersist { get; set; }
        public bool AutoRolePersist { get; set; }

        public Server CreateConcrete() {
            return new Server(Id, AutoMutePersist, AutoDeafenPersist, AutoRolePersist);
        }
    }
}
