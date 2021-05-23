using Newtonsoft.Json;
using Rug.Osc.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PinGod.VP
{
    [ComVisible(true)]
    [GuidAttribute(ContractGuids.ControllerClass)]
    public class Controller : IController, IDisposable
    {
        /// <summary>
        /// Sends switch events and other actions
        /// </summary>
        private OscSender sender;
        private OscReceiver receiver;
        private Process displayProcess;
        private MemoryMap _memoryMap;

        byte[] _lastCoilStates;       
        byte[] _lastLampStates;       
        int[] _lastLedStates;

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
        public byte CoilCount { get; set; } = 32;
        public byte LampCount { get; set; } = 64;
        public byte LedCount { get; set; } = 64;

        int vpHwnd;

        public Controller()
        {
            _memoryMap = new MemoryMap();            
        }

        public void CreateMemoryMap(long size = 2048)
        {
            _memoryMap.CreateMemoryMap(size, coils: CoilCount, lamps: LampCount, leds: LedCount);
        }

        /// <summary>
        /// Gets changed from memory map
        /// </summary>
        /// <returns>object[,2]</returns>
        public dynamic ChangedLamps()
        {
            var _lampStates = _memoryMap.GetLampStates();
            var arr = new int[0, 2];
            if (_lastLampStates == null)
            {
                _lastLampStates = new byte[_lampStates.Length];
                _lampStates.CopyTo(_lastLampStates, 0);
                return arr;
            }
            if (_lampStates != _lastLampStates)
            {
                //collect the changed coils
                List<int> chgd = new List<int>();
                for (int i = 0; i < _lampStates.Length; i += 2)
                {
                    if (_lampStates[i + 1] != _lastLampStates[i + 1])
                    {
                        chgd.Add(_lampStates[i]);
                        chgd.Add(_lampStates[i + 1]);
                    }
                }

                //create int[,] to send back (VP)
                arr = new int[chgd.Count / 2, 2];
                //Array.Copy(chgd.ToArray(), arr, chgd.Count); //Cant array copy multi dimension?
                Buffer.BlockCopy(chgd.ToArray(), 0, arr, 0, chgd.Count);
                _lampStates.CopyTo(_lastLampStates, 0);
                return arr;
            }
            else
            {
                return arr;
            }
        }

        /// <summary>
        /// Gets changed from memory map
        /// </summary>
        /// <returns>object[,3]</returns>
        public dynamic ChangedPDLeds()
        {
            var _ledStates = _memoryMap.GetLedStates();
            var arr = new object[0, 3];
            if (_lastLedStates == null)
            {
                _lastLedStates = new int[_ledStates.Length];
                _ledStates.CopyTo(_lastLedStates, 0);
                return null;
            }
            if (_ledStates != _lastLedStates)
            {
                //collect the changed coils
                List<int> chgd = new List<int>();
                for (int i = 0; i < _ledStates.Length; i+=3)
                {
                    //check for state and colour
                    if (_ledStates[i + 1] != _lastLedStates[i + 1] || _ledStates[i + 2] != _lastLedStates[i + 2])
                    {
                        chgd.Add(_ledStates[i]);
                        chgd.Add(_ledStates[i+1]);
                        chgd.Add(_ledStates[i+2]);
                    }
                }

                if (chgd.Count <= 0)
                    return null;

                //create int[,] to send back (VP)
                //Array.Copy(chgd.ToArray(), arr, chgd.Count); //Cant array copy multi dimension?

                //arr = new int[chgd.Count/3, 3];                
                //Buffer.BlockCopy(chgd.ToArray(), 0, arr, 0, chgd.Count);

                //have to convert the object array for VP, PITA
                int c = 0;
                arr = new object[chgd.Count / 3, 3];
                for (int ii = 0; ii < chgd.Count; ii +=3)
                {
                    arr[c, 0] = chgd[ii];
                    arr[c, 1] = chgd[ii + 1];
                    arr[c, 2] = chgd[ii + 2];
                    c++;
                }

                _ledStates.CopyTo(_lastLedStates, 0);
                return arr;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets changed from memory map
        /// </summary>
        /// <returns>object[,2]</returns>
        public dynamic ChangedSolenoids()
        {
            var _coilStates = _memoryMap.GetCoilStates();
            var arr = new object[0, 2];
            if (_lastCoilStates == null)
            {
                _lastCoilStates = new byte[_coilStates.Length];
                _coilStates.CopyTo(_lastCoilStates, 0);
                return arr;
            }
            if (_coilStates != _lastCoilStates)
            {                
                //collect the changed coils
                List<byte> chgd = new List<byte>();                
                for (int i = 0; i < _coilStates.Length; i+=2)
                {
                    if (_coilStates[i+1] != _lastCoilStates[i+1])
                    {
                        chgd.Add(_coilStates[i]); 
                        chgd.Add(_coilStates[i+1]);
                    }
                }

                //have to convert the object array for VP, PITA
                int c = 0;
                arr = new object[chgd.Count/2, 2];                
                for (int ii = 0; ii < chgd.Count; ii+=2)
                {
                    arr[c, 0] = chgd[ii];
                    arr[c, 1] = chgd[ii+1];
                    c++;
                }
                //Array.Copy(chgd.ToArray(), arr, chgd.Count); //Cant array copy multi dimension?
                //Buffer.BlockCopy(chgd.ToArray(), 0, arr, 0, chgd.Count);
                _coilStates.CopyTo(_lastCoilStates, 0);
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

            //receive /evt messages from window
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
            sender?.Send(new OscMessage("/action", action, pressed));
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

            this.Dispose();
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

            CreateMemoryMap();
            ConnectOsc();            

            //send switches
            ControllerRunning = true;

            displayProcess = new Process();
            displayProcess.StartInfo = startInfo;
            displayProcess.Start();
        }

        bool isDisposing = false;        
        public void Dispose()
        {
            if (isDisposing) return;
            isDisposing = true;
            _memoryMap.Dispose();

        }
        #endregion
    }
}