﻿// File name: VolatilePageStatePersister.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2018 Alessio Parma <alessio.parma@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using PommaLabs.KVLite.SQLite;
using System.Web.UI;

namespace PommaLabs.KVLite.WebForms
{
    /// <summary>
    ///   This class is a view state implementation based on <see cref="VolatileCache"/>.
    /// </summary>
    public sealed class VolatileViewStatePersister : AbstractViewStatePersister
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="VolatileViewStatePersister"/> class.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <remarks>This constructor is required.</remarks>
        public VolatileViewStatePersister(Page page)
            : base(page, WebCaches.Volatile)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="VolatileViewStatePersister"/> class.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="cache">The volatile cache.</param>
        public VolatileViewStatePersister(Page page, VolatileCache cache)
            : base(page, cache)
        {
        }
    }
}