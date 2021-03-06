﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Ultz.SuperInvoke.InteropServices;
using Ultz.SuperInvoke.Loader;

namespace Ultz.SuperInvoke.Emit
{
    public class LibraryBuilder
    {
        private AssemblyBuilder _asm;
        private Random _random = new Random();
        private ModuleBuilder _mod;
        
        public string Name { get; set; }

        public LibraryBuilder()
        {
        }

        public void Add(in BuilderOptions opts)
        {
            var bytes = new byte[4];
            _random.NextBytes(bytes);
            Name ??= "Ultz.Private.SIG." + opts.Type.Assembly.GetName().Name + ".X" +
                       BitConverter.ToString(bytes).Replace("-", "");
            _asm ??= AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(Name),
#if NET461
                AssemblyBuilderAccess.RunAndSave);
                _mod ??= _asm.DefineDynamicModule(Name + ".dll", Name + ".dll");
#else
                AssemblyBuilderAccess.Run);
                _mod ??= _asm.DefineDynamicModule(Name + ".dll");
#endif
            
            _random.NextBytes(bytes);
            var wipType =
                _mod.DefineType(opts.Type.Namespace + ".X" + BitConverter.ToString(bytes).Replace("-", "") + "." + opts.Type.Name,
                    TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, opts.Type);

            CreateMethods(opts.Type, wipType, out var slotEntryPoints, opts.Generator);
            CreateConstructor(opts.Type, wipType, opts.UseLazyBinding, slotEntryPoints);

            wipType.CreateTypeInfo();
        }

        private void CreateMethods(Type src, TypeBuilder dest, out IReadOnlyList<string> slots, IGenerator generator)
        {
            var parentAttr = src.GetCustomAttribute<NativeApiAttribute>();
            var slotEps = new List<string>();
            foreach (var method in src.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
            {
                var attr = method.GetCustomAttribute<NativeApiAttribute>();
                
                if (!method.IsAbstract)
                {
                    continue;
                }

                var wip = dest.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, method.CallingConvention, method.ReturnType,
                    method.ReturnParameter.GetRequiredCustomModifiers(),
                    method.ReturnParameter.GetOptionalCustomModifiers(),
                    method.GetParameters().Select(x => x.ParameterType).ToArray(),
                    method.GetParameters().Select(x => x.GetRequiredCustomModifiers()).ToArray(),
                    method.GetParameters().Select(x => x.GetOptionalCustomModifiers()).ToArray());
                MarshalUtils.CopyGenericTypes(method, wip);
                var ctx = new MethodGenerationContext(method, wip,
                    NativeApiAttribute.GetEntryPoint(attr, parentAttr, method.Name), slotEps.Count,
                    NativeApiAttribute.GetCallingConvention(attr, parentAttr));
                slotEps.Add(ctx.EntryPoint);
                generator.GenerateMethod(ctx);
            }

            slots = slotEps;
        }

        private void CreateConstructor(Type src, TypeBuilder dest, bool lazy, IReadOnlyList<string> slotEntryPoints)
        {
            var ctor = dest.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName, CallingConventions.Standard, new[] {typeof(NativeApiContext).MakeByRefType()});
            var il = ctor.GetILGenerator();

            // Load the NativeApiContext we've already been passed,
            // so we can pass it to NativeApiContainer.CreateContext()
            il.Emit(OpCodes.Ldarg_1);

            // Push the nullable for the slot count onto the stack
            il.Emit(OpCodes.Ldc_I4, slotEntryPoints.Count);
            il.Emit(OpCodes.Newobj, typeof(int?).GetConstructor(new []{typeof(int)}));
            
            // Create the new context
            il.Emit(OpCodes.Call, NativeApiContainer.NewContextMethod);
            
            // Store the new context
            var loc = il.DeclareLocal(typeof(NativeApiContext));
            il.Emit(OpCodes.Stloc, loc);
            
            // Call the base constructor
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloca, loc);
            il.Emit(OpCodes.Call,
                src.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new[] {typeof(NativeApiContext).MakeByRefType()}, null) ?? throw new InvalidOperationException(
                    "Source type must have a constructor matching" +
                    "the NativeApiContainer(NativeApiContext&) constructor."));

            if (!lazy)
            {
                for (var i = 0; i < slotEntryPoints.Count; i++)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.Emit(OpCodes.Ldstr, slotEntryPoints[i]);
                    il.Emit(OpCodes.Call, typeof(NativeApiContainer).GetMethod("Load",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                        new[] {typeof(int), typeof(string)}, null));
                    il.Emit(OpCodes.Pop);
                }
            }

            il.Emit(OpCodes.Ret);
        }

        public Assembly Build()
        {
            return _asm;
        }

        public static Assembly CreateAssembly(params BuilderOptions[] pipeline)
        {
            var builder = new LibraryBuilder();
            foreach (var opts in pipeline)
            {
                builder.Add(opts);
            }

            return builder.Build();
        }
    }
}