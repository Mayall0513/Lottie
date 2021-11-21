namespace Lottie.Database.Models {
    public interface IModelFor<T> {
        T CreateConcrete();
    }
}
