﻿using System.Numerics;

namespace OpenH2.Rendering.Abstractions
{
    public interface IGraphicsHost
    {
        void CreateWindow(Vector2 size, bool hidden = false);
        IGraphicsAdapter GetGraphicsAdapter();
        void EnableConsoleDebug();

        bool AspectRatioChanged { get; }
        float AspectRatio { get; }
    }
}
