﻿using OpenH2.Core.Architecture;
using OpenH2.Core.GameObjects;
using OpenH2.Engine.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenH2.Engine.Entities
{
    public class Bloc : GameObjectEntity, IBloc
    {
        public Bloc()
        {
            this.Components = new Component[0];
        }
    }
}
