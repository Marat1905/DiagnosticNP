using System;
using System.Runtime.InteropServices;

namespace DiagnosticNP.Models.Vibrometer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ViPenAdvertising
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        private byte[] R0;

        public Int16 Velocity;
        public Int16 Acceleration;
        public Int16 Kurtosis;
        public Int16 Temperature;
    }
}