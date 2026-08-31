// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Text;

namespace CommunityToolkit.WinUI.Lottie.CompDataFlatbuffer
{
    /// <summary>
    /// A minimal reader for the FlatBuffers binary wire format. All accesses are
    /// bounds checked; a malformed buffer causes a <see cref="FlatBufferFormatException"/>
    /// rather than undefined behavior.
    /// </summary>
    readonly struct FlatBufferTable
    {
        readonly byte[] _buffer;
        readonly int _position;

        internal FlatBufferTable(byte[] buffer, int position)
        {
            _buffer = buffer;
            _position = position;
        }

        internal bool IsNull => _buffer is null;

        /// <summary>
        /// Gets the root table of a buffer, checking its file identifier.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="fileIdentifier">The expected 4 character file identifier.</param>
        /// <returns>The root table.</returns>
        internal static FlatBufferTable GetRoot(byte[] buffer, string fileIdentifier)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length < 8)
            {
                throw new FlatBufferFormatException("Buffer is too small.");
            }

            for (var i = 0; i < 4; i++)
            {
                if (buffer[4 + i] != (byte)fileIdentifier[i])
                {
                    throw new FlatBufferFormatException("Unrecognized file identifier.");
                }
            }

            var rootOffset = ReadInt32(buffer, 0);
            return new FlatBufferTable(buffer, CheckedAdd(buffer, 0, rootOffset));
        }

        /// <summary>
        /// Returns the absolute position of the data of the field with the given
        /// id, or 0 if the field is not present.
        /// </summary>
        /// <param name="field">The id of the field.</param>
        /// <returns>The position of the field's data, or 0.</returns>
        internal int FieldPosition(int field)
        {
            var vtable = _position - ReadInt32(_buffer, _position);
            if (vtable < 0 || vtable + 4 > _buffer.Length)
            {
                throw new FlatBufferFormatException("Invalid vtable offset.");
            }

            var vtableSize = ReadUInt16(_buffer, vtable);
            var fieldIndex = 4 + (field * 2);
            if (fieldIndex + 2 > vtableSize)
            {
                return 0;
            }

            var fieldOffset = ReadUInt16(_buffer, vtable + fieldIndex);
            if (fieldOffset == 0)
            {
                return 0;
            }

            var result = _position + fieldOffset;
            if (result < 0 || result >= _buffer.Length)
            {
                throw new FlatBufferFormatException("Field is outside the buffer.");
            }

            return result;
        }

        internal byte GetUInt8(int field, byte defaultValue)
        {
            var position = FieldPosition(field);
            return position == 0 ? defaultValue : _buffer[position];
        }

        internal byte? GetOptionalUInt8(int field)
        {
            var position = FieldPosition(field);
            return position == 0 ? (byte?)null : _buffer[position];
        }

        internal bool GetBool(int field, bool defaultValue)
            => GetUInt8(field, (byte)(defaultValue ? 1 : 0)) != 0;

        internal bool? GetOptionalBool(int field)
        {
            var value = GetOptionalUInt8(field);
            return value.HasValue ? value.Value != 0 : (bool?)null;
        }

        internal ushort GetUInt16(int field, ushort defaultValue)
        {
            var position = FieldPosition(field);
            return position == 0 ? defaultValue : ReadUInt16(_buffer, position);
        }

        internal int GetInt32(int field, int defaultValue)
        {
            var position = FieldPosition(field);
            return position == 0 ? defaultValue : ReadInt32(_buffer, position);
        }

        internal int? GetOptionalInt32(int field)
        {
            var position = FieldPosition(field);
            return position == 0 ? (int?)null : ReadInt32(_buffer, position);
        }

        internal uint GetUInt32(int field, uint defaultValue)
        {
            var position = FieldPosition(field);
            return position == 0 ? defaultValue : unchecked((uint)ReadInt32(_buffer, position));
        }

        internal long GetInt64(int field, long defaultValue)
        {
            var position = FieldPosition(field);
            if (position == 0)
            {
                return defaultValue;
            }

            var low = unchecked((uint)ReadInt32(_buffer, position));
            var high = ReadInt32(_buffer, position + 4);
            return unchecked((long)(((ulong)(uint)high << 32) | low));
        }

        internal float GetFloat(int field, float defaultValue)
        {
            var position = FieldPosition(field);
            return position == 0 ? defaultValue : BitConverter.Int32BitsToSingle(ReadInt32(_buffer, position));
        }

        internal float? GetOptionalFloat(int field)
        {
            var position = FieldPosition(field);
            return position == 0 ? (float?)null : BitConverter.Int32BitsToSingle(ReadInt32(_buffer, position));
        }

        /// <summary>
        /// Returns the position of an inline struct field, or 0 if not present.
        /// Use the ReadStructFloat and ReadStructUInt8 methods to read its fields.
        /// </summary>
        /// <param name="field">The id of the field.</param>
        /// <returns>The position of the struct, or 0.</returns>
        internal int GetStructPosition(int field) => FieldPosition(field);

        internal float ReadStructFloat(int structPosition, int offset)
            => BitConverter.Int32BitsToSingle(ReadInt32(_buffer, structPosition + offset));

        internal byte ReadStructUInt8(int structPosition, int offset)
        {
            if (structPosition + offset >= _buffer.Length)
            {
                throw new FlatBufferFormatException("Struct is outside the buffer.");
            }

            return _buffer[structPosition + offset];
        }

        internal FlatBufferTable GetTable(int field)
        {
            var position = FieldPosition(field);
            return position == 0
                ? default
                : new FlatBufferTable(_buffer, CheckedAdd(_buffer, position, ReadInt32(_buffer, position)));
        }

        internal string? GetString(int field)
        {
            var position = FieldPosition(field);
            if (position == 0)
            {
                return null;
            }

            var stringPosition = CheckedAdd(_buffer, position, ReadInt32(_buffer, position));
            var length = ReadInt32(_buffer, stringPosition);
            if (length < 0 || stringPosition + 4 + length > _buffer.Length)
            {
                throw new FlatBufferFormatException("String is outside the buffer.");
            }

            return Encoding.UTF8.GetString(_buffer, stringPosition + 4, length);
        }

        /// <summary>
        /// Returns the number of elements in a vector field.
        /// </summary>
        /// <param name="field">The id of the field.</param>
        /// <returns>The number of elements, or 0 if the field is not present.</returns>
        internal int VectorLength(int field)
        {
            var position = FieldPosition(field);
            if (position == 0)
            {
                return 0;
            }

            var vector = CheckedAdd(_buffer, position, ReadInt32(_buffer, position));
            var length = ReadInt32(_buffer, vector);
            if (length < 0)
            {
                throw new FlatBufferFormatException("Invalid vector length.");
            }

            return length;
        }

        internal byte GetVectorUInt8(int field, int index)
        {
            var position = VectorElementPosition(field, index, sizeof(byte));
            return _buffer[position];
        }

        internal float GetVectorFloat(int field, int index)
            => BitConverter.Int32BitsToSingle(ReadInt32(_buffer, VectorElementPosition(field, index, sizeof(float))));

        internal uint GetVectorUInt32(int field, int index)
            => unchecked((uint)ReadInt32(_buffer, VectorElementPosition(field, index, sizeof(uint))));

        internal FlatBufferTable GetVectorTable(int field, int index)
        {
            var position = VectorElementPosition(field, index, sizeof(int));
            return new FlatBufferTable(_buffer, CheckedAdd(_buffer, position, ReadInt32(_buffer, position)));
        }

        internal string GetVectorString(int field, int index)
        {
            var position = VectorElementPosition(field, index, sizeof(int));
            var stringPosition = CheckedAdd(_buffer, position, ReadInt32(_buffer, position));
            var length = ReadInt32(_buffer, stringPosition);
            if (length < 0 || stringPosition + 4 + length > _buffer.Length)
            {
                throw new FlatBufferFormatException("String is outside the buffer.");
            }

            return Encoding.UTF8.GetString(_buffer, stringPosition + 4, length);
        }

        static int ReadInt32(byte[] buffer, int position)
        {
            if (position < 0 || position + 4 > buffer.Length)
            {
                throw new FlatBufferFormatException("Read is outside the buffer.");
            }

            return buffer[position] |
                   (buffer[position + 1] << 8) |
                   (buffer[position + 2] << 16) |
                   (buffer[position + 3] << 24);
        }

        static ushort ReadUInt16(byte[] buffer, int position)
        {
            if (position < 0 || position + 2 > buffer.Length)
            {
                throw new FlatBufferFormatException("Read is outside the buffer.");
            }

            return unchecked((ushort)(buffer[position] | (buffer[position + 1] << 8)));
        }

        static int CheckedAdd(byte[] buffer, int position, int offset)
        {
            var result = (long)position + offset;
            if (result < 0 || result >= buffer.Length)
            {
                throw new FlatBufferFormatException("Offset is outside the buffer.");
            }

            return (int)result;
        }

        int VectorElementPosition(int field, int index, int elementSize)
        {
            var position = FieldPosition(field);
            if (position == 0)
            {
                throw new FlatBufferFormatException("Vector is not present.");
            }

            var vector = CheckedAdd(_buffer, position, ReadInt32(_buffer, position));
            var length = ReadInt32(_buffer, vector);
            if (index < 0 || index >= length)
            {
                throw new FlatBufferFormatException("Vector index is out of range.");
            }

            var result = vector + 4 + (index * elementSize);
            if (result < 0 || result + elementSize > _buffer.Length)
            {
                throw new FlatBufferFormatException("Vector element is outside the buffer.");
            }

            return result;
        }
    }
}
