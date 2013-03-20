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
using System.ServiceModel;
using System.Threading;
using Common.Logging;
using Ektron.Cms;
using Ektron.Cms.Common;
using Ektron.Cms.Content;
using Ektron.Cms.Framework.Content;
using Ektron.Cms.Framework.User;
using GoodlyFere.Import.Ektron.Extensions;
using GoodlyFere.Import.Ektron.Tools;
using GoodlyFere.Import.Interfaces;

#endregion

namespace GoodlyFere.Import.Ektron.Destination
{
    public abstract class DestinationBase : IDestination
    {
        #region Constants and Fields

        private const int MaxGroupsInCriteria = 20;

        private static readonly ILog Log = LogManager.GetLogger<DestinationBase>();
        private readonly string _adminPassword;
        private readonly string _adminUserName;
        private readonly object _authTokenLock = new object();
        private readonly object _authenticatingLock = new object();
        private bool _authenticating;

        #endregion

        #region Constructors and Destructors

        protected DestinationBase(string ektronServicesUrl, string adminUserName, string adminPassword)
        {
            SetServicesPath(ektronServicesUrl);

            _adminUserName = adminUserName;
            _adminPassword = adminPassword;

            Authenticate();
        }

        #endregion

        #region Properties

        protected string AuthToken { get; set; }

        protected ContentManager ContentManager
        {
            get
            {
                ContentManager mgr = new ContentManager();
                mgr.RequestInformation.AuthenticationToken = AuthToken;
                return mgr;
            }
        }

        protected DataTable Data { get; set; }

        protected bool HasAuthentication
        {
            get
            {
                lock (_authTokenLock)
                {
                    return !string.IsNullOrWhiteSpace(AuthToken);
                }
            }
        }

        #endregion

        #region Public Methods

        public abstract bool Receive(DataTable table);

        #endregion

        #region Methods

        protected static ContentData CheckForExistingItem(DataRow row, IEnumerable<ContentData> existingItems)
        {
            row.LogContentInfo("checking for existence in ektron.");
            ContentData item;

            if (row.IsNew())
            {
                row.LogContentInfo("no contentId found, checking by title and folder path.");
                string title = DestinationHelper.EncodeTitle(row["title"].ToString());
                string folderPath = row["folderPath"].ToString();

                item = existingItems.FirstOrDefault(ei => ei.Title == title && ei.Path == folderPath);
            }
            else
            {
                long id = (long)row["contentId"];
                item = existingItems.FirstOrDefault(ei => ei.Id == id);
            }

            if (item != null)
            {
                row.LogContentInfo("found existing item.");
            }
            else
            {
                row.LogContentInfo("did not find existing item.");
            }

            return item;
        }

        protected static void LogUpdateError(DataRow row, Exception ex)
        {
            row.LogContentError(
                "failed to update with id {0}: {1}",
                ex,
                row["contentId"],
                ex.Message);
        }

        protected void Authenticate()
        {
            lock (_authenticatingLock)
            {
                if (_authenticating)
                {
                    return;
                }

                _authenticating = true;
            }

            Log.InfoFormat("Authenticating with username: {0} and password: {1}", _adminUserName, _adminPassword);

            lock (_authTokenLock)
            {
                UserManager um = new UserManager();
                AuthToken = um.Authenticate(_adminUserName, _adminPassword);
            }

            lock (_authenticatingLock)
            {
                _authenticating = false;
            }
        }

        protected void DoContentAdd(DataRow row, ContentData content, short timeouts = 0, bool failOnFault = false)
        {
            try
            {
                if (HasAuthentication)
                {
                    ContentManager.Add(content);
                    row.LogContentInfo("saved successfully with id {0}.", content.Id);
                }
                else
                {
                    row.LogContentWarn("does not have authentication.");
                }
            }
            catch (TimeoutException te)
            {
                if (timeouts < 10)
                {
                    row.LogContentInfo("save timed out.  Trying again.");
                    Thread.Sleep(1000);
                    DoContentAdd(row, content, ++timeouts);
                }
                else
                {
                    row.LogContentError("failed to save.", te);
                }
            }
            catch (FaultException fe)
            {
                if (!failOnFault
                    && fe.Message.Contains("The current user does not have permission to carry out this request"))
                {
                    row.LogContentWarn("had authentication error.  Re-authenticating then retrying.");
                    Authenticate();
                    DoContentAdd(row, content, failOnFault: true);
                }
                else
                {
                    row.LogContentError("failed to save.", fe);
                }
            }
            catch (CommunicationException ce)
            {
                if (!failOnFault)
                {
                    row.LogContentWarn("had communication error.  Waiting and then retrying.");
                    Thread.Sleep(10000);
                    DoContentAdd(row, content, failOnFault: true);
                }
                else
                {
                    LogUpdateError(row, ce);
                }
            }
            catch (Exception ex)
            {
                row.LogContentError("failed to save.", ex);
            }
        }

