﻿using ModHelper;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CustomAlbums
{
    public static class Utils
    {
        /// <summary>
        /// Read embedded file from this dll
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
        public static T JsonDeserialize<T>(this Stream steamReader)
        {
            var buffer = new byte[steamReader.Length];
            steamReader.Read(buffer, 0, buffer.Length);
            return JsonConvert.DeserializeObject<T>(Encoding.Default.GetString(buffer));
        }
        public static T JsonDeserialize<T>(this string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }
        public static string JsonSerialize(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }
        public static Type GetNestedNonPublicType(this Type type, string name)
        {
            return type.GetNestedType(name, BindingFlags.NonPublic | BindingFlags.Instance);
        }
        public static string RemoveFromEnd(this string str, IEnumerable<string> suffixes)
        {
            foreach(var suffix in suffixes)
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
        public static byte[] ToArray(this Stream steamReader)
        {
            var buffer = new byte[steamReader.Length];
            steamReader.Read(buffer, 0, buffer.Length);
            return buffer;
        }
        public static MemoryStream ToStream(this byte[] bytes)
        {
            return new MemoryStream(bytes);
        }
        public static MemoryStream AudioMemStream(WaveStream waveStream)
        {
            MemoryStream outputStream = new MemoryStream();
            using (WaveFileWriter waveFileWriter = new WaveFileWriter(outputStream, waveStream.WaveFormat))
            {
                byte[] bytes = new byte[waveStream.Length];
                waveStream.Position = 0;
                waveStream.Read(bytes, 0, Convert.ToInt32(waveStream.Length));
                waveFileWriter.Write(bytes, 0, bytes.Length);
                waveFileWriter.Flush();
            }
            return outputStream;
        }

        public static string GetMD5(this byte[] bytes)
        {
            byte[] hash = MD5.Create().ComputeHash(bytes);
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < hash.Length; ++index)
                stringBuilder.Append(hash[index].ToString("x2"));
            return stringBuilder.ToString();
        }
    }
}
