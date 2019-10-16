﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ultz.SuperInvoke.Loader;
using Ultz.SuperInvoke.Native;
using Ultz.SuperInvoke.Proxy;

namespace Ultz.SuperInvoke
{
    public class LibraryActivator
    {
        public static Type GetImplementation<T>(BuilderOptions? jitOpts = null)
            where T:NativeApiContainer
        {
            var opts = jitOpts ?? BuilderOptions.GetDefault(typeof(T));
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetExportedTypes())
                       .FirstOrDefault(x => typeof(T).IsAssignableFrom(x) && x != typeof(T)) ??
                   GetImplementationJustInTime(ref opts);
        }

        public static Type GetImplementationJustInTime(ref BuilderOptions opts)
        {
            using var ms = new MemoryStream();
            using var asm = LibraryBuilder.CreateAssembly(new[] {opts});
            var type = opts.Type;
            asm.Write(ms);
            var loadedAsm = Assembly.Load(ms.ToArray());
            return loadedAsm.GetExportedTypes().FirstOrDefault(type.IsAssignableFrom);
        }

        public static T CreateStaticallyLinkedInstance<T>(BuilderOptions? jitOpts = null)
            where T:NativeApiContainer
        {
            var opts = jitOpts ?? BuilderOptions.GetDefault(typeof(T));
            opts.PInvokeName = "__Internal";
            return CreateInstance<T>(new NativeLibrary("__Internal", new PInvokeLibraryLoader(typeof(T))), jitOpts);
        }

        public static T CreateInstance<T>(NativeLibrary nativeLibrary, BuilderOptions? jitOpts = null)
            where T:NativeApiContainer
        {
            var impl = GetImplementation<T>();
            return (T) Activator.CreateInstance(impl, nativeLibrary);
        }

        public static T CreateInstance<T>(string name, BuilderOptions? jitOpts = null)
            where T:NativeApiContainer
        {
            return CreateInstance<T>(new NativeLibrary(name), jitOpts);
        }

        public static T CreateInstance<T>(string name, LibraryLoader loader, BuilderOptions? jitOpts = null)
            where T:NativeApiContainer
        {
            return CreateInstance<T>(new NativeLibrary(name, loader), jitOpts);
        }

        public static T CreateInstance<T>(string name, LibraryLoader loader, PathResolver resolver,
            BuilderOptions? jitOpts = null)
            where T:NativeApiContainer
        {
            return CreateInstance<T>(new NativeLibrary(name, loader, resolver), jitOpts);
        }

        public static T CreateInstance<T>(string[] names, BuilderOptions? jitOpts = null)
            where T:NativeApiContainer
        {
            return CreateInstance<T>(new NativeLibrary(names), jitOpts);
        }

        public static T CreateInstance<T>(string[] names, LibraryLoader loader, BuilderOptions? jitOpts = null)
            where T:NativeApiContainer
        {
            return CreateInstance<T>(new NativeLibrary(names, loader), jitOpts);
        }

        public static T CreateInstance<T>(string[] names, LibraryLoader loader, PathResolver resolver,
            BuilderOptions? jitOpts = null)
            where T:NativeApiContainer
        {
            return CreateInstance<T>(new NativeLibrary(names, loader, resolver), jitOpts);
        }
    }
}