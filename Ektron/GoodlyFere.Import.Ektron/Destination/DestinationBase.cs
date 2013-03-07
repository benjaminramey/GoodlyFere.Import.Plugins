#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DestinationBase.cs">
// GoodlyFere.Import.Ektron
// 
// Copyright (C) 2013 Benjamin Ramey
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
// http://www.gnu.org/licenses/lgpl-2.1-standalone.html
// 
// You can contact me at ben.ramey@gmail.com.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using Common.Logging;
using Ektron.Cms;
using Ektron.Cms.Common;
using Ektron.Cms.Content;
using Ektron.Cms.Framework.Content;
using Ektron.Cms.Framework.User;
using Ektron.Cms.Search.Expressions;
using GoodlyFere.Import.Ektron.Extensions;
using GoodlyFere.Import.Interfaces;

#endregion

namespace GoodlyFere.Import.Ektron.Destination
{
    public abstract class DestinationBase : IDestination
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<DestinationBase>();
        private readonly string _adminPassword;
        private readonly string _adminUserName;

        #endregion

        #region Constructors and Destructors

        protected DestinationBase(string ektronServicesUrl, string adminUserName, string adminPassword)
        {
            ConfigurationManager.AppSettings["ek_ServicesPath"] = ektronServicesUrl;
            _adminUserName = adminUserName;
            _adminPassword = adminPassword;

            string authToken = Authenticate();
            ContentManager = new ContentManager();
            ContentManager.RequestInformation.AuthenticationToken = authToken;
        }

        #endregion

        #region Properties

        protected ContentManager ContentManager { get; private set; }
        protected DataTable Data { get; set; }

        #endregion

        #region Public Methods

        public abstract bool Receive(DataTable table);

        #endregion

        #region Methods

        protected string Authenticate()
        {
            Log.InfoFormat("Authenticating with username: {0} and password: {1}", _adminUserName, _adminPassword);

            UserManager um = new UserManager();
            return um.Authenticate(_adminUserName, _adminPassword);
        }

        protected List<ContentData> GetExistingContent()
        {
            int numNotNewRows = Data.Rows.Cast<DataRow>().Count(dr => !dr.IsNew());
            Log.InfoFormat("Getting existing content from {0} row(s).", numNotNewRows);
            List<ContentData> existingItems = LookForExistingContent();
            Log.InfoFormat("Found {0} results for existing content.", existingItems.Count);

            if (numNotNewRows != existingItems.Count)
            {
                IEnumerable<long> rowIds =
                    Data.Rows.Cast<DataRow>().Where(dr => !dr.IsNew()).Select(dr => (long)dr["contentId"]);
                IEnumerable<long> resultIds = existingItems.Select(c => c.Id);
                IEnumerable<long> missingIds = rowIds.Except(resultIds);
                Log.WarnFormat("IDs not found with search: {0}", string.Join(",", missingIds));
            }

            return existingItems;
        }

        protected virtual void GetExistingContentFilters(ContentCriteria criteria)
        {
            CriteriaFilterGroup<ContentProperty> group = new CriteriaFilterGroup<ContentProperty>();
            group.Condition = LogicalOperation.Or;

            foreach (DataRow row in Data.Rows.Cast<DataRow>().Where(dr => !dr.IsNew()))
            {
                group.AddFilter(ContentProperty.Id, CriteriaFilterOperator.EqualTo, row["contentid"]);
            }

            criteria.FilterGroups.Add(group);
        }

        protected Expression RollUpExpressions(Queue<Expression> expressions)
        {
            while (expressions.Count > 1)
            {
                OrExpression or = new OrExpression(
                    expressions.Dequeue(),
                    expressions.Dequeue());
                expressions.Enqueue(or);
            }

            if (expressions.Count == 1)
            {
                var expression = expressions.Dequeue();
                return expression;
            }

            return null;
        }

        protected bool TableHasRows()
        {
            bool tableHasRows = Data.Rows.Count > 0;
            Log.DebugFormat("Data table has rows: {0}", tableHasRows);
            return tableHasRows;
        }

        /// <summary>
        ///     Ensures the table has the 'html', 'folderName' and 'title' columns
        /// </summary>
        /// <returns>True if table is valid, false otherwise.</returns>
        protected abstract bool TableHasValidSchema();

        protected virtual void ValidateTable()
        {
            if (!TableHasValidSchema())
            {
                throw new ArgumentException("data", "Data table does not have all of the required columns.");
            }

            if (!TableHasRows())
            {
                throw new ArgumentException("data", "Table has no rows.");
            }
        }

        private List<ContentData> LookForExistingContent()
        {
            ContentCriteria criteria = new ContentCriteria();
            criteria.Condition = LogicalOperation.Or;
            GetExistingContentFilters(criteria);

            return ContentManager.GetList(criteria);
        }

        #endregion
    }
}