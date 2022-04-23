using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IL2CppJson = Il2CppNewtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnhollowerBaseLib;
using Assets.Scripts.PeroTools.Nice.Interface;
using NLayer;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using NVorbis.NAudioSupport;

namespace CustomAlbums
{
    public static class Utils
    {
        private static readonly Logger Log = new Logger("Utils");

        unsafe public static IntPtr NativeMethod(Type type, string name, Type[] parameters = null, Type[] generics = null)
        {
            var method = AccessTools.Method(type, name, parameters, generics);

            var methodPtr = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method)
                .GetValue(null);
            return methodPtr;
        }
        /// <summary>
        /// Read embedded file from this assembly.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static byte[] ReadEmbeddedFile(string file)
        {
            var assembly = Assembly.GetExecutingAssembly();
            byte[] buffer;
            using (var stream = assembly.GetManifestResourceStream($"{Assembly.GetExecutingAssembly().GetName().Name}.{file}"))
            {
                buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
            }
            return buffer;
        }
        /// <summary>
        /// Load json from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="steamReader"></param>
        /// <returns></returns>
        public static T IL2CppJsonDeserialize<T>(this Stream steamReader)
        {
            var buffer = new byte[steamReader.Length];
            steamReader.Read(buffer, 0, buffer.Length);
            return IL2CppJson.JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(buffer));
        }
        /// <summary>
        /// Load json from string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <returns></returns>
        public static T IL2CppJsonDeserialize<T>(this string text)
        {
            return IL2CppJson.JsonConvert.DeserializeObject<T>(text);
        }
        public static JObject ToJObject(this Il2CppSystem.Object o)
        {
            JToken token;
            Newtonsoft.Json.JsonSerializer jsonSerializer = Newtonsoft.Json.JsonSerializer.CreateDefault();
            JTokenWriter jtokenWriter = new JTokenWriter();
            jsonSerializer.Serialize(jtokenWriter, o);
            token = jtokenWriter.Token;
            return (JObject)token;
        }
        /// <summary>
        /// Convert a il2cpp object to json string.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string IL2CppJsonSerialize(this Il2CppSystem.Object obj)
        {
            return IL2CppJson.JsonConvert.SerializeObject(obj);
        }
        /// <summary>
        /// Load json from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="steamReader"></param>
        /// <returns></returns>
        public static T JsonDeserialize<T>(this Stream steamReader)
        {
            var buffer = new byte[steamReader.Length];
            steamReader.Read(buffer, 0, buffer.Length);
            return JsonConvert.DeserializeObject<T>(Encoding.Default.GetString(buffer));
        }
        /// <summary>
        /// Load json from string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text"></param>
        /// <returns></returns>
        public static T JsonDeserialize<T>(this string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }
        /// <summary>
        /// Convert a object to json string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string JsonSerialize(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
        /// <summary>
        /// Get the specified non-public type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type GetNestedNonPublicType(this Type type, string name)
        {
            return type.GetNestedType(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }
        public static string RemoveFromEnd(this string str, IEnumerable<string> suffixes)
        {
            foreach (var suffix in suffixes)
            {
                if (str.EndsWith(suffix))
                {
                    return str.Substring(0, str.Length - suffix.Length);
                }
            }
            return str;
        }
        public static string RemoveFromEnd(this string str, string suffix)
        {
            if (str.EndsWith(suffix))
            {
                return str.Substring(0, str.Length - suffix.Length);
            }
            return str;
        }
        public static string RemoveFromStart(this string str, IEnumerable<string> suffixes)
        {
            foreach (var suffix in suffixes)
            {
                if (str.StartsWith(suffix))
                {
                    return str.Substring(suffix.Length);
                }
            }
            return str;
        }
        public static string RemoveFromStart(this string str, string suffix)
        {
            if (str.StartsWith(suffix))
            {
                return str.Substring(suffix.Length);
            }
            return str;
        }
        /// <summary>
        /// Read all bytes from a Stream.
        /// </summary>
        /// <param name="steamReader"></param>
        /// <returns></returns>
        public static byte[] ToArray(this Stream steamReader)
        {
            var buffer = new byte[steamReader.Length];
            steamReader.Read(buffer, 0, buffer.Length);
            return buffer;
        }
        /// <summary>
        /// Put all bytes into Stream.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        unsafe public static Il2CppSystem.IO.MemoryStream ToIL2CppStream(this byte[] bytes)
        {
            var array = new Il2CppStructArray<byte>(bytes.Length);
            for(var i=0;i<bytes.Length;i++)
            {
                array[i] = bytes[i];
            }
            return new Il2CppSystem.IO.MemoryStream(array);
        }
        public static System.IO.MemoryStream ToStream(this byte[] bytes)
        {
            return new System.IO.MemoryStream(bytes);
        }
        public static string ToString(this IEnumerable<byte> bytes, string format)
        {
            string result = string.Empty;
            foreach (var _byte in bytes)
            {
                result += _byte.ToString(format);
            }
            return result;
        }
        public static byte[] GetMD5(this IEnumerable<byte> bytes)
        {
            return MD5.Create().ComputeHash(bytes.ToArray());
        }
        /// <summary>
        /// Search all implementation classes of interfce
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllImplement(this Type type)
        {
            var classes = Assembly.GetAssembly(type).GetTypes().Where(t =>
                t.GetInterfaces().Contains(type) && t.GetConstructor(Type.EmptyTypes) != null);

            return classes;
        }
        public static IEnumerable<Type> GetAllSublass(this Type type)
        {
            var classes = Assembly.GetAssembly(type).GetTypes().Where(
                t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(type));

            return classes;
        }

        /// <summary>
        /// Gets the result of an IVariable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T GetResult<T>(this IVariable data) {
            try {
                return VariableUtils.GetResult<T>(data);
            } catch(Exception e) {
                Log.Error(typeof(T).ToString() + " " + e.ToString());
                return default(T);
            }
        }

        /// <summary>
        /// Sets the result of an IVariable.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="value"></param>
        public static void SetResult(this IVariable data, Il2CppSystem.Object value) {
            VariableUtils.SetResult(data, value);
        }
    }
}
