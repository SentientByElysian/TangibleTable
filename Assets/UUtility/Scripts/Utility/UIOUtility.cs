using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UTool.Utility
{
    public static partial class UUtility
    {
        /// <summary>
        /// Creates File and Directory If it does not exist on the Path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns true if a New File was Created</returns>
        public static bool CheckAndCreateFile(string path)
        {
            bool fileCreated = false;

            string filename = path.Split('\\').Last();
            string folder = path.Replace($@"\{filename}", "");
            CheckAndCreateDirectory(folder);

            path = PlatformSpecificPath(path);

            if (!File.Exists(path))
            {
                File.Create(path).Close();
                fileCreated = true;
            }

            return fileCreated;
        }

        /// <summary>
        /// Creates Directory If it does not exist on the Path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns true if a New Directory was Created</returns>
        public static bool CheckAndCreateDirectory(string path)
        {
            bool DirCreated = false;

            path = PlatformSpecificPath(path);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                DirCreated = true;
            }

            return DirCreated;
        }

        public static bool IsFileAccessible(string path)
        {
            try
            {
                File.Open(path, FileMode.Open, FileAccess.ReadWrite).Dispose();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void WriteAllText(string path, string text)
        {
            File.WriteAllText(PlatformSpecificPath(path), text);
        }

        public static string ReadAllText(string path)
        {
            return File.ReadAllText(PlatformSpecificPath(path));
        }

        public static void DeleteFile(string path)
        {
            File.Delete(PlatformSpecificPath(path));
        }

        public static string[] GetDirectoryFiles(string path)
        {
            return Directory.GetFiles(PlatformSpecificPath(path));
        }

        public static bool FileExists(string path)
        {
            return File.Exists(PlatformSpecificPath(path));
        }

        public static FileStream CreateFile(string path)
        {
            return File.Create(PlatformSpecificPath(path));
        }

        public static string PlatformSpecificPath(string path)
        {
#if PLATFORM_ANDROID && !UNITY_EDITOR
            return path.Replace(@"\", "/");
#else
            return path;
#endif
        }
    }
}