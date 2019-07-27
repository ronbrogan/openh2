﻿using System;
using OpenH2.Core.Tags;
using OpenH2.Core.Types;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenH2.Core.Extensions;
using System.Collections.Concurrent;
using OpenH2.Foundation;

namespace OpenH2.Translation.TagData.Processors
{
    public class BspTagDataProcessor
    {

        public static BspTagData ProcessTag(BaseTag tag)
        {
            var bsp = tag as Bsp;

            if (bsp == null)
                throw new ArgumentException("Tag must be a Bsp", nameof(tag));

            var tagData = new BspTagData(bsp);
            tagData.RenderModels = new BspTagData.RenderModel[bsp.RenderChunks.Length];

            for(var c = 0; c < bsp.RenderChunks.Length; c++)
            {
                var chunk = bsp.RenderChunks[c];
                var verts = ProcessVerticies(chunk);

                var meshes = new List<Mesh>();


                if (chunk.Resources.Length < 9)
                {
                    // TODO investigate when differing amount of resources
                    // Skip if we don't have the right data setup
                    continue;
                }

                var meshResource = chunk.Resources[1];
                var meshData = meshResource.Data.Span;

                // Process face data
                var faceResource = chunk.Resources[2];
                var faceData = faceResource.Data.Span;


                var lastMeshId = 0;
                List<int> currentIndicies = new List<int>(chunk.TriangleCount * 3);

                // Entry size is 8
                for (var i = 0; i < meshResource.Size / 8; i++)
                {
                    var start = i * 8;
                    var indexStart = meshData.ReadUInt16At(start);
                    var indexCount = meshData.ReadUInt16At(start + 2);
                    var meshId = meshData.ReadInt16At(start + 6);

                    if(currentIndicies.Any() && meshId != lastMeshId)
                    {
                        var mesh = new Mesh();
                        mesh.Verticies = verts;
                        mesh.Indicies = currentIndicies.ToArray();
                        mesh.MaterialIdentifier = lastMeshId;
                        meshes.Add(mesh);

                        currentIndicies.Clear();
                    }

                    lastMeshId = meshId;

                    for (var s = 0; s < indexCount; s++)
                    {
                        var indicesStart = (indexStart * 2) + (s * 2);

                        currentIndicies.Add(faceData.ReadUInt16At(indicesStart));
                    }
                }

                if (currentIndicies.Any())
                {
                    var mesh = new Mesh();
                    mesh.Verticies = verts;
                    mesh.Indicies = currentIndicies.ToArray();
                    mesh.MaterialIdentifier = lastMeshId;
                    meshes.Add(mesh);

                    currentIndicies.Clear();
                }

                var model = new BspTagData.RenderModel();
                model.Meshes = meshes;
                tagData.RenderModels[c] = model;
            }

            return tagData;
        }

        private static VertexFormat[] ProcessVerticies(Bsp.RenderChunk chunk)
        {
            var verts = new VertexFormat[chunk.VertexCount];

            var posResouce = chunk.Resources[6];
            var posData = posResouce.Data.Span;

            for (var i = 0; i < chunk.VertexCount; i++)
            {
                var vert = new VertexFormat();

                vert.Position = posData.ReadVec3At(i * 12);

                verts[i] = vert;
            }

            var texResouce = chunk.Resources[7];
            var texData = texResouce.Data.Span;

            for (var i = 0; i < chunk.VertexCount; i++)
            {
                var vert = verts[i];

                vert.TexCoords = texData.ReadVec2At(i * 8);

                verts[i] = vert;
            }

            var tbnResouce = chunk.Resources[8];
            var tbnData = tbnResouce.Data.Span;

            for (var i = 0; i < chunk.VertexCount; i++)
            {
                var vert = verts[i];

                var start = i * 36;
                vert.Tangent = tbnData.ReadVec3At(start);
                vert.Bitangent = tbnData.ReadVec3At(start + 12);
                vert.Normal = tbnData.ReadVec3At(start + 24);

                verts[i] = vert;
            }

            return verts;
        }

        public BspTagData ProcessCollisionGeometry(Bsp bsp, BspTagData tagData)
        {
            var block = bsp.CollisionInfos.First();

            var faces = new List<int[]>();

            for (var i = 0; i < block.Faces.Length; i++)
            {
                var face = block.Faces[i];

                var faceVerts = new List<int>(8);

                var currentEdge = block.HalfEdges[face.FirstEdge];

                while (true)
                {
                    int fromVert;
                    int toVert;
                    int nextEdge;

                    if (currentEdge.Face0 == i)
                    {
                        fromVert = currentEdge.Vertex0;
                        toVert = currentEdge.Vertex1;
                        nextEdge = currentEdge.NextEdge;
                    }
                    else
                    {
                        fromVert = currentEdge.Vertex1;
                        toVert = currentEdge.Vertex0;
                        nextEdge = currentEdge.PrevEdge;
                    }

                    if (faceVerts.Count == 0)
                    {
                        faceVerts.Add(fromVert);
                    }

                    if (faceVerts[0] == toVert)
                        break;

                    faceVerts.Add(toVert);

                    currentEdge = block.HalfEdges[nextEdge];
                }

                faces.Add(faceVerts.ToArray());
            }

            //tagData.Faces = faces.ToArray();

            //tagData.Verticies = block.Verticies
            //    .Select(r => new Vector3(r.x, r.y, r.z))
            //    .Select(v => new Vertex() { Position = v })
            //    .ToArray();

            return tagData;
        }
    }
}
