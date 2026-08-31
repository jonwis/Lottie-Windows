// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;

namespace CommunityToolkit.WinUI.Lottie.CompDataFlatbuffer
{
    /// <summary>
    /// Thrown when a buffer is not a valid serialized composition.
    /// </summary>
#if PUBLIC_CompDataFlatbuffer
    public
#endif
    sealed class FlatBufferFormatException : Exception
    {
        public FlatBufferFormatException()
        {
        }

        public FlatBufferFormatException(string message)
            : base(message)
        {
        }

        public FlatBufferFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
