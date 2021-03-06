﻿using OpenH2.Core.Tags;
using OpenH2.Foundation;
using OpenH2.Rendering.Abstractions;
using OpenH2.Rendering.Shaders;
using OpenH2.Rendering.Shaders.Generic;
using OpenH2.Rendering.Shaders.Skybox;
using OpenH2.Rendering.Shaders.Wireframe;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenH2.Rendering.OpenGL
{
    public partial class OpenGLGraphicsAdapter : IGraphicsAdapter
    {
        // Uniform index used in shaders
        private class UniformIndices
        {
            public const int Global = 0;
            public const int Shader = 1;
            public const int Transform = 2;
        }

        private Dictionary<IMaterial<BitmapTag>, MaterialBindings> boundMaterials = new Dictionary<IMaterial<BitmapTag>, MaterialBindings>();
        private ITextureBinder textureBinder = new OpenGLTextureBinder();

        private Shader activeShader;
        private Dictionary<Shader, int> shaderHandles = new Dictionary<Shader, int>();

        private int GlobalUniformHandle;
        private GlobalUniform GlobalUniform;

        private int LightingUniformHandle;
        private LightingUniform LightingUniform;

        private int TransformUniformHandle;

        public OpenGLGraphicsAdapter()
        {
        }

        public void BeginFrame(GlobalUniform global)
        {
            GlobalUniform = global;
            SetupGlobalUniform();

            LightingUniform = new LightingUniform() { PointLights = new PointLightUniform[0] };
        }

        public void UseShader(Shader shader)
        {
            if (shaderHandles.TryGetValue(shader, out var handle) == false)
            {
                handle = ShaderCompiler.CreateShader(shader);
                shaderHandles[shader] = handle;
            }

            GL.UseProgram(handle);

            activeShader = shader;

            if(shaderBeginActions.TryGetValue(shader, out var action))
            {
                action();
            }
        }

        public void SetSunLight(Vector3 sunDirection)
        {
            //LightingUniform.SunDirection = new Vector4(sunDirection, 0f);
        }

        public void AddLight(PointLight light)
        {
            var newLights = new List<PointLightUniform>(LightingUniform.PointLights);
            newLights.Add(new PointLightUniform(light));

            LightingUniform.PointLights = newLights.ToArray();
        }

        public int UploadModel(Model<BitmapTag> model, out DrawCommand[] meshCommands)
        {
            int vao, vbo, ibo;

            GL.GenVertexArrays(1, out vao);
            GL.BindVertexArray(vao);

            var vertCount = model.Meshes.Sum(m => m.Verticies.Length);
            var indxCount = model.Meshes.Sum(m => m.Indicies.Length);

            meshCommands = new DrawCommand[model.Meshes.Length];
            var vertices = new VertexFormat[vertCount];
            var indices = new int[indxCount];

            var currentVert = 0;
            var currentIndx = 0;

            for(var i = 0; i < model.Meshes.Length; i++)
            {
                var mesh = model.Meshes[i];

                var command = new DrawCommand(mesh)
                {
                    VaoHandle = vao,
                    IndexBase = currentIndx,
                    VertexBase = currentVert,
                    ColorChangeData = model.ColorChangeData
                };

                Array.Copy(mesh.Verticies, 0, vertices, currentVert, mesh.Verticies.Length);
                currentVert += mesh.Verticies.Length;

                Array.Copy(mesh.Indicies, 0, indices, currentIndx, mesh.Indicies.Length);
                currentIndx += mesh.Indicies.Length;

                meshCommands[i] = command;
            }

            GL.GenBuffers(1, out vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * VertexFormat.Size), vertices, BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out ibo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indices.Length * sizeof(int)), indices, BufferUsageHint.StaticDraw);

            SetupVertexFormatAttributes();

            return vao;
        }

        public void EndFrame()
        {
        }

        public MaterialBindings SetupTextures(IMaterial<BitmapTag> material)
        {
            if (boundMaterials.TryGetValue(material, out var bindings))
            {
                return bindings;
            }

            bindings = new MaterialBindings();

            if (material.DiffuseMap != null)
            {
                textureBinder.GetOrBind(material.DiffuseMap, out var diffuseHandle);
                bindings.DiffuseHandle = diffuseHandle;
            }

            if (material.DetailMap1 != null)
            {
                textureBinder.GetOrBind(material.DetailMap1, out var handle);
                bindings.Detail1Handle = handle;
            }

            if (material.DetailMap2 != null)
            {
                textureBinder.GetOrBind(material.DetailMap2, out var handle);
                bindings.Detail2Handle = handle;
            }

            if (material.ColorChangeMask != null)
            {
                textureBinder.GetOrBind(material.ColorChangeMask, out var handle);
                bindings.ColorChangeHandle = handle;
            }

            if (material.AlphaMap != null)
            {
                textureBinder.GetOrBind(material.AlphaMap, out var alphaHandle);
                bindings.AlphaHandle = alphaHandle;
            }

            if (material.EmissiveMap != null)
            {
                textureBinder.GetOrBind(material.EmissiveMap, out var emissiveHandle);
                bindings.EmissiveHandle = emissiveHandle;
            }

            if (material.NormalMap != null)
            {
                textureBinder.GetOrBind(material.NormalMap, out var handle);
                bindings.NormalHandle = handle;
            }

            boundMaterials.Add(material, bindings);

            return bindings;
        }

        public void UseTransform(Matrix4x4 transform)
        {
            var success = Matrix4x4.Invert(transform, out var inverted);
            Debug.Assert(success);

            SetupTransformUniform(new TransformUniform(transform, inverted));
        }

        public void DrawMeshes(DrawCommand[] commands)
        {
            for(var i = 0; i < commands.Length; i++)
            {
                ref DrawCommand command = ref commands[i];

                SetupLighting();

                BindOrCreateShaderUniform(ref command);
                BindVao(ref command);

                var primitiveType = command.ElementType switch
                {
                    MeshElementType.TriangleList => PrimitiveType.Triangles,
                    MeshElementType.TriangleStrip => PrimitiveType.TriangleStrip,
                    MeshElementType.TriangleStripDecal => PrimitiveType.TriangleStrip,
                    MeshElementType.Point => PrimitiveType.Points,
                    _ => PrimitiveType.Triangles
                };

                GL.DrawElementsBaseVertex(primitiveType,
                    command.IndiciesCount,
                    DrawElementsType.UnsignedInt,
                    (IntPtr)(command.IndexBase * sizeof(int)), 
                    command.VertexBase);
            }
        }

        private Dictionary<IMaterial<BitmapTag>, int[]> MaterialUniforms = new Dictionary<IMaterial<BitmapTag>, int[]>();

        private int currentlyBoundShaderUniform = -1;
        private void BindOrCreateShaderUniform(ref DrawCommand command)
        {
            // If the uniform was already buffered, we'll just reuse that buffered uniform
            // Currently these material uniforms never change at runtime - if this changes
            // there will have to be some sort of invalidation to ensure they're updated
            if (command.ShaderUniformHandle != -1)
            {
                if(command.ShaderUniformHandle != currentlyBoundShaderUniform)
                {
                    GL.BindBufferBase(BufferRangeTarget.UniformBuffer, UniformIndices.Shader, command.ShaderUniformHandle);
                }
            }
            else
            {
                if (MaterialUniforms.TryGetValue(command.Mesh.Material, out var uniforms) == false)
                {
                    uniforms = new int[(int)Shader.MAX_VALUE];
                    MaterialUniforms[command.Mesh.Material] = uniforms;
                }

                var bindings = SetupTextures(command.Mesh.Material);
                command.ShaderUniformHandle = GenerateShaderUniform(command, bindings);
                uniforms[(int)activeShader] = command.ShaderUniformHandle;
            }
        }

        private int GenerateShaderUniform(DrawCommand command, MaterialBindings bindings)
        {
            var mesh = command.Mesh;
            var existingUniformHandle = 0;

            switch (activeShader)
            {
                case Shader.Skybox:
                    BindAndBufferShaderUniform(
                        activeShader,
                        new SkyboxUniform(mesh.Material, bindings),
                        SkyboxUniform.Size,
                        out existingUniformHandle);
                    break;
                case Shader.Generic:
                    BindAndBufferShaderUniform(
                        activeShader,
                        new GenericUniform(mesh, command.ColorChangeData, bindings),
                        GenericUniform.Size,
                        out existingUniformHandle);

                    break;
                case Shader.Wireframe:
                    BindAndBufferShaderUniform(
                        activeShader,
                        new WireframeUniform(mesh.Material),
                        WireframeUniform.Size,
                        out existingUniformHandle);
                    break;
                case Shader.Pointviz:
                case Shader.TextureViewer:
                    break;
            }

            return existingUniformHandle;
        }

        private int currentVao = -1;
        private void BindVao(ref DrawCommand command)
        {
            Debug.Assert(command.VaoHandle != -1);

            if (currentVao != command.VaoHandle)
            {
                GL.BindVertexArray(command.VaoHandle);
                currentVao = command.VaoHandle;
            }
        }

        private static void SetupVertexFormatAttributes()
        {
            // Attributes for VertexFormat.Position
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, VertexFormat.Size, 0);

            // Attributes for VertexFormat.TexCoords
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, VertexFormat.Size, 12);

            // Attributes for VertexFormat.Normal
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, VertexFormat.Size, 20);

            // Attributes for VertexFormat.Tangent
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 3, VertexAttribPointerType.Float, false, VertexFormat.Size, 32);

            // Attributes for VertexFormat.Bitangent
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 3, VertexAttribPointerType.Float, false, VertexFormat.Size, 44);
        }

        private void SetupGlobalUniform()
        {
            if (GlobalUniformHandle == default(int))
            {
                GL.GenBuffers(1, out GlobalUniformHandle);
                GL.BindBuffer(BufferTarget.UniformBuffer, GlobalUniformHandle);
                GL.BufferData(BufferTarget.UniformBuffer, GlobalUniform.Size, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }
            else
            {
                GL.BindBuffer(BufferTarget.UniformBuffer, GlobalUniformHandle);
            }

            GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, GlobalUniform.Size, ref GlobalUniform);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, UniformIndices.Global, GlobalUniformHandle);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        private void BindAndBufferShaderUniform<T>(Shader shader, T uniform, int size, out int handle) where T : struct
        {
            GL.GenBuffers(1, out handle);
            GL.BindBuffer(BufferTarget.UniformBuffer, handle);
            GL.BufferData(BufferTarget.UniformBuffer, size, ref uniform, BufferUsageHint.DynamicDraw);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, UniformIndices.Shader, handle);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        private void SetupTransformUniform(TransformUniform transform)
        {
            if (TransformUniformHandle == default(int))
            {
                GL.GenBuffers(1, out TransformUniformHandle);
                GL.BindBuffer(BufferTarget.UniformBuffer, TransformUniformHandle);
                GL.BufferData(BufferTarget.UniformBuffer, GlobalUniform.Size, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }
            else
            {
                GL.BindBuffer(BufferTarget.UniformBuffer, TransformUniformHandle);
            }

            GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, TransformUniform.Size, ref transform);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, UniformIndices.Transform, TransformUniformHandle);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        public void SetupLighting()
        {
            if (LightingUniformHandle == default(int))
            {
                GL.GenBuffers(1, out LightingUniformHandle);
                GL.BindBuffer(BufferTarget.UniformBuffer, LightingUniformHandle);
                GL.BufferData(BufferTarget.UniformBuffer, 320, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }
            else
            {
                GL.BindBuffer(BufferTarget.UniformBuffer, LightingUniformHandle);
            }

            GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, 320, LightingUniform.PointLights);

            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 3, LightingUniformHandle);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        private Dictionary<Shader, Action> shaderBeginActions = new Dictionary<Shader, Action>()
        {
            { Shader.Skybox, () => {
                GL.Disable(EnableCap.DepthTest);
            }},
            { Shader.Generic, () => {
                GL.Enable(EnableCap.DepthTest);
            }}
        };

        private Dictionary<Shader, Action> shaderEndActions = new Dictionary<Shader, Action>()
        {
            
        };
    }
}
