﻿using OpenH2.Core.Extensions;
using OpenH2.Core.Factories;
using OpenH2.Core.Maps;
using OpenH2.Core.Offsets;
using OpenH2.Core.Parsing;
using OpenH2.Core.Tags;
using OpenH2.Foundation.Logging;
using OpenBlam.Serialization;
using OpenBlam.Serialization.Layout;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Numerics;

namespace OpenH2.Core.Patching
{
    public class TagPatcher
    {
        private readonly IH2Map originalMap;
        private readonly Stream mapToPatch;

        private Dictionary<Type, Action<Stream, int, JsonElement>> DataWriters = new Dictionary<Type, Action<Stream, int, JsonElement>>
        {
            { typeof(byte), (s,o,e) => s.WriteByteAt(o, e.GetByte()) },
            { typeof(short), (s,o,e) => s.WriteInt16At(o, e.GetInt16()) },
            { typeof(ushort), (s,o,e) => s.WriteUInt16At(o, e.GetUInt16()) },
            { typeof(int), (s,o,e) => s.WriteInt32At(o, e.GetInt32()) },
            { typeof(uint), (s,o,e) => s.WriteUInt32At(o, e.GetUInt32()) },
            { typeof(float), (s,o,e) => s.WriteFloatAt(o, e.GetSingle()) },
        };

        public TagPatcher(IH2Map originalMap, Stream mapToPatch)
        {
            this.originalMap = originalMap;
            this.mapToPatch = mapToPatch;

            this.DataWriters.Add(typeof(ITagRef), WriteTagRef);
            this.DataWriters.Add(typeof(TagRef), WriteTagRef);
            this.DataWriters.Add(typeof(TagRef<>), WriteTagRef);
            this.DataWriters.Add(typeof(Vector3), WriteVec3);
        }

        public void Apply(TagPatch patchSet)
        {
            var tagId = GetTagIdFromString(patchSet.TagName);

            var tagInfo = originalMap.TagIndex[tagId];

            foreach(var bin in patchSet.BinaryPatches)
            {
                PatchBinaryData(tagInfo, bin);
            }

            foreach(var prop in patchSet.PropertyPatches)
            {
                PatchProperty(tagInfo, prop);
            }
        }

        public void PatchBinaryData(TagIndexEntry tagInfo, TagBinaryPatch bin)
        {
            var patchOffset = tagInfo.Offset.Value + bin.RelativeOffset;

            Logger.Log(bin.Data.Length + "bytes @ " + bin.RelativeOffset, Logger.Color.Cyan);

            this.mapToPatch.Position = patchOffset;
            this.mapToPatch.Write(bin.Data);
        }

        public void PatchProperty(TagIndexEntry tagInfo, TagPropertyPatch patch)
        {
            var propertyInfo = ResolvePropertyInfo(tagInfo, patch.PropertySelector);

            Logger.Log(propertyInfo.PropertyType.ToString() + " @ " + propertyInfo.RelativeOffset, Logger.Color.Magenta);

            WritePropertyValue(tagInfo, propertyInfo, patch);
        }

        private ResolvedTagPropertyInfo ResolvePropertyInfo(TagIndexEntry tagInfo, string propertyPath)
        {
            var topTagType = TagFactory.GetTypeForTag(tagInfo.Tag);

            var steps = PropertyAccessorParser.ExtractProperties(propertyPath);

            var offset = 0;

            var stepType = topTagType;

            foreach(var step in steps)
            {
                var prop = stepType.GetProperty(step.PropertyName);

                if(prop == null)
                {
                    throw new Exception($"Couldn't find property '{step.PropertyName}' on type '{stepType}'");
                }

                if (step.AccessType == PropertyAccessorParser.PropertyAccessType.Normal)
                {
                    offset += BlamSerializer.StartsAt(stepType, step.PropertyName);
                    stepType = prop.PropertyType;
                }
                else if(step.AccessType == PropertyAccessorParser.PropertyAccessType.ElementAccess)
                {
                    if(prop.PropertyType.IsArray == false || step.ElementArgument is not int)
                    {
                        throw new NotSupportedException("Only arrays are currently supported for element access");
                    }

                    var elementSize = BlamSerializer.SizeOf(prop.PropertyType.GetElementType());
                    var elementOffset = (elementSize * ((int)step.ElementArgument));

                    if (prop.GetCustomAttribute<ReferenceArrayAttribute>() != null)
                    {
                        var startsAt = BlamSerializer.StartsAt(stepType, step.PropertyName);

                        // Read element array base offset
                        var baseOffset = new SecondaryOffset(this.originalMap, this.mapToPatch.ReadInt32At(tagInfo.Offset.Value + offset + startsAt + 4));

                        // baseOffset is the absolute offset, need to subtract tag offset and prior property offsets to get relative
                        offset += baseOffset.Value - tagInfo.Offset.Value - offset + elementOffset;
                    }
                    else if(prop.GetCustomAttribute<PrimitiveArrayAttribute>() != null)
                    {
                        offset += elementOffset;
                    }
                    else
                    {
                        throw new Exception("Only primitive and reference arrays are supported");
                    }

                    stepType = prop.PropertyType.GetElementType();
                }
            }

            return new ResolvedTagPropertyInfo()
            {
                RelativeOffset = offset,
                PropertyType = stepType
            };
        }

