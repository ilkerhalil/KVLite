// File name: OracleDynamicParameters.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
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

using Dapper;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace PommaLabs.KVLite.Oracle
{
    internal sealed class OracleDynamicParameters : SqlMapper.IDynamicParameters
    {
        private static readonly Hashtable ParamReaderCache = new Hashtable();

        private readonly Dictionary<string, ParamInfo> _parameters = new Dictionary<string, ParamInfo>();
        private List<object> _templates;

        private class ParamInfo
        {
            public string Name { get; set; }

            public object Value { get; set; }

            public ParameterDirection ParameterDirection { get; set; }

            public OracleDbType? DbType { get; set; }

            public int? Size { get; set; }

            public IDbDataParameter AttachedParam { get; set; }
        }

        /// <summary>
        ///   Constructs a dynamic parameter bag.
        /// </summary>
        public OracleDynamicParameters()
        {
        }

        /// <summary>
        ///   Constructs a dynamic parameter bag.
        /// </summary>
        /// <param name="template">Can be an anonymous type or a DynamicParameters bag.</param>
        public OracleDynamicParameters(object template)
        {
            AddDynamicParams(template);
        }

        /// <summary>
        ///   Appends a whole object full of params to the dynamic list.
        ///   EG: AddDynamicParams(new {A = 1, B = 2}) will add property A and B to the dynamic list.
        /// </summary>
        /// <param name="param"></param>
        public void AddDynamicParams(dynamic param)
        {
            var obj = param as object;
            if (obj != null)
            {
                var subDynamic = obj as OracleDynamicParameters;
                if (subDynamic == null)
                {
                    var dictionary = obj as IEnumerable<KeyValuePair<string, object>>;
                    if (dictionary == null)
                    {
                        _templates = _templates ?? new List<object>();
                        _templates.Add(obj);
                    }
                    else
                    {
                        foreach (var kvp in dictionary)
                        {
                            Add(kvp.Key, kvp.Value);
                        }
                    }
                }
                else
                {
                    if (subDynamic._parameters != null)
                    {
                        foreach (var kvp in subDynamic._parameters)
                        {
                            _parameters.Add(kvp.Key, kvp.Value);
                        }
                    }

                    if (subDynamic._templates != null)
                    {
                        _templates = _templates ?? new List<object>();
                        foreach (var t in subDynamic._templates)
                        {
                            _templates.Add(t);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   Adds a parameter to this dynamic parameter list.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        /// <param name="dbType">Parameter DB type.</param>
        /// <param name="direction">Parameter direction.</param>
        /// <param name="size">Parameter size.</param>
        public void Add(string name, object value = null, OracleDbType? dbType = null, ParameterDirection? direction = null, int? size = null)
        {
            _parameters[Clean(name)] = new ParamInfo
            {
                Name = name,
                Value = value,
                ParameterDirection = direction ?? ParameterDirection.Input,
                DbType = dbType,
                Size = size
            };
        }

        private static string Clean(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                switch (name[0])
                {
                    case '@':
                    case ':':
                    case '?':
                        return name.Substring(1);
                }
            }
            return name;
        }

        void SqlMapper.IDynamicParameters.AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            AddParameters(command, identity);
        }

        /// <summary>
        ///   Add all the parameters needed to the command just before it executes
        /// </summary>
        /// <param name="command">The raw command prior to execution</param>
        /// <param name="identity">Information about the query</param>
        protected void AddParameters(IDbCommand command, SqlMapper.Identity identity)
        {
            if (_templates != null)
            {
                foreach (var template in _templates)
                {
                    var newIdent = identity.ForDynamicParameters(template.GetType());
                    var appender = ParamReaderCache[newIdent] as Action<IDbCommand, object>;

                    if (appender == null)
                    {
                        lock (ParamReaderCache)
                        {
                            appender = ParamReaderCache[newIdent] as Action<IDbCommand, object>;
                            if (appender == null)
                            {
                                appender = SqlMapper.CreateParamInfoGenerator(newIdent, false, false);
                                ParamReaderCache[newIdent] = appender;
                            }
                        }
                    }

                    appender(command, template);
                }
            }

            foreach (var param in _parameters.Values)
            {
                var name = Clean(param.Name);
                var oracleCommand = command as OracleCommand;

                var add = !oracleCommand.Parameters.Contains(name);
                OracleParameter p;
                if (add)
                {
                    p = oracleCommand.CreateParameter();
                    p.ParameterName = name;
                }
                else
                {
                    p = oracleCommand.Parameters[name];
                }

                var val = param.Value;
                p.Value = val ?? DBNull.Value;
                p.Direction = param.ParameterDirection;

                var s = val as string;
                if (s != null)
                {
                    if (s.Length <= 4000)
                    {
                        p.Size = 4000;
                    }
                }

                if (param.Size != null)
                {
                    p.Size = param.Size.Value;
                }
                if (param.DbType != null)
                {
                    p.OracleDbType = param.DbType.Value;
                }

                if (add)
                {
                    command.Parameters.Add(p);
                }
                param.AttachedParam = p;
            }
        }

        /// <summary>
        ///   All the names of the parameters in the bag, use Get to yank them out.
        /// </summary>
        public IEnumerable<string> ParameterNames => _parameters.Select(p => p.Key);

        /// <summary>
        ///   Gets the value of a parameter.
        /// </summary>
        /// <typeparam name="T">Parameter type.</typeparam>
        /// <param name="name">Parameter name.</param>
        /// <returns>
        ///   The value, note that <see cref="DBNull.Value"/> is not returned, instead the value is
        ///   returned as null.
        /// </returns>
        public T Get<T>(string name)
        {
            var val = _parameters[Clean(name)].AttachedParam.Value;
            if (val == DBNull.Value)
            {
                if (!ReferenceEquals(default(T), null))
                {
                    throw new ApplicationException("Attempting to cast a DBNull to a non nullable type!");
                }
                return default(T);
            }
            return (T) val;
        }
    }
}
