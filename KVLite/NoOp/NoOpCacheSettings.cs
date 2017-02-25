// Copyright 2015-2025 Alessio Parma <alessio.parma@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
// in compliance with the License. You may obtain a copy of the License at:
//
// "http://www.apache.org/licenses/LICENSE-2.0"
//
// Unless required by applicable law or agreed to in writing, software distributed under the License
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions and limitations under
// the License.

using System;
using System.Runtime.Serialization;

namespace PommaLabs.KVLite.NoOp
{
    /// <summary>
    ///   Settings used by <see cref="NoOpCache"/>.
    /// </summary>
    [Serializable, DataContract]
    public sealed class NoOpCacheSettings : AbstractCacheSettings<NoOpCacheSettings>
    {
        /// <summary>
        ///   Gets the cache URI; used for logging.
        /// </summary>
        /// <value>The cache URI.</value>
        [DataMember]
        public override string CacheUri { get; } = Guid.NewGuid().ToString("D");
    }
}
