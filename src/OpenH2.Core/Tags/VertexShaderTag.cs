﻿using OpenH2.Core.Tags.Layout;
using OpenBlam.Serialization.Layout;

namespace OpenH2.Core.Tags
{
    [TagLabel(TagName.vrtx)]
    public class VertexShaderTag : BaseTag
    {
        public override string Name { get; set; }

        public VertexShaderTag(uint id) : base(id)
        {
        }

        [ReferenceArray(4)]
        public ShaderReference[] Shaders { get; set; }


        [FixedLength(20)]
        public class ShaderReference
        {
            [ReferenceArray(4)]
            public byte[] ShaderData { get; set; }
        }
    }
}
