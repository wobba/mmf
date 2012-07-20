using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace mAdcOW.Serializer
{
    public abstract class SerializeDeserializeAbstractBase<T>: ISerializeDeserialize<T>
    {

        bool _useClonedType = false;
        Mapper<T> _mapper = null;



        public virtual byte[] ObjectToBytes(T data)
        {
            if (_useClonedType)
            {
                IMappedType clonedObj = _mapper.MapFromInstance(data);
                MethodInfo method = this.GetType().GetMethod("SerializeObjectToBytes");
                method = method.MakeGenericMethod(new Type[] { Mapper.GetMappedType() });
                return (byte[])method.Invoke(this,new object[]{clonedObj});
            }

            return SerializeObjectToBytes<T>(data);


        }

        public virtual T BytesToObject(byte[] bytes)
        {
            if (_useClonedType)
            {
                MethodInfo method = this.GetType().GetMethod("DeSerializeBytesToObject");
                method = method.MakeGenericMethod(new Type[] { Mapper.GetMappedType() });
                return (T)method.Invoke(this, new object[] { bytes });
            }
            return DeSerializeBytesToObject<T>(bytes);
        }


        public virtual bool CanSerializeType()
        {
            if (CanDoSerializeType<T>())
            {
                return true;
            }
            else
            {
                if (!typeof(T).IsPrimitive)
                {
                    MethodInfo method = typeof(SerializeDeserializeAbstractBase<T>).GetMethod("CanDoSerializeType");
                    method = method.MakeGenericMethod(new Type[] { Mapper.GetMappedType() });
                    _useClonedType = (bool)method.Invoke(this, null);
                }
                return _useClonedType;
            }
           
        }

      
        //U can be of Type T or of IMappedType<T>
        public abstract byte[] SerializeObjectToBytes<U>(U data);
       
        //U can be of Type T or of IMappedType<T>
        public abstract U DeSerializeBytesToObject<U>(byte[] bytes);
       
        protected bool UseClonedType
        {
            get { return _useClonedType; }
            set { _useClonedType = value; }
        }


        protected Mapper<T> Mapper
        {
            get
            {
                if (_mapper == null)
                {
                    _mapper = new Mapper<T>();
                }
                return _mapper;
            }
        }
            


        public bool CanDoSerializeType<U>()
        {
            try
            {
                object[] args = null;
                if (typeof(U) == typeof(string))
                {
                    args = new object[] { new[] { 'T', 'e', 's', 't', 'T', 'e', 's', 't', 'T', 'e', 's', 't' } };
                }
                U classInstance = (U)Activator.CreateInstance(typeof(U), args);
                byte[] bytes = SerializeObjectToBytes<U>(classInstance);
                if (bytes.Length == 0) return false;
                DeSerializeBytesToObject<U>(bytes);
            }
            catch (Exception ex)
            {  
                    return false;
            }
            return true;
        }
    }
}
