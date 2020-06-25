// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tmds.DBus.Emit, PublicKey=002400000480000094000000060200000024000052534131000400000100010071a8770f460cce31df0feb6f94b328aebd55bffeb5c69504593df097fdd9b29586dbd155419031834411c8919516cc565dee6b813c033676218496edcbe7939c0dd1f919f3d1a228ebe83b05a3bbdbae53ce11bcf4c04a42d8df1a83c2d06cb4ebb0b447e3963f48a1ca968996f3f0db8ab0e840a89d0a5d5a237e2f09189ed3")]

namespace Tmds.DBus.CodeGen
{
    internal class DynamicAssembly
    {
        public static readonly DynamicAssembly Instance = new DynamicAssembly();

        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<Type, TypeInfo> _proxyTypeMap;
        private readonly Dictionary<Type, TypeInfo> _adapterTypeMap;
        private readonly object _gate = new object();

        private DynamicAssembly()
        {
            var keyStream = typeof(DynamicAssembly).GetTypeInfo().Assembly.GetManifestResourceStream("Tmds.DBus.sign.snk");
            if (keyStream == null) throw new InvalidOperationException("'Tmds.DBus.sign.snk' not found in resources");
            var keyBuffer = new byte[keyStream.Length];
            keyStream.Read(keyBuffer, 0, keyBuffer.Length);
            var assemblyName = new AssemblyName(Connection.DynamicAssemblyName);
            assemblyName.Version = new Version(1, 0, 0);
            assemblyName.Flags = AssemblyNameFlags.PublicKey;
            assemblyName.SetPublicKey(keyBuffer);
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(Connection.DynamicAssemblyName);
            _proxyTypeMap = new Dictionary<Type, TypeInfo>();
            _adapterTypeMap = new Dictionary<Type, TypeInfo>();
        }

        public TypeInfo GetProxyTypeInfo(Type interfaceType)
        {
            TypeInfo typeInfo;            
            lock (_proxyTypeMap)
            {
                if (_proxyTypeMap.TryGetValue(interfaceType, out typeInfo))
                {
                    return typeInfo;
                }
            }

            lock (_gate)
            {
                lock (_proxyTypeMap)
                {
                    if (_proxyTypeMap.TryGetValue(interfaceType, out typeInfo))
                    {
                        return typeInfo;
                    }
                }

                typeInfo = new DBusObjectProxyTypeBuilder(_moduleBuilder).Build(interfaceType);

                lock (_proxyTypeMap)
                {
                    _proxyTypeMap[interfaceType] = typeInfo;
                }

                return typeInfo;
            }
        }

        public TypeInfo GetExportTypeInfo(Type objectType)
        {
            TypeInfo typeInfo;

            lock (_adapterTypeMap)
            {
                if (_adapterTypeMap.TryGetValue(objectType, out typeInfo))
                {
                    return typeInfo;
                }
            }

            lock (_gate)
            {
                lock (_adapterTypeMap)
                {
                    if (_adapterTypeMap.TryGetValue(objectType, out typeInfo))
                    {
                        return typeInfo;
                    }
                }

                typeInfo = new DBusAdapterTypeBuilder(_moduleBuilder).Build(objectType);

                lock (_adapterTypeMap)
                {
                    _adapterTypeMap[objectType] = typeInfo;
                }

                return typeInfo;
            }
        }
    }
}
