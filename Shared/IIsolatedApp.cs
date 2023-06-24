using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared;
public interface IIsolatedApp {
    #region Properties
    int HostVersion { get; set; }
    string HostVersionFriendly { get; set; }
    #endregion

    #region Methods
    public void Startup();
    public void End();
    #endregion
}
