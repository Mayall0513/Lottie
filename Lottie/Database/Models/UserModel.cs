namespace Lottie.Database.Models {
    public sealed class UserModel : IModelFor<User> {
        public ulong Id { get; set; }
        public bool GlobalMutePersisted { get; set; }
        public bool GlobalDeafenPersisted { get; set; }

        public User CreateConcrete() {
            return new User(Id, GlobalMutePersisted, GlobalDeafenPersisted);
        }
    }
}
