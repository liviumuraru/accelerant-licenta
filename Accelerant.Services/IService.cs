using System.Collections.Generic;

namespace Accelerant.Services
{
    public interface IService<TParam, TReturn, TId>
    {
        TReturn Get(TId Id);
        IEnumerable<TReturn> GetMany(IEnumerable<TId> Ids);
        TReturn Update(TParam item);
        TReturn Add(TParam item);
        IEnumerable<TReturn> GetAllForUser(TId UserId);
    }
}
