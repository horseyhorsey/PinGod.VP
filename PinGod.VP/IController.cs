using System;
using System.Runtime.InteropServices;

namespace PinGod.VP.Domain
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

        #region Machine Items
        byte CoilCount { get; set; }
        byte LampCount { get; set; }
        byte LedCount { get; set; }
        byte SwitchCount { get; set; }
        #endregion

        bool GameRunning { get; set; }       

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
        /// Get changed leds
        /// </summary>
        /// <returns>object[i,3] Id, State, colour (ole)</returns>
        [ComVisible(true)]
        dynamic ChangedPDLeds();

        /// <summary>
        /// Used by the implementing class. Here for testing creating maps without display
        /// </summary>
        /// <param name="size"></param>
        [ComVisible(true)]
        void CreateMemoryMap(long size = 2048);

        /// <summary>
        /// Pause the display
        /// </summary>
        /// <param name="paused"></param>
        [ComVisible(true)]
        void Pause(int paused);

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
        /// Stops and kills the display
        /// </summary>
        [ComVisible(true)]
        void Stop();

        /// <summary>
        /// Writes a switch event /sw to be picked up by InputMap in Godot
        /// </summary>
        /// <param name="swNum"></param>
        /// <param name="state"></param>
        [ComVisible(true)]
        void Switch(int swNum, int state);
    }
}
