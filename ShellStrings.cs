using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SmarterScreen
{
    public static class ShellStrings
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int EnumResourceNames(IntPtr hModule, IntPtr lpszType, EnumResNameProc lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetLastError();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam);

        private static bool EnumResourceNamesCallback(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam)
        {
            int name = (int)lpName;
            string resourceString = GetStringResource(hModule, name);
            if (resourceString != null)
            {
                if (resourceString.ToLower() == "application")
                {

                }
                Console.WriteLine($"Resource ID {name}: {resourceString}");
            }
            return true; // Continue enumeration
        }
        private static string GetStringResource(IntPtr hModule, int resourceId)
        {
            IntPtr hResInfo = FindResource(hModule, (IntPtr)resourceId, (IntPtr)6); // 6 indicates string table
            if (hResInfo == IntPtr.Zero)
            {
                return null;
            }

            IntPtr hResData = LoadResource(hModule, hResInfo);
            if (hResData == IntPtr.Zero)
            {
                return null;
            }

            IntPtr pResource = LockResource(hResData);
            int size = SizeofResource(hModule, hResInfo);

            if (size == 0)
            {
                return null;
            }

            byte[] resourceBytes = new byte[size];
            Marshal.Copy(pResource, resourceBytes, 0, size);
            return Encoding.Unicode.GetString(resourceBytes);
        }
        public static void ListResourceStrings(string dllPath)
        {
            IntPtr hModule = LoadLibrary(dllPath);
            if (hModule == IntPtr.Zero)
            {
                Console.WriteLine($"Failed to load DLL: {dllPath}");
                return;
            }

            try
            {
                Console.WriteLine($"Listing resources in {dllPath}:");
                EnumResourceNames(hModule, (IntPtr)6, EnumResourceNamesCallback, IntPtr.Zero);
            }
            finally
            {
                FreeLibrary(hModule);
            }
        }

        private static string ExpandEnvironmentVariables(string path)
        {
            // Start position for searching the next placeholder
            int startIndex = 0;

            // Loop through the path string to find and replace placeholders
            while (startIndex < path.Length)
            {
                int percentStart = path.IndexOf('%', startIndex);

                if (percentStart == -1)
                {
                    // No more placeholders
                    break;
                }

                int percentEnd = path.IndexOf('%', percentStart + 1);

                if (percentEnd == -1)
                {
                    // Closing '%' not found; add the remaining part of the string
                    break;
                }

                // Extract the variable name between the '%'
                string variableName = path.Substring(percentStart + 1, percentEnd - percentStart - 1);

                // Get the environment variable value
                string variableValue = Environment.GetEnvironmentVariable(variableName);

                // Replace the placeholder with the actual value
                if (variableValue != null)
                {
                    path = path.Substring(0, percentStart) + variableValue + path.Substring(percentEnd + 1);

                    // Update the search start index to account for the replaced value
                    startIndex = percentStart + variableValue.Length;
                }
                else
                {
                    // If the environment variable is not found, move past the current '%'
                    startIndex = percentEnd + 1;
                }
            }

            return path;
        }
        private static string Find(string targetFile)
        {
            targetFile = ExpandEnvironmentVariables(targetFile);

            string fullPath = Path.GetFullPath(targetFile);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            string path = Environment.GetEnvironmentVariable("PATH");
            string[] paths = path.Split(Path.PathSeparator);

            foreach (string dir in paths)
            {
                string filePath = dir + "\\" + targetFile;
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }

            return null;
        }

        // Evaluates a path expression "@shell32.dll,-50944" 
        public static string GetString(string pathExpression)
        {
            if (!pathExpression.StartsWith("@"))
            {
                return pathExpression;
            }
            string pathExpressionTrimmed = pathExpression.Substring(1);
            string[] pathExpressionSplit = pathExpressionTrimmed.Split(',');
            string dllName = pathExpressionSplit[0];
            string dllPath = Find(dllName);
            IntPtr hModule = LoadLibrary(dllPath);
            if (hModule == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(GetLastError());
            }
            try
            {
                int resourceID = int.Parse(pathExpressionSplit[1]);
                if(resourceID < 0)
                {
                    resourceID = -resourceID;
                }
                IntPtr hResInfo = FindResource(hModule, (IntPtr)resourceID, (IntPtr)6); // 6 indicates string table
                if (hResInfo == IntPtr.Zero)
                {
                    throw new System.ComponentModel.Win32Exception(GetLastError());
                }

                IntPtr hResData = LoadResource(hModule, hResInfo);
                if (hResData == IntPtr.Zero)
                {
                    throw new System.ComponentModel.Win32Exception(GetLastError());
                }

                IntPtr pResource = LockResource(hResData);
                int size = SizeofResource(hModule, hResInfo);

                byte[] resourceBytes = new byte[size];
                Marshal.Copy(pResource, resourceBytes, 0, size);
                return Encoding.Unicode.GetString(resourceBytes);
            }
            finally
            {
                FreeLibrary(hModule);
            }
        }
    }
}
