﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ultz.SuperInvoke.AOT
{
    public static class AheadOfTimeActivator
    {
        public static void WriteImplementation(Stream stream, ref BuilderOptions opts)
        {
            using var asm = LibraryBuilder.CreateAssembly(new[] {opts});
            asm.Write(stream);
        }
        
        public static void WriteImplementation(Stream stream, IEnumerable<BuilderOptions> opts)
        {
            using var asm = LibraryBuilder.CreateAssembly(opts);
            asm.Write(stream);
        }

        public static void WriteImplementation<T>(Stream stream)
        {
            var opts = BuilderOptions.GetDefault(typeof(T));
            WriteImplementation(stream, ref opts);
        }

        public static Assembly LoadImplementation<T>(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return AppDomain.CurrentDomain.Load(ms.ToArray());
        }
    }
}