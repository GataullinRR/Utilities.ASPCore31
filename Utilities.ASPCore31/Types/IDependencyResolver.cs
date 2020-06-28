using System.Collections.Generic;
using System.Text;

namespace Utilities.Types
{
    public interface IDependencyResolver
    {
        void ResolveProperties(object target, params string[] properties);
    }
}
