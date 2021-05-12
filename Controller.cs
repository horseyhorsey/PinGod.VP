using Rug.Osc.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PinGod.VP
{
    [ComVisible(true)]
    [GuidAttribute(ContractGuids.ControllerClass)]
    public class Controller : IController
    {
        private OscSender sender;
        private OscReceiver receiver;
        private Process displayProcess;

        object _coilLock = new object();
        static int[] _lastCoilStates;
        static int[] _coilStates;
        object _lampLock = new object();
        static int[] _lastLampStates;
        static int[] _lampStates;                               

        public bool ControllerRunning { get; private set; }
        public bool GameRunning { get; set; }

        #region Display Properties
        public bool DisplayFullScreen { get; set; }
        public int DisplayWidth { get; set; }
        public int DisplayHeight { get; set; }
        public int DisplayX { get; set; }
        public int DisplayY { get; set; }
        public bool DisplayAlwaysOnTop { get; set; }
        public bool DisplayLowDpi { get; set; }
        public bool DisplayNoWindow { get; set; }
        #endregion

        public byte CoilCount { get; set; } = 32;
        public byte LampCount { get; set; } = 64;
        public int SendPort { get; set; } = 9000;
        public int ReceivePort { get; set; } = 9001;

        int vpHwnd;

        public void Run(int vpHwnd, string game)
        {
            string displayArgs = BuildDisplayArguments();
            var sinfo = new ProcessStartInfo(game, displayArgs);
            sinfo.WorkingDirectory = game;
            Run(vpHwnd, sinfo);
        }

        /// <summary>
        /// Providing Godot is in environment paths
        /// </summary>
        /// <param name="vpHwnd"></param>
        /// <param name="game"></param>
        public void RunDebug(int vpHwnd, string game)
        {
            string displayArgs = BuildDisplayArguments();
            var sinfo = new ProcessStartInfo("godot", displayArgs);
            sinfo.WorkingDirectory = game;
            Run(vpHwnd, sinfo);
        }

        public void SetAction(string action, int pressed)
        {
            sender.Send(new OscMessage("/action", action, pressed));
        }        

        /// <summary>
        /// Sends a switch state
        /// </summary>
        /// <param name="swNum"></param>
        /// <param name="state"></param>
        public void Switch(int swNum, int state)
        {
            SetAction($"sw{swNum}", state);
        }

        /// <summary>
        /// Pause the display
        /// </summary>
        /// <param name="paused"></param>
        public void Pause(int paused)
        {
            SetAction($"pause", paused);
        }

        /// <summary>
        /// Stop and clean up
        /// </summary>
        public void Stop()
        {
            SetAction($"quit", 1);
            Task.Run(() =>
            {
                ControllerRunning = false;
                GameRunning = false;
                receiver?.Close();
                sender?.Close();
                receiver = null;
                sender = null;
                displayProcess?.Kill();
                displayProcess = null;
            });
        }

        public dynamic ChangedSolenoids()
        {
            //find any changed coils since last checked
            var changed = new Dictionary<int, int>();
            for (int i = 0; i < _coilStates.Length - 1; i++)
            {
                if (_coilStates[i] != _lastCoilStates[i])
                {
                    changed.Add(i, _coilStates[i]);
                }

                _lastCoilStates[i] = _coilStates[i];
            }

            //new states found, return object [,]
            if (changed?.Count > 0)
            {
                var arr = new object[changed.Count, 2];
                int i = 0;
                foreach (var coil in changed)
                {
                    arr[i, 0] = coil.Key; arr[i, 1] = coil.Value;
                    i++;
                }

                return arr;
            }

            var arr2 = new object[1, 2];
            return arr2;
        }

        public dynamic ChangedLamps()
        {
            var changed = new Dictionary<int, int>();
            for (int i = 0; i < _lampStates.Length - 1; i++)
            {
                if (_lampStates[i] != _lastLampStates[i])
                {
                    changed.Add(i, _lampStates[i]);
                }

                _lastLampStates[i] = _lampStates[i];
            }

            //create new object to send to VP with changed lamp states
            if (changed?.Count > 0)
            {
                var arr = new object[changed.Count, 2];
                int i = 0;
                foreach (var coil in changed)
                {
                    arr[i, 0] = coil.Key;
                    arr[i, 1] = coil.Value;
                    i++;
                }

                return arr;
            }

            var arr2 = new object[1, 2];
            return arr2;
        }

        [DllImportAttribute("User32.dll")]
        private static extern System.IntPtr SetForegroundWindow(int hWnd);

        /// <summary>
        /// Activates the Visual pinball player
        /// </summary>
        /// <param name="vpHwnd"></param>
        private static void ActivateVpWindow(int vpHwnd)
        {
            if (vpHwnd > 0)
                SetForegroundWindow(vpHwnd);
        }

        void ConnectOsc()
        {
            if (sender == null)
            {
                sender = new OscSender(IPAddress.Loopback, SendPort);
                sender.Connect();
            }

            //recieve messages from the game. Coils, Lamps
            if (receiver == null)
            {
                receiver = new OscReceiver(IPAddress.Loopback, ReceivePort);
                Task.Run(() =>
                {
                    receiver.Connect();
                    while (ControllerRunning)
                    {
                        var packet = receiver.Receive();
                        var bytes = packet.ToByteArray();
                        var message = OscMessage.Read(bytes, bytes.Length);

                        if (message.Address == "/evt")
                        {
                            if (message[0].ToString() == "game_ready")
                            {
                                ActivateVpWindow(vpHwnd);
                                GameRunning = true;
                            }
                        }
                        else if (message.Address == "/coils")
                        {
                            int.TryParse(message[0].ToString(), out var coilId);
                            int.TryParse(message[1].ToString(), out var coilState);

                            lock (_coilLock)
                            {
                                _coilStates[coilId] = coilState;
                            }
                        }
                        else if (message.Address == "/lamps")
                        {
                            int.TryParse(message[0].ToString(), out var lampId);
                            int.TryParse(message[1].ToString(), out var lampState);

                            lock (_lampLock)
                            {
                                _lampStates[lampId] = lampState;
                            }
                        }
                    }
                });
            }
        }

        private void Run(int vpHwnd, ProcessStartInfo startInfo)
        {
            this.vpHwnd = vpHwnd;

            _lampStates = Enumerable.Repeat(0, LampCount).ToArray();
            _coilStates = Enumerable.Repeat(0, CoilCount).ToArray();
            _lastLampStates = Enumerable.Repeat(0, LampCount).ToArray();
            _lastCoilStates = Enumerable.Repeat(0, CoilCount).ToArray();

            //send switches
            ControllerRunning = true;

            ConnectOsc();

            displayProcess = new Process();
            displayProcess.StartInfo = startInfo;
            displayProcess.Start();
        }

        private string BuildDisplayArguments()
        {
            var displayArgs = $"--position {DisplayX}, {DisplayY} ";
            if (DisplayWidth > 0 && DisplayHeight > 0)
                displayArgs += $"--resolution {DisplayWidth}x{DisplayHeight}";
            displayArgs = DisplayFullScreen ? displayArgs += " -f" : displayArgs;
            displayArgs = DisplayAlwaysOnTop ? displayArgs += " -t" : displayArgs;
            displayArgs = DisplayLowDpi ? displayArgs += " --low-dpi" : displayArgs;
            displayArgs = DisplayNoWindow ? displayArgs += " --no-window" : displayArgs;
            return displayArgs;
        }
    }
}