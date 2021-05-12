using System;
using System.Runtime.InteropServices;

namespace PinGod.VP
{
    [ComVisible(true)]
    [Guid(ContractGuids.ControllerInterface)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IController
    {
        #region Display Properties
        bool DisplayFullScreen { get; set; }
        int DisplayWidth { get; set; }
        int DisplayHeight { get; set; }
        int DisplayX { get; set; }
        int DisplayY { get; set; }
        bool DisplayAlwaysOnTop { get; set; }
        bool DisplayLowDpi { get; set; }
        bool DisplayNoWindow { get; set; }
        #endregion

        bool GameRunning { get; set; }
        
        /// <summary>
        /// Set the array size of coils to check for
        /// </summary>
        byte CoilCount { get; set; }
        /// <summary>
        /// Set the array size of lamps to check for
        /// </summary>
        byte LampCount { get; set; }

        int SendPort { get; set; }
        int ReceivePort { get; set; }

        /// <summary>
        /// Get changed coils / solenoids receive
        /// </summary>
        /// <returns>object[i,2] Id, State</returns>
        [ComVisible(true)]
        dynamic ChangedSolenoids();

        /// <summary>
        /// Get changed lamps
        /// </summary>
        /// <returns>object[i,2] Id, State</returns>
        [ComVisible(true)]
        dynamic ChangedLamps();

        /// <summary>
        /// Runs a packaged game, no debug (if set by developer on export)
        /// </summary>
        /// <param name="vpHwnd"></param>
        /// <param name="game"></param>
        [ComVisible(true)]
        void Run(int vpHwnd, string game);

        /// <summary>
        /// Runs the game from it's source directory. Godot needs to be in environment
        /// </summary>
        /// <param name="vpHwnd"></param>
        /// <param name="game"></param>
        [ComVisible(true)]
        void RunDebug(int vpHwnd, string game);

        /// <summary>
        /// Sends an action to the display. InputMap in Godot
        /// </summary>
        /// <param name="action"></param>
        /// <param name="pressed"></param>
        [ComVisible(true)]
        void SetAction(string action, int pressed);

        /// <summary>
        /// Stops and kills the display
        /// </summary>
        [ComVisible(true)]
        void Stop();

        /// <summary>
        /// Sends a switch event /sw to be picked up by InputMap in Godot
        /// </summary>
        /// <param name="swNum"></param>
        /// <param name="state"></param>
        [ComVisible(true)]
        void Switch(int swNum, int state);

        /// <summary>
        /// TODO: Sometimes on pause it won't resume from that state
        /// </summary>
        /// <param name="paused"></param>
        [ComVisible(true)]
        void Pause(int paused);
    }
}
