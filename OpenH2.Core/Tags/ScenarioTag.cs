﻿using OpenH2.Core.Tags.Layout;

namespace OpenH2.Core.Tags
{
    [TagLabel("scnr")]
    public class ScenarioTag : BaseTag
    {
        public override string Name { get; set; }
        public ScenarioTag(uint id) : base(id)
        {
        }

        [InternalReferenceValue(8)]
        public SkyboxReference[] SkyboxReferences { get; set; }


        [InternalReferenceValue(72)] public Obj72[] Obj72s { get; set; }

        [InternalReferenceValue(80)] public SceneryInstance[] SceneryInstances { get; set; }
        [InternalReferenceValue(88)] public SceneryDefinition[] SceneryReferences { get; set; }

        [InternalReferenceValue(152)] public Obj152[] Obj152s { get; set; }

        [InternalReferenceValue(168)] public Obj168[] Obj168s { get; set; }

        [InternalReferenceValue(176)] public Obj176[] Obj176s { get; set; }

        [InternalReferenceValue(216)] public Obj216[] Obj216s { get; set; }

        [InternalReferenceValue(224)] public Obj224[] Obj224s { get; set; }

        [InternalReferenceValue(232)] public Obj232[] Obj232s { get; set; }

        [InternalReferenceValue(240)] public Obj240[] Obj240s { get; set; }

        [InternalReferenceValue(256)] public Obj256[] Obj256s { get; set; }

        [InternalReferenceValue(264)] public Obj264[] Obj264s { get; set; }

        [InternalReferenceValue(280)] public Obj280[] Obj280s { get; set; }

        [InternalReferenceValue(288)] public Obj288[] Obj288s { get; set; }

        [InternalReferenceValue(296)] public Obj296[] Obj296s { get; set; }

        [InternalReferenceValue(320)] public Obj320[] Obj320s { get; set; }

        [InternalReferenceValue(432)] public Obj432[] Obj432s { get; set; }

        [InternalReferenceValue(472)] public Obj472[] Obj472s { get; set; }


        [InternalReferenceValue(528)]
        public Terrain[] Terrains { get; set; }

        [InternalReferenceValue(536)] public Obj536[] Obj536s { get; set; }

        [InternalReferenceValue(560)] public Obj560[] Obj560s { get; set; }

        [InternalReferenceValue(568)] public Obj568[] Obj568s { get; set; }

        [InternalReferenceValue(592)] public Obj592[] Obj592s { get; set; }

        [InternalReferenceValue(600)] public Obj600[] Obj600s { get; set; }

        [InternalReferenceValue(656)] public Obj656[] Obj656s { get; set; }

        [InternalReferenceValue(792)] public Obj792[] Obj792s { get; set; }

        [InternalReferenceValue(808)] public Obj808[] Obj808s { get; set; }

        [InternalReferenceValue(816)] public Obj816[] Obj816s { get; set; }

        [InternalReferenceValue(840)] public Obj840[] Obj840s { get; set; }

        [InternalReferenceValue(904)] public Obj904[] Obj904s { get; set; }

        [InternalReferenceValue(920)] public Obj920[] Obj920s { get; set; }

        [InternalReferenceValue(984)] public Obj984[] Obj984s { get; set; }


        [FixedLength(8)]
        public class SkyboxReference
        {
            [PrimitiveValue(4)]
            public uint SkyboxId { get; set; }
        }

        [FixedLength(68)]
        public class Terrain
        {
            [PrimitiveValue(20)]
            public uint BspId { get; set; }

            public BspTag Bsp { get; set; }

            // TODO implement lightmap tag
            [PrimitiveValue(28)]
            public uint LightmapId { get; set; }

            public BaseTag Lightmap { get; set; }
        }

        [FixedLength(36)] public class Obj72 { }

        [FixedLength(92)]
        public class SceneryInstance
        {
            [PrimitiveValue(0)]
            public ushort SceneryDefinitionIndex { get; set; }

            [PrimitiveValue(8)]
            public float X { get; set; }

            [PrimitiveValue(12)]
            public float Y { get; set; }

