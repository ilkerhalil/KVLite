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
        public const string HiddenFieldName = "__VIEWSTATE_ID";

        private ViewStateStorageSettings _settings;

        protected BaseStatePersister(Page page)
            : base(page)
        {
        }

        public ViewStateStorageSettings ViewStateSettings
        {
            get { return _settings; }
            set { _settings = value; }
        }

        public abstract void Clear();

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

        private static string SanitizeInput(string input)
        {
            if (String.IsNullOrEmpty(input))
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