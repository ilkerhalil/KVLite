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

namespace KVLite.Web
{
    /// <summary>
    /// Represents the group of options used by the Flesk.Accelerator.Page class
    /// to determine the storage method of the ViewState object.
    /// </summary>
    internal sealed class ViewStateStorageSettings : ICloneable
    {
        private ViewStateStorageBehavior _behavior;
        private bool _compressed;
        private string _connectionString = "data source=127.0.0.1;Trusted_Connection=yes";
        private ViewStateStorageMethod _method;
        private string _storagePath = "~/Viewstate";
        private string _tableName = "app_ViewState";
        private double fileage = 3;
        private TimeSpan maxAge = TimeSpan.Zero;

        /// <summary>
        /// Initializes a new instance of Flesk.Accelerator.ViewState.ViewStateStorageSettings object, 
        /// using a XmlNode containing data.
        /// </summary>
        /// <param name="node"></param>
        public ViewStateStorageSettings(XmlNode node)
        {
            if (node == null) {
                return;
            }

            var handlerName = node.Attributes["PersistenceHandler"];
            if (handlerName != null) {
                this.PersistenceHandler = handlerName.Value;
            }


            var storagePath = node.Attributes["StoragePath"];
            if (storagePath != null) {
                this._storagePath = storagePath.Value;
            }


            var connString = node.Attributes["ConnectionString"];
            if (connString != null) {
                this._connectionString = connString.Value;
            }

            var tableName = node.Attributes["TableName"];
            if (tableName != null) {
                this._tableName = tableName.Value;
            }


            var storageMethod = node.Attributes["StorageMethod"];
            if (storageMethod != null) {
                try {
                    this._method = (ViewStateStorageMethod) Enum.Parse(typeof(ViewStateStorageMethod), storageMethod.Value, true);
                } catch {}
            }

            var compressed = node.Attributes["Compressed"];
            if (compressed != null) {
                this._compressed = (String.Compare(compressed.Value, bool.TrueString, true) == 0);
            }

            var behavior = node.Attributes["RequestBehavior"];
            if (behavior != null) {
                try {
                    this._behavior = (ViewStateStorageBehavior) Enum.Parse(typeof(ViewStateStorageBehavior), behavior.Value, true);
                } catch {}
            }

            var viewstatefilesMaxAge = node.Attributes["ViewStateFilesMaxAge"];
            if (viewstatefilesMaxAge != null) {
                try {
                    this.fileage = Double.Parse(viewstatefilesMaxAge.Value);
                } catch {}
            }

            var viewstateCleanupInterval = node.Attributes["ViewStateCleanupInterval"];
            if (viewstateCleanupInterval != null) {
                try {
                    this.maxAge = TimeSpan.Parse(viewstateCleanupInterval.Value);
                } catch {}
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public ViewStateStorageSettings() {}

        public string PersistenceHandler { get; set; }

        /// <summary>
        /// True if the contents of the viewstate object are to be compressed, otherwise, false.
        /// </summary>
        public bool Compressed
        {
            get { return this._compressed; }
            set { this._compressed = value; }
        }

        /// <summary>
        /// Sets ViewState Files age to be deleted. 
        /// All ViewState files wich are more than ViewStateFilesMaxAge will be deleted.
        /// </summary>
        public double ViewStateFilesMaxAge
        {
            get { return this.fileage; }
            set { this.fileage = value; }
        }


        public TimeSpan ViewStateCleanupInterval
        {
            get { return maxAge; }
            set { maxAge = value; }
        }

        /// <summary>
        /// Determines the actual method of storing the ViewState.
        /// </summary>
        public ViewStateStorageMethod Method
        {
            get { return this._method; }
            set { this._method = value; }
        }

        /// <summary>
        /// Gets or sets the storage behavior for the page request; if set to FirstLoad, the page will then reuse the Viewstate data
        /// for the following postbacks, if set to EachLoad, the page will generate Viewstate data for each request.
        /// </summary>
        public ViewStateStorageBehavior RequestBehavior
        {
            get { return this._behavior; }
            set { this._behavior = value; }
        }

        /// <summary>
        /// If the Method property is set to 'File', use this property to set the virtual directory
        /// where the viewstate files are created.
        /// </summary>
        public string StorageVirtualPath
        {
            get { return this._storagePath; }
            set { this._storagePath = value; }
        }

        /// <summary>
        /// If the Method property is set to 'SqlServer', use this property
        /// to set the connection string of the database
        /// where the viewstate will be stored.
        /// </summary>
        public string ConnectionString
        {
            get { return this._connectionString; }
            set { this._connectionString = value; }
        }


        /// <summary>
        /// Gets or sets the name of the database table where the
        /// viewstate data will be stored.
        /// </summary>
        /// <value></value>
        public string TableName
        {
            get { return this._tableName; }
            set { this._tableName = value; }
        }

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        #endregion

        /// <summary>
        /// Initializes a Flesk.Accelerator.ViewState.ViewStateStorageSettings instance, fetching the values from a predefined configuration key.
        /// </summary>
        /// <returns></returns>
        public static ViewStateStorageSettings GetSettings()
        {
            var settings = (ViewStateStorageSettings) ConfigurationManager.GetSection("Flesk.NET/ViewStateOptimizer");
            if (settings == null) {
                return new ViewStateStorageSettings();
            }
            return settings;
        }

        public ViewStateStorageSettings Clone()
        {
            var ret = new ViewStateStorageSettings();
            ret._behavior = this._behavior;
            ret._compressed = this._compressed;
            ret._connectionString = this._connectionString;
            ret._method = this._method;
            ret._storagePath = this._storagePath;
            ret._tableName = this._tableName;
            ret.fileage = this.fileage;
            return ret;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ViewStateStorageMethod
    {
        /// <summary>
        /// The page viewstate is stored as a hidden field in the page output (default behavior).
        /// </summary>
        Default = 0,

        /// <summary>
        /// The page viewstate is serialized and saved in a file.
        /// </summary>
        File,

        /// <summary>
        /// The page viewstate is serialized and saved in the process identity's isolated file storage.
        /// </summary>
        IsolatedStorage,

        /// <summary>
        /// The page viewstate is stored in the Session object.
        /// </summary>
        Session,

        /// <summary>
        /// The page viewstate is stored in an SQL server table.
        /// </summary>
        SqlServer
    }


    /// <summary>
    /// 
    /// </summary>
    public enum ViewStateStorageBehavior
    {
        /// <summary>
        /// The Viewstate storage is generated on the first request to a page,
        /// and is reused in the following postbacks.
        /// </summary>
        FirstLoad,

        /// <summary>
        /// The Viewstate storage is generated on each request to a page.
        /// </summary>
        EachLoad
    }
}