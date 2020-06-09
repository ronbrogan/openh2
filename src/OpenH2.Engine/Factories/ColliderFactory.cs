﻿using OpenH2.Core.Representations;
using OpenH2.Core.Tags;
using OpenH2.Foundation;
using OpenH2.Foundation.Physics;
using OpenH2.Physics.Colliders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace OpenH2.Engine.Factories
{
    public static class ColliderFactory
    {
        private static ICollider DefaultCollider = new BoxCollider(IdentityTransform.Instance(), new Vector3(0.5f));

        public static ICollider GetColliderForHlmt(H2vMap map, HaloModelTag hlmt, int damageLevel = 0)
        {
            if (map.TryGetTag(hlmt.ColliderId, out var coll) == false)
            {
                Console.WriteLine($"Couldn't find COLL[{hlmt.ColliderId.Id}]");
                return DefaultCollider;
            }

            if (coll.ColliderComponents.Length == 0
                || coll.ColliderComponents[0].DamageLevels.Length <= damageLevel
                || coll.ColliderComponents[0].DamageLevels[damageLevel].Parts.Length == 0)
            {
                Console.WriteLine($"No colliders defined in coll[{coll.Id}] for damage level {damageLevel}");
                return DefaultCollider;
            }

            var colliderMeshes = new List<Vector3[]>();

            foreach (var component in coll.ColliderComponents)
            {
                var container = component.DamageLevels[damageLevel];

                foreach(var info in container.Parts)
                {
                    colliderMeshes.Add(info.Vertices.Select(v => new Vector3(v.x, v.y, v.z)).ToArray());
                }
            }

            var collider = new ConvexModelCollider(colliderMeshes);

            return collider;
        }
    }
}