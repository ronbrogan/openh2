﻿using OpenH2.Core.Enums;
using OpenH2.Core.Parsing;
using OpenH2.Core.Representations;

namespace OpenH2.Core.Tags
{
    public abstract class BaseTag
    {
        public virtual string Name { get; set; }

        public uint Id { get; private set; }

        public uint Offset { get; set; }
        
        public uint Length { get; set; }

        public TagIndexEntry TagIndexEntry { get; set; }

        public DataFile DataFile { get; set; }

#if DEBUG
        public int InternalSecondaryMagic { get; set; }
#endif

        public BaseTag(uint id)
        {
            this.Id = id;
        }

        public virtual void PopulateExternalData(H2vReader reader) { }
    }
}