        protected void DoContentUpdate(
            DataRow row, ContentData existingItem, short timeouts = 0, bool failOnFault = false)
        {
            try
            {
                if (HasAuthentication)
                {
                    ContentManager.Update(existingItem);
                    row.LogContentInfo("updated successfully with id {0}.", existingItem.Id);
                }
                else
                {
                    row.LogContentWarn("does not have authentication.");
                }
            }
            catch (TimeoutException te)
            {
                if (timeouts < 10)
                {
                    row.LogContentInfo("update timed out.  Trying again.");
                    Thread.Sleep(1000);
                    DoContentUpdate(row, existingItem, ++timeouts);
                }
                else
                {
                    LogUpdateError(row, te);
                }
            }
            catch (FaultException fe)
            {
                if (!failOnFault
                    && fe.Message.Contains("The current user does not have permission to carry out this request"))
                {
                    Authenticate();
                    DoContentUpdate(row, existingItem, failOnFault: true);
                }
                else
                {
                    LogUpdateError(row, fe);
                }
            }
            catch (Exception ex)
            {
                LogUpdateError(row, ex);
            }
        }

        protected List<ContentData> GetExistingContent()
        {
            List<ContentData> existingItems = LookForExistingContent();
            Log.InfoFormat("Found {0} results for existing content.", existingItems.Count);

            if (existingItems.Count > 0)
            {
                IEnumerable<string> rowTitles =
                    Data.Rows.Cast<DataRow>().Select(dr => DestinationHelper.EncodeTitle(dr["title"].ToString()));
                IEnumerable<string> resultTitles = existingItems.Select(c => c.Title).ToArray();

                if (!resultTitles.Any())
                {
                    Log.WarnFormat("No existing items were found.");
                }
                else
                {
                    IEnumerable<string> missingTitles = rowTitles.Except(resultTitles).ToArray();
                    if (missingTitles.Any())
                    {
                        Log.WarnFormat("Items not found with search: {0}", string.Join(",", missingTitles));
                    }
                }
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

            if (group.Filters.Any())
            {
                criteria.FilterGroups.Add(group);
            }
        }

        protected void ManageThreads(
            IEnumerable<List<DataRow>> threadGroups,
            List<ContentData> existingItems)
        {
            ThreadManager threadManager = new ThreadManager();
            foreach (var threadGroupRows in threadGroups)
            {
                var rows = threadGroupRows;
                Action action = () => RowGroupSaveOrUpdate(rows, existingItems);

                threadManager.RunWithAction(action);
            }

            threadManager.WaitForCompletion();
        }

        protected abstract void RowGroupSaveOrUpdate(List<DataRow> rows, List<ContentData> existingItems);

        protected void SaveOrUpdateContentItems()
        {
            Log.InfoFormat("Saving or updating each row.");
            List<ContentData> existingItems = GetExistingContent();
            IEnumerable<List<DataRow>> threadGroups = DestinationHelper.MakeThreadGroups(Data.Rows.Cast<DataRow>());

            ManageThreads(threadGroups, existingItems);
        }

        protected bool TableHasRows()
        {
            bool tableHasRows = Data.Rows.Count > 0;
            Log.InfoFormat("Data table has rows: {0}", tableHasRows);
            return tableHasRows;
        }

        /// <summary>
        ///     Ensures the table has the 'html', 'folderPath' and 'title' columns
        /// </summary>
        /// <returns>True if table is valid, false otherwise.</returns>
        protected abstract bool TableHasValidSchema();

        protected virtual void ValidateTable()
        {
            Log.InfoFormat("Validating table '{0}'", Data.TableName);
            if (!TableHasValidSchema())
            {
                throw new ArgumentException("Data table does not have all of the required columns.", "data");
            }

            if (!TableHasRows())
            {
                throw new ArgumentException("Table has no rows.", "data");
            }
        }

        private static void SetServicesPath(string ektronServicesUrl)
        {
            Log.InfoFormat("Saving ektron services url: {0}", ektronServicesUrl);
            ConfigurationManager.AppSettings["ek_ServicesPath"] = ektronServicesUrl;
        }

        private List<ContentData> LookForExistingContent()
        {
            ContentCriteria criteria = new ContentCriteria
                {
                    PagingInfo = { RecordsPerPage = 10000 },
                    Condition = LogicalOperation.Or
                };
            GetExistingContentFilters(criteria);

            // remove groups with no filters, they mess up search
            CriteriaFilterGroup<ContentProperty>[] groupsWithFilters =
                criteria.FilterGroups.Where(fg => fg.Filters.Any()).ToArray();

            List<ContentData> contentList = new List<ContentData>();
            if (groupsWithFilters.Count() > MaxGroupsInCriteria)
            {
                for (int i = 0; i < groupsWithFilters.Length / MaxGroupsInCriteria + 1; i++)
                {
                    var groups = groupsWithFilters.Skip(i * MaxGroupsInCriteria).Take(MaxGroupsInCriteria).ToArray();
                    if (groups.Any())
                    {
                        criteria.FilterGroups.Clear();
                        criteria.FilterGroups.AddRange(groups);
                        contentList.AddRange(ContentManager.GetList(criteria));
                    }
                }
            }
            else
            {
                criteria.FilterGroups.Clear();
                criteria.FilterGroups.AddRange(groupsWithFilters);
                contentList.AddRange(ContentManager.GetList(criteria));
            }

            return contentList;
        }

        #endregion
    }
}