using Newtonsoft.Json;
using Rug.Osc.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PinGod.VP
{
    [ComVisible(true)]
    [GuidAttribute(ContractGuids.ControllerClass)]
    public class Controller : IController
    {
        /// <summary>
        /// Sends switch events and other actions
        /// </summary>
        private OscSender sender;
        private OscReceiver receiver;
        private Process displayProcess;

        byte[,] _coilStates;
        byte[,] _lastCoilStates;
        byte[,] _lampStates;        
        byte[,] _lastLampStates;
        int[,] _ledStates;
        int[,] _lastLedStates;
        object _coilLock = new object();
        object _lampLock = new object();
        object _ledLock = new object();

        public bool ControllerRunning { get; set; }
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
        public bool DisplayNoBorder { get; set; }
        public bool DisplayMaximized { get; set; }
        #endregion

        public int SendPort { get; set; } = 9000;
        public int ReceivePort { get; set; } = 9001;

        int vpHwnd;

        public dynamic ChangedLamps()
        {
            //find any changed lamps since last checked
            var changed = new List<KeyValuePair<int, int>>();
            var arr = new object[0, 2];
            if (_lastLampStates == null)
            {
                _lastLampStates = _lampStates;
                return arr;
            }
            else if (_lampStates != _lastLampStates)
            {
                //collect the changed lamps
                for (int i = 0; i < _lampStates.Length/2; i++)
                {
                    if (_lampStates[i, 1] != _lastLampStates[i, 1])
                    {
                        changed.Add(new KeyValuePair<int, int>(_lampStates[i, 0], _lampStates[i, 1]));
                    }
                }
                _lastLampStates = _lampStates;
                //create object[,] to send back (VP)
                arr = ToObjectArray(changed);                
                return arr;
            }
            else
            {
                return arr;
            }
        }

        class Led
        {
            public int Num { get; set; }
            public int State { get; set; }
            public int Color { get; set; }
        }

        public dynamic ChangedPDLeds()
        {
            //find any changed leds since last checked
            var changed = new List<Led>();
            var arr = new object[0, 3];
            if (_lastLedStates == null)
            {
                _lastLedStates = _ledStates;
                return arr;
            }
            else if (_ledStates != _lastLedStates)
            {
                //collect the changed lamps
                for (int i = 0; i < _ledStates.Length / 3; i++)
                {
                    if (_ledStates[i, 1] != _lastLedStates[i, 1] || _ledStates[i, 2] != _lastLedStates[i, 2])
                    {
                        changed.Add(new Led() { Num = _ledStates[i, 0], State = _ledStates[i, 1], Color = _ledStates[i, 2] });
                    }
                }
                
                //create object[,] to send back (VP)
                arr = new object[changed.Count, 3];
                int ii = 0;
                foreach (var coil in changed)
                {
                    arr[ii, 0] = coil.Num; 
                    arr[ii, 1] = coil.State;
                    arr[ii, 2] = coil.Color;
                    ii++;
                }
                _lastLedStates = _ledStates;
                return arr;
            }
            else
            {
                return arr;
            }
        }

        public dynamic ChangedSolenoids()
        {
            //find any changed coils since last checked
            var changed = new List<KeyValuePair<int, int>>();
            var arr = new object[0, 2];
            if (_lastCoilStates == null)
            {
                _lastCoilStates = _coilStates;
                return arr;
            }
            else if (_coilStates != _lastCoilStates)
            {
                //collect the changed coils
                for (int i = 0; i < _coilStates.Length / 2; i++)
                {
                    if (_coilStates[i, 1] != _lastCoilStates[i, 1])
                    {
                        changed.Add(new KeyValuePair<int, int>(_coilStates[i, 0], _coilStates[i, 1]));
                    }
                }
                _lastCoilStates = _coilStates;
                //create object[,] to send back (VP)
                arr = ToObjectArray(changed);
                return arr;
            }
            else
            {
                return arr;
            }
        }

        public void ConnectOsc()
        {
            if (sender == null)
            {
                sender = new OscSender(IPAddress.Loopback, SendPort);
                sender.Connect();
            }
            else
            {
                sender.Close();
                sender.Connect();
            }

            //recieve messages from the game. Coils, Lamps
            if (receiver == null)
            {
                receiver = new OscReceiver(IPAddress.Loopback, ReceivePort);
                Task.Run(() =>
                {
                    receiver.Connect();
                    while (receiver?.State == OscSocketState.Connected)
                    {
                        var packet = receiver?.Receive();
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
                        else if (message.Address == "/all_coils")
                        {
                            var coils = JsonConvert.DeserializeObject<byte[,]>(message[0].ToString());
                            if (coils != null)
                            {
                                lock (_coilLock)
                                {
                                    _coilStates = coils;
                                }
                            }
                        }
                        else if (message.Address == "/all_lamps")
                        {
                            var lamps = JsonConvert.DeserializeObject<byte[,]>(message[0].ToString());
                            if (lamps != null)
                            {
                                lock (_lampLock)
                                {
                                    _lampStates = lamps;
                                }
                            }
                        }
                        else if (message.Address == "/all_leds")
                        {
                            var leds = JsonConvert.DeserializeObject<int[,]>(message[0].ToString());
                            if (leds != null)
                            {
                                lock (_ledLock)
                                {
                                    _ledStates = leds;
                                }
                            }
                        }                        
                    }
                });
            }
        }

        /// <summary>
        /// Pause the display
        /// </summary>
        /// <param name="paused"></param>
        public void Pause(int paused)
        {
            SetAction($"pause", paused);
        }

        public void Run(int vpHwnd, string game)
        {
            string displayArgs = BuildDisplayArguments();
            var sinfo = new ProcessStartInfo(game, displayArgs);
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
        /// Stop and clean up
        /// </summary>
        public void Stop()
        {
            SetAction($"quit", 1);
            ControllerRunning = false;
            GameRunning = false;

            receiver?.Close();
            sender?.Close();
            receiver = null;
            sender = null;
            //displayProcess?.Kill();
            displayProcess = null;
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

        #region Private methods

        /// <summary>
        /// Activates the Visual pinball player
        /// </summary>
        /// <param name="vpHwnd"></param>
        private static void ActivateVpWindow(int vpHwnd)
        {
            if (vpHwnd > 0)
                SetForegroundWindow(vpHwnd);
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
            displayArgs = DisplayNoBorder ? displayArgs += " -lf true" : displayArgs;
            displayArgs = DisplayMaximized ? displayArgs += " -m" : displayArgs;
            return displayArgs;
        }

        [DllImportAttribute("User32.dll")]
        private static extern System.IntPtr SetForegroundWindow(int hWnd);
        private void Run(int vpHwnd, ProcessStartInfo startInfo)
        {
            this.vpHwnd = vpHwnd;
            //send switches
            ControllerRunning = true;
            ConnectOsc();

            displayProcess = new Process();
            displayProcess.StartInfo = startInfo;
            displayProcess.Start();
        }
        private static object[,] ToObjectArray(List<KeyValuePair<int, int>> changed)
        {
            object[,] arr = new object[changed.Count, 2];
            int ii = 0;
            foreach (var coil in changed)
            {
                arr[ii, 0] = coil.Key; arr[ii, 1] = coil.Value;
                ii++;
            }

            return arr;
        }
        #endregion
    }
}