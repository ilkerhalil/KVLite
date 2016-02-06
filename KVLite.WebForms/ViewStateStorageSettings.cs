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
using System.Configuration;
using System.Xml;

namespace PommaLabs.KVLite.WebForms
{
    /// <summary>
    ///   Represents the group of options used by the Flesk.Accelerator.Page class to determine the
    ///   storage method of the ViewState object.
    /// </summary>
    public sealed class ViewStateStorageSettings : ICloneable
    {
        ViewStateStorageBehavior _behavior;
        bool _compressed;
        string _connectionString = "data source=127.0.0.1;Trusted_Connection=yes";
        ViewStateStorageMethod _method;
        string _storagePath = "~/Viewstate";
        string _tableName = "app_ViewState";
        double _fileage = 3;
        TimeSpan _maxAge = TimeSpan.Zero;

        /// <summary>
        ///   Initializes a new instance of Flesk.Accelerator.ViewState.ViewStateStorageSettings
        ///   object, using a XmlNode containing data.
        /// </summary>
        /// <param name="node"></param>
        public ViewStateStorageSettings(XmlNode node)
        {
            if (node == null)
            {
                return;
            }

            var handlerName = node.Attributes[nameof(PersistenceHandler)];
            if (handlerName != null)
            {
                PersistenceHandler = handlerName.Value;
            }

            var storagePath = node.Attributes["StoragePath"];
            if (storagePath != null)
            {
                _storagePath = storagePath.Value;
            }

            var connString = node.Attributes[nameof(ConnectionString)];
            if (connString != null)
            {
                _connectionString = connString.Value;
            }

            var tableName = node.Attributes[nameof(TableName)];
            if (tableName != null)
            {
                _tableName = tableName.Value;
            }

            var storageMethod = node.Attributes["StorageMethod"];
            if (storageMethod != null)
            {
                try
                {
                    _method = (ViewStateStorageMethod) Enum.Parse(typeof(ViewStateStorageMethod), storageMethod.Value, true);
                }
#pragma warning disable CC0004 // Catch block cannot be empty
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                catch { }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
#pragma warning restore CC0004 // Catch block cannot be empty
            }

            var compressed = node.Attributes[nameof(Compressed)];
            if (compressed != null)
            {
                _compressed = (string.Compare(compressed.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase) == 0);
            }

            var behavior = node.Attributes[nameof(RequestBehavior)];
            if (behavior != null)
            {
                try
                {
                    _behavior = (ViewStateStorageBehavior) Enum.Parse(typeof(ViewStateStorageBehavior), behavior.Value, true);
                }
#pragma warning disable CC0004 // Catch block cannot be empty
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                catch { }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
#pragma warning restore CC0004 // Catch block cannot be empty
            }

            var viewstatefilesMaxAge = node.Attributes[nameof(ViewStateFilesMaxAge)];
            if (viewstatefilesMaxAge != null)
            {
                try
                {
                    _fileage = Double.Parse(viewstatefilesMaxAge.Value);
                }
#pragma warning disable CC0004 // Catch block cannot be empty
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                catch { }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
#pragma warning restore CC0004 // Catch block cannot be empty
            }

            var viewstateCleanupInterval = node.Attributes[nameof(ViewStateCleanupInterval)];
            if (viewstateCleanupInterval != null)
            {
                try
                {
                    _maxAge = TimeSpan.Parse(viewstateCleanupInterval.Value);
                }
#pragma warning disable CC0004 // Catch block cannot be empty
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                catch { }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
#pragma warning restore CC0004 // Catch block cannot be empty
            }
        }

        /// <summary>
        ///   An empty constructor...
        /// </summary>
        public ViewStateStorageSettings()
        {
        }

        /// <summary>
        ///   Gets or sets the persistence handler.
        /// </summary>
        /// <value>The persistence handler.</value>
        public string PersistenceHandler { get; set; }

        /// <summary>
        ///   True if the contents of the viewstate object are to be compressed, otherwise, false.
        /// </summary>
        public bool Compressed
        {
            get { return _compressed; }
            set { _compressed = value; }
        }

