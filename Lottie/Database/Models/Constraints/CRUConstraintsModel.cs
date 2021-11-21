using Lottie.Constraints;

namespace Lottie.Database.Models.Constraints {
    class CRUConstraintsModel : IModelFor<CRUConstraints> {
        public GenericConstraintModel ChannelConstraintModel { get; set; } = new GenericConstraintModel();
        public RoleConstraintModel RoleConstraintModel { get; set; } = new RoleConstraintModel();
        public GenericConstraintModel UserConstraintModel { get; set; } = new GenericConstraintModel();

        public CRUConstraints CreateConcrete() {
            return new CRUConstraints(ChannelConstraintModel.CreateConcrete(), RoleConstraintModel.CreateConcrete(), UserConstraintModel.CreateConcrete());
        }
    }
}
