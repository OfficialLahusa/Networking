using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Client
{
    public static class Serialiser
    {
        private const string configFilePath = "config.yaml";

        private static Serializer serializer;
        private static Deserializer deserializer;
        static Serialiser()
        {
            serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build() as Serializer;
            deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build() as Deserializer;

            if(!File.Exists(configFilePath))
            {
                CreateConfigFile();
            }
        }

        public static ConfigFile LoadConfigFile()
        {
            string yaml = File.ReadAllText(configFilePath);
            ConfigFile config = deserializer.Deserialize(yaml, typeof(ConfigFile)) as ConfigFile;

            return config;
        }

        public static void SaveConfigFile(ConfigFile configFile)
        {
            string yaml = serializer.Serialize(configFile);
            FileStream fileStream = File.Create(configFilePath);
            byte[] bytes = UnicodeEncoding.UTF8.GetBytes(yaml);
            fileStream.Write(bytes);
            fileStream.Close();
        }

        private static void CreateConfigFile()
        {
            SaveConfigFile(new ConfigFile());

            Console.WriteLine("Created default config file at \"" + configFilePath + "\"");
        }
    }
}
