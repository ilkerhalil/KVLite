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

using System;
using System.Text;
using System.Web.UI;

namespace PommaLabs.KVLite.Web
{
    /// <summary>
    ///   Base class for a custom viewstate persister.
    /// </summary>
    public abstract class BaseStatePersister : PageStatePersister
    {
        /// <summary>
        ///   The prefix used to tag a viewstate.
        /// </summary>
        public const string HiddenFieldName = "__VIEWSTATE_ID";

        ViewStateStorageSettings _settings;

        /// <summary>
        ///   Initializes a new instance of the <see cref="BaseStatePersister"/> class.
        /// </summary>
        /// <param name="page">
        ///   The <see cref="T:System.Web.UI.Page"/> that the view state persistence mechanism is
        ///   created for.
        /// </param>
        protected BaseStatePersister(Page page)
            : base(page)
        {
        }

        /// <summary>
        ///   Gets or sets the view state settings.
        /// </summary>
        /// <value>The view state settings.</value>
        public ViewStateStorageSettings ViewStateSettings
        {
            get { return _settings; }
            set { _settings = value; }
        }

        /// <summary>
        ///   Clears this viewstate persister.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        ///   Gets the view state identifier.
        /// </summary>
        /// <returns>The view state identifier.</returns>
        protected virtual string GetViewStateId()
        {
            string ret;

            if (Page.IsPostBack && _settings.RequestBehavior == ViewStateStorageBehavior.FirstLoad)
            {
                ret = SanitizeInput(Page.Request.Form[HiddenFieldName]);
            }
            else
            {
                ret = Guid.NewGuid().ToString();
            }

            return ret;
        }

        static string SanitizeInput(string input)
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
    }
}