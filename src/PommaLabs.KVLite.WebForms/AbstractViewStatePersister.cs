/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Copyright 2009, Flesk Telecom                                               *
 * This file is part of Flesk.NET Software.                                    *
 *                                                                             *
 * Flesk.NET Software is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU Lesser General Public License as published by *
 * the Free Software Foundation, either version 3 of the License, or           *
 * (at your option) any later version.                                         *
 *                                                                             *
 * Flesk.NET Software is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of              *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the               *
 * GNU General Public License for more details.                                *
 *                                                                             *
 * You should have received a copy of the GNU Lesser General Public License    *
 * along with Flesk.NET Software. If not, see <http://www.gnu.org/licenses/>.  *
 *                                                                             *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using NodaTime;
using PommaLabs.KVLite.Resources;
using PommaLabs.KVLite.Thrower;
using System;
using System.Text;
using System.Web;
using System.Web.UI;

namespace PommaLabs.KVLite.WebForms
{
    /// <summary>
    ///   Base class for a custom viewstate persister.
    /// </summary>
    public abstract class AbstractViewStatePersister : PageStatePersister
    {
        #region Constants

        /// <summary>
        ///   The prefix used to tag a viewstate.
        /// </summary>
        public const string HiddenFieldName = "__VIEWSTATE_ID";

        /// <summary>
        ///   The partition used by ViewState items.
        /// </summary>
        public const string ViewStatePartition = "KVLite.Web.ViewStates";

        /// <summary>
        ///   The cache interval is computed from the session timeout plus one minute.
        /// </summary>
        private static readonly Duration CacheInterval = Duration.FromMinutes(HttpContext.Current.Session.Timeout + 1);

        #endregion Constants

        /// <summary>
        ///   Initializes a new instance of the <see cref="AbstractViewStatePersister"/> class.
        /// </summary>
        /// <param name="page">
        ///   The <see cref="T:System.Web.UI.Page"/> that the view state persistence mechanism is
        ///   created for.
        /// </param>
        /// <param name="cache">The cache where the page view state will be stored.</param>
        protected AbstractViewStatePersister(Page page, ICache cache)
            : base(page)
        {
            Raise.ArgumentNullException.IfIsNull(cache, nameof(cache), ErrorMessages.NullCache);
            Cache = cache;
        }

        #region Properties

        /// <summary>
        ///   The settings used while storing the view state.
        /// </summary>
        public ViewStateStorageSettings ViewStateSettings { get; set; } = new ViewStateStorageSettings();

        /// <summary>
        ///   The cache where the view state will be stored.
        /// </summary>
        public ICache Cache { get; }

        #endregion Properties

        /// <summary>
        ///   Clears this persister instance.
        /// </summary>
        public void Clear() => Cache.Clear();

        /// <summary>
        ///   Overridden by derived classes to deserialize and load persisted state information when
        ///   a <see cref="T:System.Web.UI.Page"/> object initializes its control hierarchy.
        /// </summary>
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

        /// <summary>
        ///   Overridden by derived classes to serialize persisted state information when a
        ///   <see cref="T:System.Web.UI.Page"/> object is unloaded from memory.
        /// </summary>
        public override void Save()
        {
            var guid = GetViewStateId();
            Page.ClientScript.RegisterHiddenField(HiddenFieldName, guid);
            SetViewState(guid);
        }

        /// <summary>
        ///   Gets the view state identifier.
        /// </summary>
        /// <returns>The view state identifier.</returns>
        protected virtual string GetViewStateId()
        {
            string ret;

            if (Page.IsPostBack && ViewStateSettings.RequestBehavior == ViewStateStorageBehavior.FirstLoad)
            {
                ret = SanitizeInput(Page.Request.Form[HiddenFieldName]);
            }
            else
            {
                ret = Guid.NewGuid().ToString();
            }

            return ret;
        }

        private static string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var ret = new StringBuilder(input);
            ret.Replace(".", "");
            ret.Replace("\\", "");
            ret.Replace("/", "");
            ret.Replace("'", "");
            return ret.ToString();
        }

        private object GetViewState(string guid) => Cache.Get<object>(ViewStatePartition, HiddenFieldName + guid).ValueOrDefault();

        private void SetViewState(string guid)
        {
            object state = new Pair(ControlState, ViewState);
            Cache.AddSliding(ViewStatePartition, HiddenFieldName + guid, state, CacheInterval);
        }
    }
}
