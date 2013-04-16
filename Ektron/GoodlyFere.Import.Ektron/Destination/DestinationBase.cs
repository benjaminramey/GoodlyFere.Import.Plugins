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

        protected const int TimeoutWait = 30000;
        private const int MaxFiltersInCriteria = 50;
        private static readonly ILog Log = LogManager.GetLogger<DestinationBase>();
        private readonly Authenticator _authenticator;

        #endregion

        #region Constructors and Destructors

        protected DestinationBase(string ektronServicesUrl, string adminUserName, string adminPassword)
        {
            SetServicesPath(ektronServicesUrl);

            _authenticator = new Authenticator(adminUserName, adminPassword);
        }

        #endregion

        #region Properties

        protected string AuthToken
        {
            set
            {
                _authenticator.AuthToken = value;
            }
            get
            {
                return _authenticator.AuthToken;
            }
        }

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
                return _authenticator.HasAuthentication;
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

        public void Authenticate()
        {
            _authenticator.Authenticate();
        }

        protected virtual void DoAPIAddCall(ContentData content)
        {
            ContentManager.Add(content);
        }

        protected virtual void DoAPIUpdateCall(ContentData existingItem)
        {
            ContentManager.Update(existingItem);
        }

        protected void DoContentAdd(DataRow row, ContentData content, short timeouts = 0, bool failOnFault = false)
        {
            try
            {
                if (HasAuthentication)
                {
                    DoAPIAddCall(content);
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
                    Thread.Sleep(TimeoutWait);
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
                    Thread.Sleep(TimeoutWait);
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
                    DoAPIUpdateCall(existingItem);
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
                    Thread.Sleep(TimeoutWait);
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
            catch (CommunicationException ce)
            {
                if (!failOnFault)
                {
                    row.LogContentWarn("had communication error.  Waiting and then retrying.");
                    Thread.Sleep(TimeoutWait);
                    DoContentUpdate(row, existingItem, failOnFault: true);
                }
                else
                {
                    LogUpdateError(row, ce);
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
            var rowGroups = Data.Rows.Cast<DataRow>().Where(dr => !dr.IsNew())
                             .Select((r, i) => new { Index = i, Row = r })
                             .GroupBy(obj => Math.Truncate(obj.Index / 10.0))
                             .Select(grp => grp.Select(v => v.Row).ToList());

            foreach (var rowGroup in rowGroups)
            {
                CriteriaFilterGroup<ContentProperty> critGroup = new CriteriaFilterGroup<ContentProperty>();
                critGroup.Condition = LogicalOperation.Or;

                foreach (DataRow row in rowGroup)
                {
                    critGroup.AddFilter(ContentProperty.Id, CriteriaFilterOperator.EqualTo, row["contentid"]);
                }

                criteria.FilterGroups.Add(critGroup);
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
            List<CriteriaFilterGroup<ContentProperty>> groupsWithFilters =
                criteria.FilterGroups.Where(fg => fg.Filters.Any()).ToList();
            int numFilters = groupsWithFilters.SelectMany(g => g.Filters).Count();

            List<ContentData> contentList = new List<ContentData>();
            Action<List<CriteriaFilterGroup<ContentProperty>>> doSearch = groups =>
                {
                    criteria.FilterGroups.Clear();
                    criteria.FilterGroups.AddRange(groups);
                    contentList.AddRange(ContentManager.GetList(criteria));
                };

            if (numFilters > MaxFiltersInCriteria)
            {
                var groups = new List<CriteriaFilterGroup<ContentProperty>>();
                int currentFilterCount = 0;
                foreach (var group in groupsWithFilters)
                {
                    if (currentFilterCount <= MaxFiltersInCriteria
                        && ((currentFilterCount + group.Filters.Count) <= MaxFiltersInCriteria
                              || groups.Count == 0))
                    {
                        currentFilterCount += group.Filters.Count;
                        groups.Add(group);
                        continue;
                    }

                    doSearch(groups);
                    currentFilterCount = group.Filters.Count;
                    groups.Clear();
                    groups.Add(group);
                }

                if (groups.Any())
                {
                    doSearch(groups);
                }
            }
            else
            {
                doSearch(groupsWithFilters);
            }

            return contentList;
        }

        #endregion
    }
}