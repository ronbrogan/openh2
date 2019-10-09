﻿using OpenH2.Core.Enums.Texture;
using OpenH2.Rendering.Abstractions;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace OpenH2.Rendering.OpenGL
{
    public class OpenGLTextureBinder : ITextureBinder
    {
        private static Dictionary<TextureFormat2, (SizedInternalFormat, PixelFormat)> FormatMappings = new Dictionary<TextureFormat2, (SizedInternalFormat, PixelFormat)>
        {
            { TextureFormat2.A8, (SizedInternalFormat.R8, PixelFormat.Red)},
            { TextureFormat2.L8, (SizedInternalFormat.R8, PixelFormat.Red)},
            { TextureFormat2.A8L8, (SizedInternalFormat.Rg16, PixelFormat.Rg)},
            { TextureFormat2.U8V8, (SizedInternalFormat.Rg16, PixelFormat.Rg) },
            { TextureFormat2.A4R4G4B4, ((SizedInternalFormat)InternalFormat.Rgba4, PixelFormat.Bgra)},
            { TextureFormat2.R8G8B8, (SizedInternalFormat.Rgba8, PixelFormat.Bgra)},
            { TextureFormat2.A8R8G8B8, (SizedInternalFormat.Rgba8, PixelFormat.Bgra)},
            { TextureFormat2.DXT1, ((SizedInternalFormat)InternalFormat.CompressedRgbS3tcDxt1Ext, (PixelFormat)InternalFormat.CompressedRgbS3tcDxt1Ext) },
            { TextureFormat2.DXT23, ((SizedInternalFormat)InternalFormat.CompressedRgbaS3tcDxt3Ext, (PixelFormat)InternalFormat.CompressedRgbaS3tcDxt3Ext) },
            { TextureFormat2.DXT45, ((SizedInternalFormat)InternalFormat.CompressedRgbaS3tcDxt5Ext, (PixelFormat)InternalFormat.CompressedRgbaS3tcDxt5Ext) },
        };

        private static Dictionary<TextureFormat2, Func<int, int, int>> MipSizeFuncs = new Dictionary<TextureFormat2, Func<int, int, int>>
        {
            { TextureFormat2.A8, (w,h) => w*h},
            { TextureFormat2.L8, (w,h) => w*h},
            { TextureFormat2.A8L8, (w,h) => w*h*2},
            { TextureFormat2.A4R4G4B4, (w,h) => w*h*4},
            { TextureFormat2.R8G8B8, (w,h) => w*h*4},
            { TextureFormat2.A8R8G8B8, (w,h) => w*h*4},
            { TextureFormat2.DXT1, (w,h) => ((w + 3) / 4) * ((h + 3) / 4) * 8 },
            { TextureFormat2.DXT23, (w,h) => ((w + 3) / 4) * ((h + 3) / 4) * 16 },
            { TextureFormat2.DXT45, (w,h) => ((w + 3) / 4) * ((h + 3) / 4) * 16 }
        };

        public int Bind(string filename)
        {
            var fullPath = Path.GetFullPath(filename);

            var bmp = new Bitmap(fullPath);
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            var texAddr = Bind(data, bmp.Width, bmp.Height);
            bmp.UnlockBits(data);
            bmp.Dispose();
            return texAddr;
        }

        public int Bind(Stream textureData)
        {
            var bmp = new Bitmap(textureData);
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var texAddr = Bind(data, bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            bmp.UnlockBits(data);
            bmp.Dispose();
            return texAddr;
        }

        public int Bind(Core.Tags.BitmapTag bitm, out long handle)
        {
            var width = bitm.Width;
            var height = bitm.Height;

            if(width == 0 || height == 0)
            {
                handle = long.MaxValue;
                return int.MaxValue;
            }

            var topLod = bitm.LevelsOfDetail[0];

            GL.GenTextures(1, out int texId);
            GL.BindTexture(TextureTarget.Texture2D, texId);

            UploadMips(topLod.Data, bitm.TextureFormat, bitm.Format, width, height, bitm.MipMapCount == 0 ? bitm.MipMapCount2 : bitm.MipMapCount);

            SetCommonTextureParams();

            handle = GL.Arb.GetTextureHandle(texId);

            GL.Arb.MakeTextureHandleResident(handle);

            CheckTextureBindErrors();

            return texId;
        }

        private void UploadMips(Memory<byte> data, TextureFormat format, TextureFormat2 format2, int width, int height, int mipMaps)
        {
            int offset = 0;

            var (sizedFormat, pixelFormat) = FormatMappings[format2];
            var size = 0;

            if (mipMaps == 0)
                mipMaps = 1;

            GL.TexStorage2D(TextureTarget2d.Texture2D, mipMaps, sizedFormat, width, height);

            switch (format2)
            {
                case TextureFormat2.A8:
                {
                    var alpha = (int)PixelFormat.Alpha;
                    GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba,  new[] { alpha, alpha, alpha, alpha });
                    break;
                }

                case TextureFormat2.L8:
                {
                    var red = (int)PixelFormat.Red;
                    GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba, new[] { red, red, red, red });
                    break;
                }

                case TextureFormat2.A8L8:
                {
                    var red = (int)PixelFormat.Red;
                    var alpha = (int)PixelFormat.Alpha;
                    GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureSwizzleRgba, new[] { red, red, red, alpha });
                    break;
                }
            }

            for (var i = 0; i < mipMaps; i++)
            {
                if (width == 0)
                    width = 1;
                if (height == 0)
                    height = 1;

                size = MipSizeFuncs[format2](width, height);
                 
                // Handle corrupt images
                if(data.Length < offset || data.Length < offset+size)
                {
                    break;
                }

                byte[] bytes = data.Slice(offset, size).ToArray();

                switch (format)
                {
                    case TextureFormat.DXT1:
                    case TextureFormat.DXT23:
                    case TextureFormat.DXT45:
                        GL.CompressedTexSubImage2D(TextureTarget.Texture2D, i, 0, 0, width, height, pixelFormat, size, bytes);
                        break;
                    case TextureFormat.SixteenBit:
                    case TextureFormat.ThirtyTwoBit:
                    case TextureFormat.Monochrome:
                        GL.TexSubImage2D(TextureTarget.Texture2D, i, 0, 0, width, height, pixelFormat, PixelType.UnsignedByte, bytes);
                        break;
                }

                offset += size;
                width >>= 1;
                height >>= 1;
            }
        }

        public int Bind(BitmapData data, int width, int height, System.Drawing.Imaging.PixelFormat inputFormat = System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        {
            GL.GenTextures(1, out int texHandle);
            GL.BindTexture(TextureTarget.Texture2D, texHandle);

            var mipMapLevels = (int)Math.Floor(Math.Log(Math.Max(width, height), 2)) + 1;
            GL.TexStorage2D(TextureTarget2d.Texture2D, mipMapLevels, SizedInternalFormat.Rgba8, width, height);

            switch (inputFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                    break;

                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                default:
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
                    break;
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            SetCommonTextureParams();
            CheckTextureBindErrors();

            return texHandle;
        }

        private void SetCommonTextureParams()
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);

            float maxAniso;
            GL.GetFloat((GetPName)ExtTextureFilterAnisotropic.MaxTextureMaxAnisotropyExt, out maxAniso);
            GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, maxAniso);
        }

        private void CheckTextureBindErrors()
        {
            var error1 = GL.GetError();
            if (error1 != ErrorCode.NoError)
            {
                Console.WriteLine("-- Error {0} occured at {1}", error1, "some place in texture loader");
            }
        }
    }
}
