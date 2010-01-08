using System;
using System.Collections.Generic;
using System.Text;

namespace AltSerialize
{
    public class AltFormatter : System.Runtime.Serialization.IFormatterConverter
    {
        #region IFormatterConverter Members

        public object Convert(object value, TypeCode typeCode)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public object Convert(object value, Type type)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public bool ToBoolean(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public byte ToByte(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public char ToChar(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public DateTime ToDateTime(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public decimal ToDecimal(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public double ToDouble(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public short ToInt16(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int ToInt32(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public long ToInt64(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public sbyte ToSByte(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public float ToSingle(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public string ToString(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ushort ToUInt16(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public uint ToUInt32(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public ulong ToUInt64(object value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
