using System;

namespace mAdcOW.DataStructures.DictionaryBacking
{
    internal class VariableLong
    {
        /// <summary>Writes an long in a variable-length format.  Writes between one and five
        /// bytes.  Smaller values take fewer bytes.  Negative numbers are not
        /// supported.
        /// </summary>
        public static byte[] GetVLongBytes(long value)
        {
            byte[] buffer = new byte[9];
            byte count = 0;
            while ((value & ~0x7F) != 0)
            {
                buffer[count] = (byte) ((value & 0x7f) | 0x80);
                //Write((byte)((value & 0x7f) | 0x80));
                value = value >> 7;
                count++;
            }
            //Write((byte)value);
            buffer[count] = (byte) value;
            byte[] result = new byte[count];
            Array.Copy(buffer, 0, result, 0, count);
            return result;
        }

        /// <summary>Reads a long stored in variable-length format.  Reads between one and
        /// nine bytes.  Smaller values take fewer bytes.  Negative numbers are not
        /// supported. 
        /// </summary>
        public static long GetVLong(byte[] buffer)
        {
            byte count = 0;
            //byte b = ReadByte();
            byte b = buffer[0];
            count++;
            long i = b & 0x7F;
            for (int shift = 7; (b & 0x80) != 0; shift += 7)
            {
                //b = ReadByte();
                b = buffer[count];
                i |= (b & 0x7FL) << shift;
                count++;
            }
            return i;
        }
    }
}