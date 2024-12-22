using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmarterScreen
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            args = new string[] { "C:\\Users\\RandomiaGaming\\Desktop\\Download.mp4" };
            if (args is null || args.Length is 0)
            {
                Install();
                Console.WriteLine("Installed smarter screen!");
            }
            else
            {
                FileStream altStream = new FileStream(args[0] + ":Zone.Identifier", FileMode.Open, FileAccess.Read);
                StreamReader streamReader = new StreamReader(altStream);
                string str = streamReader.ReadToEnd();
                streamReader.Dispose();
                altStream.Dispose();
                Console.WriteLine(str);
            }
        }
        public static void Install()
        {
            // Files run with the windows executable loader
            RestrictExecutionOfExt(".exe");
            RestrictExecutionOfExt(".com");
            RestrictExecutionOfExt(".scr");
            RestrictExecutionOfExt(".msi");
            RestrictExecutionOfExt(".msp");
            RestrictExecutionOfExt(".dll");
            RestrictExecutionOfExt(".ocx");

            // Files run with script engines
            RestrictExecutionOfExt(".bat");
            RestrictExecutionOfExt(".cmd");
            RestrictExecutionOfExt(".ps1");
            RestrictExecutionOfExt(".psm1");
            RestrictExecutionOfExt(".psd1");
            RestrictExecutionOfExt(".ps1xml");
            RestrictExecutionOfExt(".vbs");
            RestrictExecutionOfExt(".wsf");
            RestrictExecutionOfExt(".js");
            RestrictExecutionOfExt(".jse");
            RestrictExecutionOfExt(".hta");
            RestrictExecutionOfExt(".shs");

            // Microsoft office files with VBA scripts
            RestrictExecutionOfExt(".docx");
            RestrictExecutionOfExt(".xlsx");
            RestrictExecutionOfExt(".pptx");

            // Control panel and setting snap ins
            RestrictExecutionOfExt(".msc");
            RestrictExecutionOfExt(".cpl");

            // Miscalanious dangerous files
            RestrictExecutionOfExt(".jar");
            RestrictExecutionOfExt(".reg");
            RestrictExecutionOfExt(".ini");
        }
        public static void RestrictExecutionOfExt(string extension = ".exe")
        {
            if (!Registry.ClassesRoot.GetSubKeyNames().Contains(extension))
            {
                Console.WriteLine($"{extension} does not exist in the registries.");
                return;
            }
            RegistryKey dotExt = Registry.ClassesRoot.OpenSubKey(extension);
            RegistryKey extFile = Registry.ClassesRoot.OpenSubKey((string)dotExt.GetValue(null));
            if (!extFile.GetSubKeyNames().Contains("shell"))
            {
                Console.WriteLine($"{extension} does not have shell actions.");
                extFile.Close();
                dotExt.Close();
                return;
            }
            RegistryKey shell = extFile.OpenSubKey("shell");
            if (!shell.GetSubKeyNames().Contains("open"))
            {
                Console.WriteLine($"{extension} cannot be openned with the shell.");
                shell.Close();
                extFile.Close();
                dotExt.Close();
                return;
            }
            RegistryKey open = shell.OpenSubKey("open");
            if (!open.GetSubKeyNames().Contains("command"))
            {
                Console.WriteLine($"{extension} doesn't run a command when openned.");
                open.Close();
                shell.Close();
                extFile.Close();
                dotExt.Close();
                return;
            }
            RegistryKey command = open.OpenSubKey("command", true);
            Console.WriteLine($"Shell command for {extension} was {command.GetValue(null)}");
            string exePath = typeof(Program).Assembly.Location;
            command.SetValue(null, $"\"{exePath}\" \"%1\" %*");
            command.Close();
            open.Close();
            shell.Close();
            extFile.Close();
            dotExt.Close();
        }
    }
}