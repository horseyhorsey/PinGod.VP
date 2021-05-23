using PinGod.VP;
using Rug.Osc.Core;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace PinGod.Tests
{
    [CollectionDefinition(nameof(IntergrationsTestCollectionDefinition), DisableParallelization = true)]
    public class IntergrationsTestCollectionDefinition { }

    [Collection(nameof(IntergrationsTestCollectionDefinition))]
    public class IntegrationTests
    {
        public Controller Controller { get; }
        public OscSender Sender { get; }

        public IntegrationTests()
        {
            Controller = new Controller();
            Sender = new OscSender(IPAddress.Loopback, 9001);
        }        

        [Fact]
        public async Task ConnectOscAndStopController_Tests()
        {
            Controller.ConnectOsc();
            await Task.Delay(50);
            Controller.Stop();
        }
    }
}
