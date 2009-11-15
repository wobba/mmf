using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace mAdcOW.DiskStructures.Serializer
{
    /// <summary>Provides methods for dynamic type loading.</summary>
    public static class TypeLoader
    {
        /// <summary>Instantiates an instance of the specified type.</summary>
        /// <param name="typeName">The name of the System.Type.AssemblyQualifiedName to instantiate.</param>
        /// <returns>An instance of the specified type.</returns>
        /// <exception cref="ConfigurationException">Thrown if the type can't be resolved.</exception>
        /// <typeparam name="T"></typeparam>
        public static T Load<T>(string typeName)
        {
            return Load<T>(typeName, null);
        }

        /// <summary>Instantiates an instance of the specified type.</summary>
        /// <param name="typeName">The name of the System.Type.AssemblyQualifiedName to instantiate.</param>
        /// <returns>An instance of the specified type.</returns>
        /// <exception cref="ConfigurationException">Thrown if the type can't be resolved.</exception>
        /// <param name="args">An array of arguments that match in number, order and type the parameters of the constructor to invoke.</param>
        /// <typeparam name="T"></typeparam>
        public static T Load<T>(string typeName, params object[] args)
        {
            Type type = Type.GetType(typeName);

            if (type == null)
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                                                          "Type '{0}' could not be resolved. Please ensure the type definition is correct.",
                                                          typeName), "typeName");
            }

            return (T)Activator.CreateInstance(type, args);
        }

        /// <summary>Instantiates an instance of the specified type.</summary>
        /// <param name="xmlReader">The reader from which to read xml data.</param>
        /// <returns>An instance of the specified type.</returns>
        /// <exception cref="ConfigurationException">Thrown if the type can't be resolved.</exception>
        /// <typeparam name="T"></typeparam>
        public static T LoadFromXml<T>(XmlReader xmlReader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(xmlReader);
        }

        /// <summary>Instantiates an instance of the specified type.</summary>
        /// <param name="xmlNode">The node containing the object to instantiate.</param>
        /// <returns>An instance of the specified type.</returns>
        /// <exception cref="ConfigurationException">Thrown if the type can't be resolved.</exception>
        /// <typeparam name="T"></typeparam>
        public static T LoadFromXml<T>(XmlNode xmlNode)
        {
            using (XmlReader xmlReader = new XmlNodeReader(xmlNode))
            {
                return LoadFromXml<T>(xmlReader);
            }
        }

        /// <summary>Instantiates an instance of the specified type, from an xml file</summary>
        /// <param name="filePath">Path to an xml file containing instance data.</param>
        /// <returns>An instance of the specified type.</returns>
        public static T LoadFromXmlFile<T>(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                T instance = (T)serializer.Deserialize(fs);
                return instance;
            }
        }
    }
}