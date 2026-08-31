// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace CommunityToolkit.WinUI.Lottie.CompDataFlatbuffer
{
    /// <summary>
    /// A minimal writer for the FlatBuffers binary wire format. Buffers produced
    /// by this writer are readable by any FlatBuffers implementation, including
    /// the C++ code generated from lottie_comp.fbs.
    /// </summary>
    /// <remarks>
    /// The wire format is described at https://flatbuffers.dev/flatbuffers_internals.html.
    /// Data is written from the end of the buffer towards its start, so all
    /// offsets returned by this class are measured backwards from the end of the
    /// buffer.
    /// </remarks>
    sealed class FlatBufferWriter
    {
        // The offsets of the vtables that have been written so far. Used to
        // share identical vtables between tables.
        readonly List<int> _vtables = new List<int>();

        // The field offsets of the table that is currently being built.
        readonly List<int> _currentVTable = new List<int>();

        byte[] _buffer;

        // The number of bytes at the end of _buffer that contain data.
        int _used;

        // The offset of the start of the table that is currently being built,
        // or -1 if no table is being built.
        int _tableStart = -1;

        int _minAlign = 1;

        internal FlatBufferWriter(int initialSize = 4096)
        {
            _buffer = new byte[Math.Max(initialSize, 32)];
        }

        /// <summary>
        /// Starts a table with the given number of fields. Fields are added with
        /// the Add methods, then the table is completed by calling <see cref="EndTable"/>.
        /// </summary>
        /// <param name="fieldCount">The number of fields declared by the table in the schema.</param>
        internal void StartTable(int fieldCount)
        {
            if (_tableStart >= 0)
            {
                throw new InvalidOperationException("Nested table construction is not supported.");
            }

            _currentVTable.Clear();
            for (var i = 0; i < fieldCount; i++)
            {
                _currentVTable.Add(0);
            }

            _tableStart = _used;
        }

        /// <summary>
        /// Completes the table that is being built and returns its offset.
        /// </summary>
        /// <returns>The offset of the table.</returns>
        internal int EndTable()
        {
            if (_tableStart < 0)
            {
                throw new InvalidOperationException("No table is being built.");
            }

            // Reserve space for the offset to the vtable.
            Prep(sizeof(int), 0);
            EnsureSpace(sizeof(int));
            _used += sizeof(int);
            var tableEnd = _used;

            // Trailing empty fields are implied by the length of the vtable.
            var fieldCount = _currentVTable.Count;
            while (fieldCount > 0 && _currentVTable[fieldCount - 1] == 0)
            {
                fieldCount--;
            }

            // Write the vtable: the size of the vtable, the size of the table,
            // then the offset of each field within the table.
            var vtableSize = (fieldCount + 2) * sizeof(short);
            Prep(sizeof(short), vtableSize);
            for (var i = fieldCount - 1; i >= 0; i--)
            {
                var fieldOffset = _currentVTable[i];
                WriteInt16(checked((short)(fieldOffset == 0 ? 0 : tableEnd - fieldOffset)));
            }

            WriteInt16(checked((short)(tableEnd - _tableStart)));
            WriteInt16(checked((short)vtableSize));

            var vtableOffset = _used;

            // Share an identical vtable if one has already been written.
            var sharedVTableOffset = FindMatchingVTable(vtableOffset, vtableSize);
            if (sharedVTableOffset >= 0)
            {
                _used = vtableOffset - vtableSize;
                vtableOffset = sharedVTableOffset;
            }
            else
            {
                _vtables.Add(vtableOffset);
            }

            // Patch the signed offset from the table to its vtable.
            WriteInt32At(tableEnd, vtableOffset - tableEnd);

            _tableStart = -1;
            return tableEnd;
        }

        internal void AddUInt8(int field, byte value, byte defaultValue)
        {
            if (value != defaultValue)
            {
                Prep(sizeof(byte), 0);
                WriteByte(value);
                Slot(field);
            }
        }

        internal void AddUInt8(int field, byte? value)
        {
            if (value.HasValue)
            {
                Prep(sizeof(byte), 0);
                WriteByte(value.Value);
                Slot(field);
            }
        }

        internal void AddBool(int field, bool value, bool defaultValue)
            => AddUInt8(field, (byte)(value ? 1 : 0), (byte)(defaultValue ? 1 : 0));

        internal void AddBool(int field, bool? value)
            => AddUInt8(field, value.HasValue ? (byte)(value.Value ? 1 : 0) : (byte?)null);

        internal void AddUInt16(int field, ushort value, ushort defaultValue)
        {
            if (value != defaultValue)
            {
                Prep(sizeof(short), 0);
                WriteInt16(unchecked((short)value));
                Slot(field);
            }
        }

        internal void AddInt32(int field, int value, int defaultValue)
        {
            if (value != defaultValue)
            {
                Prep(sizeof(int), 0);
                WriteInt32(value);
                Slot(field);
            }
        }

        internal void AddInt32(int field, int? value)
        {
            if (value.HasValue)
            {
                Prep(sizeof(int), 0);
                WriteInt32(value.Value);
                Slot(field);
            }
        }

        internal void AddUInt32(int field, uint value, uint defaultValue)
            => AddInt32(field, unchecked((int)value), unchecked((int)defaultValue));

        internal void AddInt64(int field, long value, long defaultValue)
        {
            if (value != defaultValue)
            {
                Prep(sizeof(long), 0);
                WriteInt64(value);
                Slot(field);
            }
        }

        internal void AddFloat(int field, float value, float defaultValue)
        {
            if (!value.Equals(defaultValue))
            {
                Prep(sizeof(float), 0);
                WriteInt32(BitConverter.SingleToInt32Bits(value));
                Slot(field);
            }
        }

        internal void AddFloat(int field, float? value)
        {
            if (value.HasValue)
            {
                Prep(sizeof(float), 0);
                WriteInt32(BitConverter.SingleToInt32Bits(value.Value));
                Slot(field);
            }
        }

        /// <summary>
        /// Adds a reference to a previously created table, vector or string.
        /// An <paramref name="offset"/> of 0 means "not present".
        /// </summary>
        /// <param name="field">The id of the field.</param>
        /// <param name="offset">The offset of the referenced data.</param>
        internal void AddOffset(int field, int offset)
        {
            if (offset != 0)
            {
                Prep(sizeof(int), 0);
                WriteInt32(_used + sizeof(int) - offset);
                Slot(field);
            }
        }

        /// <summary>
        /// Prepares to write an inline struct of the given size and alignment.
        /// The struct's fields must then be written in reverse declaration order
        /// using the Write methods, followed by <see cref="AddStruct"/>.
        /// </summary>
        /// <param name="alignment">The alignment of the struct.</param>
        /// <param name="size">The size of the struct.</param>
        internal void StartStruct(int alignment, int size) => Prep(alignment, size);

        /// <summary>
        /// Adds an inline struct field whose data was just written by
        /// <see cref="StartStruct"/> and the Write methods.
        /// </summary>
        /// <param name="field">The id of the field.</param>
        internal void AddStruct(int field) => Slot(field);

        internal void WriteFloat(float value) => WriteInt32(BitConverter.SingleToInt32Bits(value));

        internal void WriteUInt8(byte value) => WriteByte(value);

        internal int CreateString(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);

            // Strings are null terminated for the convenience of C++ readers.
            Prep(sizeof(int), bytes.Length + 1);
            WriteByte(0);
            EnsureSpace(bytes.Length);
            _used += bytes.Length;
            Array.Copy(bytes, 0, _buffer, _buffer.Length - _used, bytes.Length);
            WriteInt32(bytes.Length);
            return _used;
        }

        internal int CreateByteVector(IReadOnlyList<byte> values)
        {
            StartVector(sizeof(byte), values.Count, sizeof(byte));
            for (var i = values.Count - 1; i >= 0; i--)
            {
                WriteByte(values[i]);
            }

            return EndVector(values.Count);
        }

        internal int CreateFloatVector(IReadOnlyList<float> values)
        {
            StartVector(sizeof(float), values.Count, sizeof(float));
            for (var i = values.Count - 1; i >= 0; i--)
            {
                WriteInt32(BitConverter.SingleToInt32Bits(values[i]));
            }

            return EndVector(values.Count);
        }

        internal int CreateUInt32Vector(IReadOnlyList<uint> values)
        {
            StartVector(sizeof(uint), values.Count, sizeof(uint));
            for (var i = values.Count - 1; i >= 0; i--)
            {
                WriteInt32(unchecked((int)values[i]));
            }

            return EndVector(values.Count);
        }

        /// <summary>
        /// Creates a vector of offsets to previously created tables or strings.
        /// </summary>
        /// <param name="offsets">The offsets of the referenced data.</param>
        /// <returns>The offset of the vector.</returns>
        internal int CreateOffsetVector(IReadOnlyList<int> offsets)
        {
            StartVector(sizeof(int), offsets.Count, sizeof(int));
            for (var i = offsets.Count - 1; i >= 0; i--)
            {
                Prep(sizeof(int), 0);
                WriteInt32(_used + sizeof(int) - offsets[i]);
            }

            return EndVector(offsets.Count);
        }

        /// <summary>
        /// Completes the buffer, making <paramref name="rootTable"/> its root.
        /// </summary>
        /// <param name="rootTable">The offset of the root table.</param>
        /// <param name="fileIdentifier">The 4 character file identifier.</param>
        /// <returns>The completed buffer.</returns>
        internal byte[] Finish(int rootTable, string fileIdentifier)
        {
            if (fileIdentifier is null || fileIdentifier.Length != 4)
            {
                throw new ArgumentException("File identifier must be 4 characters.", nameof(fileIdentifier));
            }

            Prep(_minAlign, sizeof(int) + 4);
            for (var i = 3; i >= 0; i--)
            {
                WriteByte((byte)fileIdentifier[i]);
            }

            WriteInt32(_used + sizeof(int) - rootTable);

            var result = new byte[_used];
            Array.Copy(_buffer, _buffer.Length - _used, result, 0, _used);
            return result;
        }

        void StartVector(int elementSize, int count, int alignment)
        {
            Prep(sizeof(int), elementSize * count);
            Prep(alignment, elementSize * count);
        }

        int EndVector(int count)
        {
            WriteInt32(count);
            return _used;
        }

        // Records the offset of the field that was just written.
        void Slot(int field)
        {
            if (field >= _currentVTable.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(field));
            }

            _currentVTable[field] = _used;
        }

        // Pads so that, after writing additionalBytes more bytes, the write
        // position will be aligned to the given alignment.
        void Prep(int alignment, int additionalBytes)
        {
            if (alignment > _minAlign)
            {
                _minAlign = alignment;
            }

            var alignSize = ((~(_used + additionalBytes)) + 1) & (alignment - 1);
            EnsureSpace(alignSize + additionalBytes);
            for (var i = 0; i < alignSize; i++)
            {
                WriteByte(0);
            }
        }

        int FindMatchingVTable(int vtableOffset, int vtableSize)
        {
            foreach (var candidate in _vtables)
            {
                if (ReadInt16At(candidate) != vtableSize)
                {
                    continue;
                }

                var match = true;
                for (var i = sizeof(short); i < vtableSize; i += sizeof(short))
                {
                    if (ReadInt16At(candidate - i) != ReadInt16At(vtableOffset - i))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return candidate;
                }
            }

            return -1;
        }

        void EnsureSpace(int size)
        {
            if (_used + size <= _buffer.Length)
            {
                return;
            }

            var newSize = _buffer.Length;
            while (newSize < _used + size)
            {
                newSize *= 2;
            }

            var newBuffer = new byte[newSize];
            Array.Copy(_buffer, _buffer.Length - _used, newBuffer, newSize - _used, _used);
            _buffer = newBuffer;
        }

        void WriteByte(byte value)
        {
            EnsureSpace(sizeof(byte));
            _used += sizeof(byte);
            _buffer[_buffer.Length - _used] = value;
        }

        void WriteInt16(short value)
        {
            EnsureSpace(sizeof(short));
            _used += sizeof(short);
            var index = _buffer.Length - _used;
            _buffer[index] = unchecked((byte)value);
            _buffer[index + 1] = unchecked((byte)(value >> 8));
        }

        void WriteInt32(int value)
        {
            EnsureSpace(sizeof(int));
            _used += sizeof(int);
            WriteInt32At(_used, value);
        }

        void WriteInt64(long value)
        {
            // The buffer is written backwards, so the high word is written first.
            WriteInt32(unchecked((int)(value >> 32)));
            WriteInt32(unchecked((int)value));
        }

        void WriteInt32At(int offset, int value)
        {
            var index = _buffer.Length - offset;
            _buffer[index] = unchecked((byte)value);
            _buffer[index + 1] = unchecked((byte)(value >> 8));
            _buffer[index + 2] = unchecked((byte)(value >> 16));
            _buffer[index + 3] = unchecked((byte)(value >> 24));
        }

        short ReadInt16At(int offset)
        {
            var index = _buffer.Length - offset;
            return unchecked((short)(_buffer[index] | (_buffer[index + 1] << 8)));
        }
    }
}
