using DiscordBot6.Constraints;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot6.Database.Models.Constraints {
    class CRUConstraintsModel : IModelFor<CRUConstraints> {
        public GenericConstraintModel ChannelConstraintModel { get; set; } = new GenericConstraintModel();
        public RoleConstraintModel RoleConstraintModel { get; set; } = new RoleConstraintModel();
        public GenericConstraintModel UserConstraintModel { get; set; } = new GenericConstraintModel();

        public CRUConstraints CreateConcrete() {
            return new CRUConstraints(ChannelConstraintModel.CreateConcrete(), RoleConstraintModel.CreateConcrete(), UserConstraintModel.CreateConcrete());
        }
    }
}
