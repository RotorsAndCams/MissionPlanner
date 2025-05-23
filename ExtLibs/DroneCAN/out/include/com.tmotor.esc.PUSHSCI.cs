using uint8_t = System.Byte;
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;
using uint64_t = System.UInt64;

using int8_t = System.SByte;
using int16_t = System.Int16;
using int32_t = System.Int32;
using int64_t = System.Int64;

using float32 = System.Single;

using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DroneCAN
{
    public partial class DroneCAN 
    {
        public partial class com_tmotor_esc_PUSHSCI: IDroneCANSerialize 
        {
            public const int COM_TMOTOR_ESC_PUSHSCI_MAX_PACK_SIZE = 260;
            public const ulong COM_TMOTOR_ESC_PUSHSCI_DT_SIG = 0xCE2B6D6B6BDC0AE8;
            public const int COM_TMOTOR_ESC_PUSHSCI_DT_ID = 1038;

            public uint32_t data_sequence = new uint32_t();
            public uint8_t data_len; [MarshalAs(UnmanagedType.ByValArray,SizeConst=255)] public uint8_t[] data = Enumerable.Range(1, 255).Select(i => new uint8_t()).ToArray();

            public void encode(dronecan_serializer_chunk_cb_ptr_t chunk_cb, object ctx, bool fdcan = false)
            {
                encode_com_tmotor_esc_PUSHSCI(this, chunk_cb, ctx, fdcan);
            }

            public void decode(CanardRxTransfer transfer, bool fdcan = false)
            {
                decode_com_tmotor_esc_PUSHSCI(transfer, this, fdcan);
            }

            public static com_tmotor_esc_PUSHSCI ByteArrayToDroneCANMsg(byte[] transfer, int startoffset, bool fdcan = false)
            {
                var ans = new com_tmotor_esc_PUSHSCI();
                ans.decode(new DroneCAN.CanardRxTransfer(transfer.Skip(startoffset).ToArray()), fdcan);
                return ans;
            }
        }
    }
}