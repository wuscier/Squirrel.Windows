﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Squirrel
{
    static class SquirrelAwareExecutableDetector
    {
        public static int? GetPESquirrelAwareVersion(string executable)
        {
            if (!File.Exists(executable)) return null;
            var fullname = Path.GetFullPath(executable);

            return GetAssemblySquirrelAwareVersion(fullname) ?? GetVersionBlockSquirrelAwareValue(fullname);
        }

        static int? GetAssemblySquirrelAwareVersion(string executable)
        {
            try 
            {
                var assembly = Assembly.ReflectionOnlyLoadFrom(executable);
                var attrs = assembly.GetCustomAttributesData();
                var attribute = attrs.FirstOrDefault(x => 
                {
                    if (x.AttributeType != typeof(AssemblyMetadataAttribute)) return false;
                    if (x.ConstructorArguments.Count != 2) return false;
                    return x.ConstructorArguments[0].Value.ToString() == "SquirrelAwareVersion";
                });

                if (attribute == null) return null;

                int result;
                if (!Int32.TryParse(attribute.ConstructorArguments[1].Value.ToString(), NumberStyles.Integer, CultureInfo.CurrentCulture, out result)) 
                {
                    return null;
                }

                return result;
            } 
            catch (FileLoadException) { return null; }
            catch (BadImageFormatException) { return null; }
        }

        static int? GetVersionBlockSquirrelAwareValue(string executable)
        {
            int size = NativeMethods.GetFileVersionInfoSize(executable, IntPtr.Zero);

            // Nice try, buffer overflow
            if (size <= 0 || size > 4096) return null;

            var buf = new byte[size];
            if (!NativeMethods.GetFileVersionInfo(executable, IntPtr.Zero, size, buf)) return null;

            IntPtr result; int resultSize;
            if (!NativeMethods.VerQueryValue(buf, "\\StringFileInfo\\040904B0\\SquirrelAwareVersion", out result, out resultSize)) 
            {
                return null;
            }

            int ret;
            string resultData = Marshal.PtrToStringAnsi(result, resultSize-1 /* Subtract one for null terminator */);
            if (!Int32.TryParse(resultData, NumberStyles.Integer, CultureInfo.CurrentCulture, out ret)) return null;

            return ret;
        }
    }
}
