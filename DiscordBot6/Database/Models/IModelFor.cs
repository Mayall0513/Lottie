using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot6.Database.Models {
    public interface IModelFor<T> {
        T CreateConcrete();
    }
}
