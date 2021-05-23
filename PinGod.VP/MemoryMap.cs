using System;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace PinGod.VP
{
    public class MemoryMap : IDisposable
    {
        const int MAP_SIZE = 2048;
        const string MAP_NAME = "pingod_vp";        
        const string MUTEX_NAME = "pingod_vp_mutex";

        private Mutex mutex;
        private MemoryMappedFile mmf;
        internal MemoryMappedViewAccessor coilsMap;
        private MemoryMappedViewAccessor lampsMap;
        private MemoryMappedViewAccessor ledsMap;

        byte[] _coilStates;
        byte[] _lampStates;
        int[] _ledStates;

        private bool mutexCreated = false;
        public void CreateMemoryMap(long size = MAP_SIZE, int coils = 32, int lamps = 64, int leds = 64)
        {
            if (mutex == null)
            {
                var mutexCreated = Mutex.TryOpenExisting(MUTEX_NAME, out mutex);
                if (!mutexCreated)
                {
                    mutex = new Mutex(true, MUTEX_NAME, out mutexCreated);
                }

                _coilStates = new byte[coils * 2];
                _lampStates = new byte[lamps * 2];
                _ledStates = new int[leds * 3];

                //Create a memory mapped file - windows. Create a mapping for each game item type
                mmf = MemoryMappedFile.CreateOrOpen(MAP_NAME, size, MemoryMappedFileAccess.ReadWrite);
                int offset = 0;
                coilsMap = mmf.CreateViewAccessor(0, _coilStates.Length, MemoryMappedFileAccess.ReadWrite);
                offset += _coilStates.Length;
                lampsMap = mmf.CreateViewAccessor(offset, _lampStates.Length, MemoryMappedFileAccess.ReadWrite);
                offset += _lampStates.Length;
                ledsMap = mmf.CreateViewAccessor(offset, sizeof(int) * _ledStates.Length, MemoryMappedFileAccess.ReadWrite);
            }
        }

        internal byte[] GetCoilStates() 
        {
            coilsMap.ReadArray(0, _coilStates, 0, _coilStates.Length);
            return _coilStates;
        }
        internal byte[] GetLampStates() 
        {
            lampsMap.ReadArray(0, _lampStates, 0, _lampStates.Length);
            return _lampStates;
        }
        internal int[] GetLedStates() 
        {
            ledsMap.ReadArray(0, _ledStates, 0, _ledStates.Length);
            return _ledStates;
        }

        public void Dispose() => Dispose(true);
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                mmf?.Dispose();
                coilsMap?.Dispose();
                lampsMap?.Dispose();
                ledsMap?.Dispose();
                if (mutexCreated)
                    mutex?.ReleaseMutex();
            }
            GC.SuppressFinalize(this);
        }
    }
}
