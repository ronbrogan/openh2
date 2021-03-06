﻿namespace OpenH2.Core.Tags.Common.Models
{
    public interface IModelResourceContainer
    {
        ushort VertexCount { get; set; }

        ushort TriangleCount { get; set; }

        ushort PartCount { get; set; }

        uint DataBlockRawOffset { get; set; }

        uint DataBlockSize { get; set; }

        uint DataPreambleSize { get; set; }

        ModelResourceBlockHeader Header { get; set; }

        ModelResource[] Resources { get; set; }
    }
}
