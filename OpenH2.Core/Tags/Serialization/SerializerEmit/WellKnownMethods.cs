﻿using OpenH2.Core.Extensions;
using OpenH2.Core.Offsets;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace OpenH2.Core.Tags.Serialization.SerializerEmit
{
    internal static class MI
    {
        public static Dictionary<Type, MethodInfo> PrimitiveSpanReaders = new Dictionary<Type, MethodInfo>
        {
            { typeof(byte), typeof(SpanByteExtensions).GetMethod(nameof(SpanByteExtensions.ReadByteAt)) },
            { typeof(short), typeof(SpanByteExtensions).GetMethod(nameof(SpanByteExtensions.ReadInt16At)) },
            { typeof(ushort), typeof(SpanByteExtensions).GetMethod(nameof(SpanByteExtensions.ReadUInt16At)) },
            { typeof(int), MI.SpanByte.ReadInt32At},
            { typeof(uint), typeof(SpanByteExtensions).GetMethod(nameof(SpanByteExtensions.ReadUInt32At)) },
            { typeof(float), typeof(SpanByteExtensions).GetMethod(nameof(SpanByteExtensions.ReadFloatAt)) },
        };

        public static Dictionary<Type, int> PrimitiveSizes = new Dictionary<Type, int>
        {
            { typeof(byte), 1 },
            { typeof(short), 2 },
            { typeof(ushort), 2 },
            { typeof(int), 4},
            { typeof(uint), 4 },
            { typeof(float), 4 },
        };

        public static class Runtime
        {
            public static MethodInfo GetUninitializedObject = typeof(FormatterServices)
                .GetMethod(nameof(FormatterServices.GetUninitializedObject));
        }

        public static class SystemType
        {
            public static MethodInfo GetTypeFromHandle = typeof(Type)
                .GetMethod(nameof(Type.GetTypeFromHandle));
        }

        public static class SpanByte
        {
            public static MethodInfo ReadInt32At = typeof(SpanByteExtensions)
                .GetMethod(nameof(SpanByteExtensions.ReadInt32At));

            public static MethodInfo ReadMetaCaoAt = typeof(SpanByteExtensions)
                .GetMethod(nameof(SpanByteExtensions.ReadMetaCaoAt), new[] { typeof(Span<byte>), typeof(int), typeof(int) });

            public static MethodInfo Slice = typeof(Span<byte>)
                .GetMethod(nameof(Span<byte>.Slice), new[] { typeof(int), typeof(int) });
        }

        public static class Cao
        {
            public static ConstructorInfo Ctor = typeof(CountAndOffset)
                .GetConstructor(new[] { typeof(int), typeof(IOffset) });

            public static MethodInfo OffsetGetter = typeof(CountAndOffset)
               .GetProperty(nameof(CountAndOffset.Offset)).GetGetMethod();

            public static MethodInfo CountGetter = typeof(CountAndOffset)
                .GetProperty(nameof(CountAndOffset.Count)).GetGetMethod();

            public static MethodInfo IntDictAddMethod = typeof(Dictionary<int, CountAndOffset>)
                .GetMethod("Add", new[] { typeof(int), typeof(CountAndOffset) });

            public static MethodInfo IntDictLookup = typeof(Dictionary<int, CountAndOffset>)
                .GetMethod("get_Item", new[] { typeof(int) });
        }

        public static class Offset
        {
            public static ConstructorInfo TagInternalOffsetCtor = typeof(TagInternalOffset)
                .GetConstructor(new[] { typeof(int), typeof(int) });

            public static MethodInfo IOffsetValue = typeof(IOffset)
                .GetProperty(nameof(IOffset.Value)).GetGetMethod();
        }
    }
}