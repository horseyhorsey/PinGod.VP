using System.Diagnostics;
using System.Linq;
using Xunit;

namespace PinGod.VP.Tests
{
    /// <summary>
    /// focus VP window tests
    /// </summary>
    public class VpWindowTests
    {
        [Fact]
        public void ActivateVpPlayerTEsts()
        {
            var processes = Process.GetProcesses().Where(x => x.ProcessName.Contains("VPinballX"));
            if (processes?.Count() > 0)
            {
                Controller.SetForegroundWindow(processes.ElementAt(0).MainWindowHandle.ToInt32());
            }

            //Controller.BringWindowToTop(8915836);
            //Controller.SetForegroundWindow(8915836);
        }
    }
}
