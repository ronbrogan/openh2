﻿using OpenH2.Core.Enums.Texture;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenH2.Core.Representations.Meta
{
    public class BitmMeta : BaseMeta
    {
        public TextureType TextureType { get; set; }

        public TextureFormat TextureFormat { get; set; }

        public TextureUsage TextureUsage { get; set; }

        public short MipMapCount { get; set; }

        public string Tag { get; set; }

        public short Width { get; set; }

        public short Height { get; set; }

        public short Depth { get; set; }

        public short Type { get; set; }

        public short Format { get; set; }

        public TextureProperties Properties { get; set; }

        public short RegX { get; set; }

        public short RegY { get; set; }

        public short MipMapCount2 { get; set; }

        public short PixelOffset { get; set; }

        public BitmLevelOfDetail[] LevelsOfDetail { get; set; }

        public uint ID { get; set; }
    }
}
