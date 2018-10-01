﻿using System;
using System.Buffers;
using System.Text;

namespace OpenH2.Core.Extensions
{
    public static class SpanByteExtensions
    {
        public static string ToStringFromNullTerminated(this Span<byte> data)
        {
            var builder = new StringBuilder(data.Length);

            var current = 0;
            while (true)
            {
                if (current == data.Length || data[current] == 0b0)
                {
                    break;
                }

                builder.Append((char)data[current]);
                current++;
            }

            return builder.ToString();
        }

        public static string ReadStringFrom(this Span<byte> data, int offset, int length)
        {
            return data.Slice(offset, length).ToStringFromNullTerminated();
        }

        public static string ReadStringStarting(this Span<byte> data, int offset)
        {
            var builder = new StringBuilder(32);

            var current = offset;
            while (true)
            {
                if (data[current] == 0b0)
                {
                    break;
                }

                builder.Append((char)data[current]);
                current++;
            }

            return builder.ToString();
        }

        public static short ReadInt16At(this Span<byte> data, int offset)
        {
            var bytes = data.Slice(offset, 2);

            short value = 0;
            var shift = 0;

            foreach (short b in bytes)
            {
                // Shift bits into correct position and add into value
                value = (short)(value | (b << (shift * 8)));

                shift++;
            }

            return value;
            
        }

        public static int ReadInt32At(this Span<byte> data, int offset)
        {
            var bytes = data.Slice(offset, 4);

            var value = 0;
            var shift = 0;

            foreach (int b in bytes)
            {
                // Shift bits into correct position and add into value
                value = value | (b << (shift * 8));

                shift++;
            }

            return value;
        }

        public static uint ReadUInt32At(this Span<byte> data, int offset)
        {
            var bytes = data.Slice(offset, 4);

            uint value = 0;
            var shift = 0;

            foreach (uint b in bytes)
            {
                // Shift bits into correct position and add into value
                value = value | (b << (shift * 8));

                shift++;
            }

            return value;
        }
    }
}