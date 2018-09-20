﻿using OpenH2.Core.Representations;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenH2.Core.Offsets
{
    public class SecondaryOffset : IOffset
    {
        private Scene scene;
        private int offset;

        public SecondaryOffset(Scene scene, int offsetValue)
        {
            this.offset = offsetValue;
            this.scene = scene;
        }

        public int Value => this.offset + this.scene.SecondaryMagic;

        public int OriginalValue => this.offset;
    }
}