        private void WritePropertyValue(TagIndexEntry tagInfo, ResolvedTagPropertyInfo info, TagPropertyPatch patch)
        {
            if(info.RelativeOffset < 0)
            {
                Debugger.Break();
            }

            var propType = info.PropertyType;

            if(propType.IsGenericType)
            {
                propType = propType.GetGenericTypeDefinition();
            }

            if(propType.IsEnum)
            {
                propType = propType.GetEnumUnderlyingType();
            }

            if(this.DataWriters.TryGetValue(propType, out var writer))
            {
                writer(this.mapToPatch, tagInfo.Offset.Value + info.RelativeOffset, patch.Value);
            }
            else
            {
                Console.WriteLine($"Writing '{info.PropertyType}' values is not configured, skipping");
            }
        }

        public void WriteTagRef(Stream data, int offset, JsonElement value)
        {
            uint tagId = uint.MaxValue;

            if(value.ValueKind == JsonValueKind.Number)
            {
                tagId = value.GetUInt32();
            }
            else if(value.ValueKind == JsonValueKind.String)
            {
                tagId = GetTagIdFromString(value.GetString());
            }
            else
            {
                throw new Exception($"Tag ref using a '{value.ValueKind}' value ({value}) is not supported");
            }

            data.WriteUInt32At(offset, tagId);
        }

        public void WriteVec3(Stream data, int offset, JsonElement value)
        {
            if(value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 3)
            {
                throw new Exception("Vector3 values must be a float array of three elements");
            }

            var i = 0;
            Span<float> floats = stackalloc float[3];

            foreach(var val in value.EnumerateArray())
            {
                if(val.ValueKind != JsonValueKind.Number)
                {
                    throw new Exception("Vector elements must be numeric");
                }

                data.WriteFloatAt(offset + (i * 4), val.GetSingle());
                i++;
            }
        }

        public void WriteByteArray(Stream data, int offset, JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.String)
            {
                throw new Exception("Byte array values must be a hex string");
            }

            var hex = value.GetString();

            if (hex.Length % 2 == 1)
                throw new Exception("Hex data must have an even number of characters");

            var arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            data.Write(arr);

            int GetHexVal(char hex)
            {
                int val = (int)hex;
                //For uppercase A-F letters:
                //return val - (val < 58 ? 48 : 55);
                //For lowercase a-f letters:
                //return val - (val < 58 ? 48 : 87);
                //Or the two combined, but a bit slower:
                return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
            }
        }

        private uint GetTagIdFromString(string tagNameWithExtension)
        {
            var tagName = tagNameWithExtension.Substring(0, tagNameWithExtension.IndexOf('.'));
            var tagFourCc = Path.GetExtension(tagNameWithExtension).Substring(1);

            if (Enum.TryParse(typeof(TagName), tagFourCc, out var tagType) == false)
            {
                throw new Exception("Tag names in patch must end with a valid extension");
            }

            if (originalMap.TryFindTagId((TagName)tagType, tagName, out var tagId) == false)
            {
                throw new Exception($"Tag '{tagNameWithExtension}' not found in map");
            }

            return tagId;
        }

        private class ResolvedTagPropertyInfo
        {
            public int RelativeOffset { get; set; }

            public Type PropertyType { get; set; }
        }
    }
}
