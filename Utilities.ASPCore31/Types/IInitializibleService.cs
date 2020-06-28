using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.Types
{
    public interface IInitializibleService
    {
        Task InitializeAsync();
    }
}
