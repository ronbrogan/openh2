﻿using OpenBlam.Serialization.Layout;
using OpenBlam.Serialization.Materialization;
using System.IO;

namespace OpenH2.Core.Maps.MCC
{
    [ArbitraryLength]
    public class H2mccCompressionSections
    {
        [PrimitiveArray(4096, 1024)]
        public CompressionSection[] Sections { get; set; }

        [FixedLength(8)]
        public readonly struct CompressionSection
        {
            //[PrimitiveValue(0)]
            public readonly int Count { get; }

            //[PrimitiveValue(4)]
            public readonly uint Offset { get; }

            public CompressionSection(int count, uint offset)
            {
                this.Count = count;
                this.Offset = offset;
            }

            // TODO: support deserializing a 'literal'/in-place array of non-primitives
            [PrimitiveValueMaterializer]
            public static CompressionSection ReadCompressionSection(Stream s, int offset)
            {
                return new CompressionSection(s.ReadInt32At(offset), s.ReadUInt32At(offset + 4));
            }
        }
    }
}
