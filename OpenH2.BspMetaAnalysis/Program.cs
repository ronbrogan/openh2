﻿using OpenH2.Core.Factories;
using System.IO;
using System.Linq;
using System.Text;
using OpenH2.Translation;
using OpenH2.Translation.TagData;
using System.Drawing;
using System;
using System.Collections.Generic;

namespace OpenH2.BspMetaAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            var mapNames = new string[]
            {
                @"D:\Halo 2 Vista Original Maps\ascension.map"
            };

            var metas = mapNames.Select(s =>
            {
                var fac = new SceneFactory();
                using (var fs = new FileStream(s, FileMode.Open))
                    return fac.FromFile(fs);
            }).ToDictionary(s => s.Header.Name, s => s);

            var ascension = metas["ascension"];

            var translator = new TagTranslator(ascension);

            var bsps = translator.GetAll<BspTagData>().ToList();

            for(var i = 0; i < bsps.Count(); i++)
            {
                var bsp = bsps[i];

                var mtl = CreateMtlFileForBsp(bsp);
                File.WriteAllText($"D:\\bsp_{i}.mtl", mtl);

                var obj = CreatObjFileForBsp(bsp);
                File.WriteAllText($"D:\\bsp_{i}.obj", $"usemtl bsp_{i}.mtl\r\n" + obj);
            }
        }

        public static string CreateMtlFileForBsp(BspTagData tag)
        {
            var sb = new StringBuilder();

            var alreadyGenerated = new HashSet<int>();

            for (var i = 0; i < tag.RenderModels.Length; i++)
            {
                var model = tag.RenderModels[i];

                foreach (var mesh in model.Meshes)
                {
                    var matId = mesh.MaterialIdentifier + 1;

                    if (alreadyGenerated.Contains(matId))
                        continue;

                    var color = GenerateRandomColor();

                    sb.AppendLine($"newmtl {matId}");
                    sb.AppendLine($"Kd {(color.R / 255f).ToString("0.000000")} {(color.G / 255f).ToString("0.000000")} {(color.B / 255f).ToString("0.000000")}");
                    sb.AppendLine($"Ka {(color.R / 255f).ToString("0.000000")} {(color.G / 255f).ToString("0.000000")} {(color.B / 255f).ToString("0.000000")}");
                    sb.AppendLine("Ks 1.000 1.000 1.000");
                    sb.AppendLine("Ns 10.000");
                    sb.AppendLine("");

                    alreadyGenerated.Add(matId);
                }
            }


            return sb.ToString();
        }

        public static Color GenerateRandomColor()
        {
            var mix = Color.Gray;

            Random random = new Random();
            int red = random.Next(256);
            int green = random.Next(256);
            int blue = random.Next(256);

            // mix the color
            if (mix != null)
            {
                red = (red + mix.R) / 2;
                green = (green + mix.G) / 2;
                blue = (blue + mix.B) / 2;
            }

            Color color = Color.FromArgb(255, red, green, blue);
            return color;
        }

        public static string CreatObjFileForBsp(BspTagData tag)
        {
            var sb = new StringBuilder();

            var vertsWritten = 1;

            for(var i = 0; i < tag.RenderModels.Length; i++)
            {
                var model = tag.RenderModels[i];
                sb.AppendLine($"o BspChunk.{i}");

                var verts = model.Meshes.First().Verticies;

                foreach (var vert in verts)
                {
                    sb.AppendLine($"v {vert.Position.X.ToString("0.000000")} {vert.Position.Y.ToString("0.000000")} {vert.Position.Z.ToString("0.000000")}");
                }

                foreach (var vert in verts)
                {
                    sb.AppendLine($"vt {vert.TexCoords.X.ToString("0.000000")} {vert.TexCoords.Y.ToString("0.000000")}");
                }

                foreach (var vert in verts)
                {
                    sb.AppendLine($"vn {vert.Normal.X.ToString("0.000000")} {vert.Normal.Y.ToString("0.000000")} {vert.Normal.Z.ToString("0.000000")}");
                }


                foreach (var mesh in model.Meshes)
                {
                    var matId = mesh.MaterialIdentifier+1;

                    sb.AppendLine($"g BspChunk.{i}.{matId}");
                    sb.AppendLine($"usemtl {matId}");
                    
                    for(var j = 0; j < mesh.Indicies.Length; j+=3)
                    {
                        var indicies = (mesh.Indicies[j], mesh.Indicies[j+1], mesh.Indicies[j+2]);

                        sb.Append("f");
                        sb.Append($" {indicies.Item1 + vertsWritten}/{indicies.Item1 + vertsWritten}/{indicies.Item1 + vertsWritten}");
                        sb.Append($" {indicies.Item2 + vertsWritten}/{indicies.Item2 + vertsWritten}/{indicies.Item2 + vertsWritten}");
                        sb.Append($" {indicies.Item3 + vertsWritten}/{indicies.Item3 + vertsWritten}/{indicies.Item3 + vertsWritten}");

                        sb.AppendLine("");
                    }
                }

                sb.AppendLine();

                vertsWritten += verts.Length;
            }

            return sb.ToString();
        }
    }
}
