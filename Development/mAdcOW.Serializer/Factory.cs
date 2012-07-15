using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace mAdcOW.Serializer
{
    public class Factory<T>
    {
        static HashSet<Type> _compiledUnsafeSerializer = new HashSet<Type>();

        private static readonly Dictionary<Type, ISerializeDeserialize<T>> _dictionaryCache =
            new Dictionary<Type, ISerializeDeserialize<T>>();

       
        public ISerializeDeserialize<T> GetSerializer()
        {
            ISerializeDeserialize<T> result;
            Type objectType = typeof(T);
            if (!_dictionaryCache.TryGetValue(objectType, out result))
            {
                _dictionaryCache[objectType] = result = PickOptimalSerializer();
            }
            Trace.WriteLine(string.Format("{0} uses {1}", typeof(T), result.GetType()));
            return result;
        }

        public ISerializeDeserialize<T> GetSerializer(string name)
        {
            return (from pair in _dictionaryCache
                    where pair.Value.GetType().AssemblyQualifiedName == name
                    select pair.Value).FirstOrDefault();
        }


        private ISerializeDeserialize<T> PickOptimalSerializer()
        {
            CompileAndRegisterUnsafeSerializer();

            List<Type> listOfSerializers = GetListOfGenericSerializers();
            listOfSerializers.AddRange(GetListOfImplementedSerializers());

            var benchmarkTimes = BenchmarkSerializers(listOfSerializers);
            if (benchmarkTimes.Count == 0)
            {  
               throw new SerializerException("No serializer available for the type");
            }
            return benchmarkTimes.Last().Value;
        }

        public List<ISerializeDeserialize<T>> GetValidSerializers()
        {
            CompileAndRegisterUnsafeSerializer();

            List<Type> listOfSerializers = GetListOfGenericSerializers();
            listOfSerializers.AddRange(GetListOfImplementedSerializers());

            var benchmarkTimes = BenchmarkSerializers(listOfSerializers);
            if (benchmarkTimes.Count == 0) throw new SerializerException("No serializer available for the type");

            return benchmarkTimes.Values.ToList();
        }

        private List<Type> GetListOfGenericSerializers()
        {
            Type interfaceGenricType = typeof(ISerializeDeserialize<T>);
            var serializers = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                              from genericType in assembly.GetTypes()
                              from interfaceType in genericType.GetInterfaces()
                                  .Where(
                                  iType =>
                                  (iType.Name == interfaceGenricType.Name && 
                                   genericType.IsGenericTypeDefinition &&
                                   //Added check on Abstract classes for ChangedID#1
                                   !genericType.IsAbstract
                                  ))
                              select genericType;
            return serializers.ToList();
        }

        private List<Type> GetListOfImplementedSerializers()
        {
            Type interfaceGenricType = typeof(ISerializeDeserialize<T>);
            var serializers = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                              from implementedType in assembly.GetTypes()
                              from interfaceType in implementedType.GetInterfaces()
                                  .Where(iType => iType == interfaceGenricType &&
                                        //Added check on Abstract classes for ChangedID#1
                                        !implementedType.IsAbstract)
                              select implementedType;
            return serializers.ToList();
        }


        private SortedDictionary<int, ISerializeDeserialize<T>> BenchmarkSerializers(IEnumerable<Type> listOfSerializers)
        {
            var benchmarkTimes = new SortedDictionary<int, ISerializeDeserialize<T>>();
            foreach (Type type in listOfSerializers)
            {
                ISerializeDeserialize<T> serializer = InstantiateSerializer(type);
                if (!serializer.CanSerializeType()) continue;
                int count = BenchMarkSerializer(serializer);
                if (count > 0) benchmarkTimes.Add(count, serializer);
            }

            foreach (var valuePair in benchmarkTimes)
                Trace.WriteLine(string.Format("{0} : {1}", valuePair.Key, valuePair.Value.GetType()));

            return benchmarkTimes;
        }

        private ISerializeDeserialize<T> InstantiateSerializer(Type type)
        {
            Type instType = type.IsGenericTypeDefinition ? type.MakeGenericType(typeof(T)) : type;
            return (ISerializeDeserialize<T>)Activator.CreateInstance(instType);
        }

        private void CompileAndRegisterUnsafeSerializer()
        {
            try
            {
                if (_compiledUnsafeSerializer.Contains(typeof(T))) return;
                CreateUnsafeSerializer<T> createUnsafeSerializer = new CreateUnsafeSerializer<T>();
                createUnsafeSerializer.GetSerializer();
                _compiledUnsafeSerializer.Add(typeof(T));
            }
            catch (SerializerException)
            {
                // ignore errors
            }
        }

        private int BenchMarkSerializer(ISerializeDeserialize<T> serDeser)
        {
            object[] args = null;
            if (typeof(T) == typeof(string))
            {
                args = new object[] { new[] { 'T', 'e', 's', 't', 'T', 'e', 's', 't', 'T', 'e', 's', 't' } };
            }
            else if (typeof(T) == typeof(byte[]))
            {
                byte[] test = new byte[100];
                T classInstance = (T)(object)test;
                Stopwatch sw = Stopwatch.StartNew();
                int count = 0;
                while (sw.ElapsedMilliseconds < 500)
                {
                    byte[] bytes = serDeser.ObjectToBytes(classInstance);
                    serDeser.BytesToObject(bytes);
                    count++;
                }
                sw.Stop();
                return count;
            }
            try
            {
                T classInstance = (T)Activator.CreateInstance(typeof(T), args);
                Stopwatch sw = Stopwatch.StartNew();
                int count = 0;
                while (sw.ElapsedMilliseconds < 500)
                {
                    byte[] bytes = serDeser.ObjectToBytes(classInstance);
                    serDeser.BytesToObject(bytes);
                    count++;
                }
                sw.Stop();
                return count;
            }
            catch (MissingMethodException e)
            {
                // Missing default constructor
                return 0;
            }
        }
    }
}