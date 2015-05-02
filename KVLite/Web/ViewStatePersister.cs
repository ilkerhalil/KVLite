// File name: ViewStatePersister.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics.Contracts;
using System.Web;
using System.Web.UI;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite.Web
{
    /// <summary>
    ///   This class is an example of a BaseStatePersister implementation.
    /// </summary>
    public sealed class ViewStatePersister : BaseStatePersister
    {
        #region Fields

        /// <summary>
        ///   The partition used by ViewState items.
        /// </summary>
        private const string ViewStatePartition = "KVLite.Web.ViewStates";

        private static ICache _cache = PersistentCache.DefaultInstance;

        private static readonly TimeSpan CacheInterval = TimeSpan.FromMinutes(HttpContext.Current.Session.Timeout + 1);

        #endregion Fields

        #region Properties

        /// <summary>
        ///   Gets or sets the cache instance currently used by the provider.
        /// </summary>
        /// <value>The cache instance currently used by the provider.</value>
        public static ICache Cache
        {
            get
            {
                Contract.Ensures(Contract.Result<ICache>() != null);
                return _cache;
            }
            set
            {
                Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullCache);
                Contract.Ensures(ReferenceEquals(Cache, value));
                _cache = value;
            }
        }

        #endregion Properties

        /// <summary>
        ///   Initializes a new instance of the <see cref="ViewStatePersister"/> class.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <remarks>This constructor is required.</remarks>
        public ViewStatePersister(Page page)
            : base(page)
        {
            ViewStateSettings = new ViewStateStorageSettings();
        }

        public override void Load()
        {
            var guid = Page.Request.Form[HiddenFieldName];

            // using the unique id, fetch the serialized viewstate data, possibly from an internal method
            var state = GetViewState(guid);

            // the state object is a System.Web.UI.Pair, because we must set the ControlState as well
            var pair = state as Pair;
            if (pair != null)
            {
                ControlState = pair.First;
                ViewState = pair.Second;
            }
        }

        public override void Save()
        {
            var guid = GetViewStateId();
            Page.ClientScript.RegisterHiddenField(HiddenFieldName, guid);
            SetViewState(guid);
        }

        /// <summary>
        ///   Clears this persister instance.
        /// </summary>
        public override void Clear()
        {
            _cache.Clear();
        }

        private static object GetViewState(string guid)
        {
            return _cache.Get<object>(ViewStatePartition, HiddenFieldName + guid).Value;
        }

        private void SetViewState(string guid)
        {
            object state = new Pair(ControlState, ViewState);
            _cache.AddSlidingAsync(ViewStatePartition, HiddenFieldName + guid, state, CacheInterval);
        }
    }
}
