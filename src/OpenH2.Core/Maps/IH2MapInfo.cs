﻿using OpenBlam.Core.MapLoading;
using OpenBlam.Core.Maps;
using System;

namespace OpenH2.Core.Maps
{
    public interface IH2MapInfo : IMap
    {
        string Name { get; }
        int PrimaryMagic { get; set; }
        int SecondaryMagic { get; set; }
        IH2MapHeader Header { get; }
    }
}
