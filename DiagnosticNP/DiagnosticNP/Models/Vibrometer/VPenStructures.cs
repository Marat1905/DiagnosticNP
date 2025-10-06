using System.Runtime.InteropServices;

namespace DiagnosticNP.Models.Vibrometer
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stUser_DataViPen
    {
        public byte Addr;
        public ushort ID;
        public uint Timestamp;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public short[] Values;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stVPenHeader
    {
        public byte ViPen_Get_Data_Command;
        public byte ViPen_Get_Data_Block;
        public byte ViPen_Get_Wave_ID;
        public byte Reserv1;
        public uint Timestamp;
        public float Coeff;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 138 / 2)]
        private ushort[] Reserv2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stVPenBlock
    {
        public byte ViPen_Get_Data_Block;
        public byte ViPen_Get_Wave_ID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 74)]
        public short[] Data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stVPenData
    {
        public stVPenHeader Header;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public stVPenBlock[] Blocks;
    }
}