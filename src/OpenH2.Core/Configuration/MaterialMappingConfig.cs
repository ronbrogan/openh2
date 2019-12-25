﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OpenH2.Core.Configuration
{
    public class MaterialMappingConfig
    {
        public Dictionary<uint, MaterialAlias> Aliases { get; set; }
        public Dictionary<uint, MaterialMapping> Mappings { get; set; }
    }
}