        /// <summary>
        ///   Sets ViewState Files age to be deleted. All ViewState files wich are more than
        ///   ViewStateFilesMaxAge will be deleted.
        /// </summary>
        public double ViewStateFilesMaxAge
        {
            get { return _fileage; }
            set { _fileage = value; }
        }

        /// <summary>
        ///   Gets or sets the view state cleanup interval.
        /// </summary>
        /// <value>The view state cleanup interval.</value>
        public TimeSpan ViewStateCleanupInterval
        {
            get { return _maxAge; }
            set { _maxAge = value; }
        }

        /// <summary>
        ///   Determines the actual method of storing the ViewState.
        /// </summary>
        public ViewStateStorageMethod Method
        {
            get { return _method; }
            set { _method = value; }
        }

        /// <summary>
        ///   Gets or sets the storage behavior for the page request; if set to FirstLoad, the page
        ///   will then reuse the Viewstate data for the following postbacks, if set to EachLoad,
        ///   the page will generate Viewstate data for each request.
        /// </summary>
        public ViewStateStorageBehavior RequestBehavior
        {
            get { return _behavior; }
            set { _behavior = value; }
        }

        /// <summary>
        ///   If the Method property is set to 'File', use this property to set the virtual
        ///   directory where the viewstate files are created.
        /// </summary>
        public string StorageVirtualPath
        {
            get { return _storagePath; }
            set { _storagePath = value; }
        }

        /// <summary>
        ///   If the Method property is set to 'SqlServer', use this property to set the connection
        ///   string of the database where the viewstate will be stored.
        /// </summary>
        public string ConnectionString
        {
            get { return _connectionString; }
            set { _connectionString = value; }
        }

        /// <summary>
        ///   Gets or sets the name of the database table where the viewstate data will be stored.
        /// </summary>
        /// <value></value>
        public string TableName
        {
            get { return _tableName; }
            set { _tableName = value; }
        }

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion ICloneable Members

        /// <summary>
        ///   Initializes a Flesk.Accelerator.ViewState.ViewStateStorageSettings instance, fetching
        ///   the values from a predefined configuration key.
        /// </summary>
        /// <returns></returns>
        public static ViewStateStorageSettings GetSettings()
        {
            var settings = (ViewStateStorageSettings) ConfigurationManager.GetSection("Flesk.NET/ViewStateOptimizer");
            return settings ?? new ViewStateStorageSettings();
        }

        /// <summary>
        ///   Clones this instance.
        /// </summary>
        /// <returns>A deep copy of this instance.</returns>
        public ViewStateStorageSettings Clone()
        {
            var ret = new ViewStateStorageSettings
            {
                _behavior = _behavior,
                _compressed = _compressed,
                _connectionString = _connectionString,
                _method = _method,
                _storagePath = _storagePath,
                _tableName = _tableName,
                _fileage = _fileage
            };
            return ret;
        }
    }

    /// <summary>
    ///   </summary>
    public enum ViewStateStorageMethod
    {
        /// <summary>
        ///   The page viewstate is stored as a hidden field in the page output (default behavior).
        /// </summary>
        Default = 0,

        /// <summary>
        ///   The page viewstate is serialized and saved in a file.
        /// </summary>
        File,

        /// <summary>
        ///   The page viewstate is serialized and saved in the process identity's isolated file storage.
        /// </summary>
        IsolatedStorage,

        /// <summary>
        ///   The page viewstate is stored in the Session object.
        /// </summary>
        Session,

        /// <summary>
        ///   The page viewstate is stored in an SQL server table.
        /// </summary>
        SqlServer
    }

    /// <summary>
    ///   </summary>
    public enum ViewStateStorageBehavior
    {
        /// <summary>
        ///   The Viewstate storage is generated on the first request to a page, and is reused in
        ///   the following postbacks.
        /// </summary>
        FirstLoad,

        /// <summary>
        ///   The Viewstate storage is generated on each request to a page.
        /// </summary>
        EachLoad
    }
}
