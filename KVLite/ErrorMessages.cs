//
// ErrorMessages.cs
// 
// Author(s):
//     Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace PommaLabs.KVLite
{
    public static class ErrorMessages
    {
       public const string Web_HttpRuntimeCache_CannotClear = "It is not possible to clear the HttpRuntime cache.";

        public const string InvalidEnumValue = "Given enumeration value is not valid.";
        public const string NotSerializableValue = @"Only serializable objects can be stored in the cache. Try putting the [Serializable] attribute on your class, if possible.";
        public const string NullOrEmptyCachePath = @"Cache path cannot be null or empty.";
        public const string NullKey = @"Key cannot be null.";
        public const string NullPartition = @"Partition cannot be null.";
        public const string NullValue = @"Value cannot be null.";
    }
}
