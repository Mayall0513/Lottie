namespace DiscordBot6.Database.Models {
    public interface IModelFor<T> {
        T CreateConcrete();
    }
}
