using Newtonsoft.Json;
using PinGod.VP;
using Rug.Osc.Core;
using System.Drawing;
using System.Net;
using System.Text;
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

        [Theory]
        //[InlineData(100)] //Won't run with all set
        //[InlineData(10)]
        [InlineData(1)]
        public async Task SendAllCoilsMessage_Tests(byte delayMs)
        {
            //connect send / receive -- set to running otherwise no receiving
            Sender.Connect();            
            Controller.ControllerRunning = true;
            Controller.ConnectOsc();

            var arr = new byte[,] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 } };
            //set all coils to 0
            Sender.Send(new OscMessage("/all_coils", JsonConvert.SerializeObject(arr)));            
            object[,] changed = Controller.ChangedSolenoids();

            //Coil 3 On send /all_coils
            arr = new byte[,] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 1 } };
            Sender.Send(new OscMessage("/all_coils", JsonConvert.SerializeObject(arr)));
            await Task.Delay(delayMs);            
            changed = Controller.ChangedSolenoids();
            Assert.True(changed.Length > 0);

            //coil 3 Off
            arr = new byte[,] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 } };
            Sender.Send(new OscMessage("/all_coils", JsonConvert.SerializeObject(arr)));
            await Task.Delay(delayMs);            
            changed = Controller.ChangedSolenoids();
            Assert.True(changed.Length > 0);

            //none changed since last time should be 0
            changed = Controller.ChangedSolenoids();
            Assert.True(changed.Length <= 0);


            await Task.Delay(500);
            Controller.ControllerRunning = false;

            //dispose
            Sender.Close();
            Controller.Stop();
        }

        [Theory]
        //[InlineData(100)] //Won't run with all set
        //[InlineData(10)]
        [InlineData(1)]
        public async Task SendAllLampsMessage_Tests(byte delayMs)
        {
            //connect send / receive -- set to running otherwise no receiving
            Sender.Connect();
            Controller.ControllerRunning = true;
            Controller.ConnectOsc();

            var arr = new byte[,] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 } };
            //set all lamp to 0
            Sender.Send(new OscMessage("/all_lamps", JsonConvert.SerializeObject(arr)));
            object[,] changed = Controller.ChangedLamps();

            //lamp 3 On send /all_lamps
            arr = new byte[,] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 1 } };
            Sender.Send(new OscMessage("/all_lamps", JsonConvert.SerializeObject(arr)));
            await Task.Delay(delayMs);
            changed = Controller.ChangedLamps();
            Assert.True(changed.Length > 0);

            //lamp 3 Off
            arr = new byte[,] { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 } };
            Sender.Send(new OscMessage("/all_lamps", JsonConvert.SerializeObject(arr)));
            await Task.Delay(delayMs);
            changed = Controller.ChangedLamps();
            Assert.True(changed.Length > 0);

            //none changed since last time should be 0
            changed = Controller.ChangedLamps();
            Assert.True(changed.Length <= 0);


            await Task.Delay(500);
            Controller.ControllerRunning = false;

            //dispose
            Sender.Close();
            Controller.Stop();
        }

        [Theory]
        //[InlineData(100)] //Won't run with all set
        //[InlineData(10)]
        [InlineData(1)]
        public async Task SendAllLedsMessage_Tests(byte delayMs)
        {
            //connect send / receive -- set to running otherwise no receiving
            Sender.Connect();
            Controller.ControllerRunning = true;
            Controller.ConnectOsc();

            var arr = new int[,] { { 0, 0, 0 },  { 1, 0 , 0} , { 2, 0, 0} , { 3, 0, 0}  };
            //set all led to 0
            Sender.Send(new OscMessage("/all_leds", JsonConvert.SerializeObject(arr)));
            object[,] changed = Controller.ChangedPDLeds();

            //led 3 On send /all_leds
            arr = new int[,] { { 0, 0, 0 }, { 1, 0, 0 }, { 2, 0, 0 }, { 3, 0, ColorTranslator.ToOle(Color.Red) } };
            Sender.Send(new OscMessage("/all_leds", JsonConvert.SerializeObject(arr)));
            await Task.Delay(delayMs);
            changed = Controller.ChangedPDLeds();
            Assert.True(changed.Length > 0);

            //led 3 Off
            arr = new int[,] {  { 0, 0, 0 }, {  1, 0, 0 }, {  2, 0, 0  }, {  3, 0, 0  } };
            Sender.Send(new OscMessage("/all_leds", JsonConvert.SerializeObject(arr)));
            await Task.Delay(delayMs);
            changed = Controller.ChangedPDLeds();
            Assert.True(changed.Length > 0);

            //none changed since last time should be 0
            changed = Controller.ChangedPDLeds();
            Assert.True(changed.Length <= 0);

            await Task.Delay(500);
            Controller.ControllerRunning = false;

            //dispose
            Sender.Close();
            Controller.Stop();
        }
    }
}
