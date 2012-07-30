using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace mAdcOW.Serializer
{
    public class Factory<T>
    {
        static readonly HashSet<Type> _compiledUnsafeSerializer = new HashSet<Type>();
        private static readonly Dictionary<Type, ISerializeDeserialize<T>> DictionaryCache = new Dictionary<Type, ISerializeDeserialize<T>>();

        public ISerializeDeserialize<T> GetSerializer()
        {
            ISerializeDeserialize<T> result;
            Type objectType = typeof(T);
            if (!DictionaryCache.TryGetValue(objectType, out result))
            {
                DictionaryCache[objectType] = result = PickOptimalSerializer();
            }
            Trace.WriteLine(string.Format("{0} uses {1}", typeof(T), result.GetType()));
            return result;
        }

        public ISerializeDeserialize<T> GetSerializer(string name)
        {
            return (from pair in DictionaryCache
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

            List<Type> serializers = new List<Type>(10);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = from genericType in assembly.GetTypes()
                                from interfaceType in genericType.GetInterfaces()
                                    .Where(iType => (iType.Name == interfaceGenricType.Name &&
                                     genericType.IsGenericTypeDefinition && !genericType.IsAbstract
                                    ))
                                select genericType;
                    serializers.AddRange(types);
                }
                catch (Exception)
                {
                }
            }
            return serializers;
        }

        private List<Type> GetListOfImplementedSerializers()
        {
            Type interfaceGenricType = typeof(ISerializeDeserialize<T>);

            List<Type> serializers = new List<Type>(10);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = from implementedType in assembly.GetTypes()
                                from interfaceType in implementedType.GetInterfaces()
                                    .Where(iType => iType == interfaceGenricType && !implementedType.IsAbstract)
                                select implementedType;
                    serializers.AddRange(types);
                }
                catch (Exception)
                {
                }
            }

            return serializers;
        }

        private SortedDictionary<int, ISerializeDeserialize<T>> BenchmarkSerializers(IEnumerable<Type> listOfSerializers)
        {
            var benchmarkTimes = new SortedDictionary<int, ISerializeDeserialize<T>>();
            foreach (Type type in listOfSerializers)
            {
                try
                {
                    ISerializeDeserialize<T> serializer = InstantiateSerializer(type);
                    if (!serializer.CanSerializeType()) continue;
                    int count = BenchMarkSerializer(serializer);
                    if (count > 0) benchmarkTimes.Add(count, serializer);
                }
                catch (Exception)
                {
                }
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
                DataHelper.AssignEmptyData(ref classInstance);
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
            catch (MissingMethodException)
            {
                // Missing default constructor
                return 0;
            }
        }
    }
}