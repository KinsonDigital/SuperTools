﻿using System;
using System.Runtime.InteropServices;

namespace Ultz.SuperInvoke.Native
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
    public class NativeApiAttribute : Attribute
    {
        /// <summary>
        ///     Gets or sets the native entry point for this method. Ignored on classes and interfaces.
        /// </summary>
        public string EntryPoint { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the native entry point prefix for this method. If used on a method, this will override the
        ///     prefix set in a class or interface declaration. Otherwise, this will be inherited from the class or
        ///     interface.
        /// </summary>
        public string Prefix { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the calling convention.
        /// </summary>
        public CallingConvention? Convention { get; set; } = null;

        public static string GetEntryPoint(NativeApiAttribute attr, NativeApiAttribute parent, string method)
        {
            return (string.IsNullOrEmpty(attr?.Prefix) ? parent?.Prefix : attr.Prefix) +
                   (string.IsNullOrEmpty(attr?.EntryPoint) ? method : attr.EntryPoint);
        }

        public static CallingConvention GetCallingConvention(NativeApiAttribute attr, NativeApiAttribute parent)
        {
            return attr?.Convention ?? parent?.Convention ?? CallingConvention.Cdecl;
        }
    }
}