            [PrimitiveValue(16)]
            public float Z { get; set; }

            [PrimitiveValue(20)]
            public float Yaw { get; set; }

            [PrimitiveValue(24)]
            public float Pitch { get; set; }

            [PrimitiveValue(28)]
            public float Roll { get; set; }
        }

        [FixedLength(40)]
        public class SceneryDefinition
        {
            [PrimitiveValue(4)]
            public uint SceneryId { get; set; }
        }

        [FixedLength(40)] public class Obj152 { }

        [FixedLength(72)]
        public class Obj168
        {
            [PrimitiveValue(8)]
            public float X { get; set; }

            [PrimitiveValue(12)]
            public float Y { get; set; }

            [PrimitiveValue(16)]
            public float Z { get; set; }

            [PrimitiveValue(20)]
            public float I { get; set; }

            [PrimitiveValue(24)]
            public float J { get; set; }

            [PrimitiveValue(28)]
            public float K { get; set; }

        }
        [FixedLength(40)]
        public class Obj176
        {

        }

        [FixedLength(80)]
        public class Obj216
        {
            [PrimitiveValue(8)]
            public float X { get; set; }

            [PrimitiveValue(12)]
            public float Y { get; set; }

            [PrimitiveValue(16)]
            public float Z { get; set; }

            [PrimitiveValue(20)]
            public float I { get; set; }

            [PrimitiveValue(24)]
            public float J { get; set; }

            [PrimitiveValue(28)]
            public float K { get; set; }
        }

        [FixedLength(40)] public class Obj224 { }

        [FixedLength(108)]
        public class Obj232
        {
            [PrimitiveValue(8)]
            public float X { get; set; }

            [PrimitiveValue(12)]
            public float Y { get; set; }

            [PrimitiveValue(16)]
            public float Z { get; set; }

            [PrimitiveValue(20)]
            public float I { get; set; }

            [PrimitiveValue(24)]
            public float J { get; set; }

            [PrimitiveValue(28)]
            public float K { get; set; }
        }

        [FixedLength(40)] public class Obj240 { }

        [FixedLength(52)]
        public class Obj256
        {
            [PrimitiveValue(0)]
            public float X { get; set; }

            [PrimitiveValue(4)]
            public float Y { get; set; }

            [PrimitiveValue(8)]
            public float Z { get; set; }
        }

        [FixedLength(68)]
        public class Obj264
        {
            [PrimitiveValue(12)]
            public float X { get; set; }

            [PrimitiveValue(16)]
            public float Y { get; set; }

            [PrimitiveValue(20)]
            public float Z { get; set; }
        }

        [FixedLength(32)]
        public class Obj280
        {
            [PrimitiveValue(0)]
            public float X { get; set; }

            [PrimitiveValue(4)]
            public float Y { get; set; }

            [PrimitiveValue(8)]
            public float Z { get; set; }
        }

        [FixedLength(144)] public class Obj288 { }

        [FixedLength(156)] public class Obj296 { }

        [FixedLength(8)] public class Obj320 { }

        [FixedLength(1)] public class Obj432 { }

        [FixedLength(128)] public class Obj472 { }

        [FixedLength(152)] public class Obj536 { }

        [FixedLength(2)] public class Obj560 { }

        [FixedLength(20)] public class Obj568 { }

        [FixedLength(100)] public class Obj592 { }

        [FixedLength(72)] public class Obj600 { }

        [FixedLength(192)] public class Obj656 { }

        [FixedLength(816)] public class Obj792 { }

        [FixedLength(76)]
        public class Obj808
        {
            [PrimitiveValue(8)]
            public float X { get; set; }

            [PrimitiveValue(12)]
            public float Y { get; set; }

            [PrimitiveValue(16)]
            public float Z { get; set; }
        }

        [FixedLength(40)] public class Obj816 { }

        [FixedLength(16)] public class Obj840 { }

        [FixedLength(16)] public class Obj904 { }

        [FixedLength(3196)] public class Obj920 { }

        [FixedLength(4)] public class Obj984 { }

    }
}