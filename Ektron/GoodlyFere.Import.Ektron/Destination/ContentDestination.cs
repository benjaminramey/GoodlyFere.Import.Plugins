#region Usings

using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System;
using Common.Logging;
using Ektron.Cms;
using Ektron.Cms.Common;
using Ektron.Cms.Content;
using Ektron.Cms.Framework.Content;
using Ektron.Cms.Framework.Organization;
using Ektron.Cms.Framework.User;
using Ektron.Cms.Search;
using Ektron.Cms.Search.Expressions;
using GoodlyFere.Import.Ektron.Extensions;
using GoodlyFere.Import.Interfaces;

#endregion

namespace GoodlyFere.Import.Ektron.Destination
{
    public class ContentDestination : IDestination
    {
        #region Constants and Fields

        protected readonly ContentManager ContentManager;
        private static readonly ILog Log = LogManager.GetLogger<ContentDestination>();
        private readonly Dictionary<string, long> _folderIds;

        #endregion

        #region Constructors and Destructors

        public ContentDestination()
        {
            _folderIds = new Dictionary<string, long>();

            string authToken = Authenticate();
            ContentManager = new ContentManager();
            ContentManager.RequestInformation.AuthenticationToken = authToken;
        }

        #endregion

        #region Public Methods

        public bool Receive(DataTable data)
        {
            Log.InfoFormat("Received data table with name '{0}'", data.TableName);

            ValidateTable(data);
            SaveOrUpdateContentItems(data);

            return false;
        }

        #endregion

        #region Methods

        protected static FolderData GetFolderData(string folderName)
        {
            Log.InfoFormat("Getting folder data for folder with name '{0}'", folderName);

            FolderManager fm = new FolderManager();
            FolderCriteria folderCrit = new FolderCriteria();
            folderCrit.AddFilter(
                FolderProperty.FolderName,
                CriteriaFilterOperator.EqualTo,
                folderName);

            FolderData folder = fm.GetList(folderCrit).FirstOrDefault();
            return folder;
        }

        protected static bool TableHasRows(DataTable data)
        {
            bool tableHasRows = data.Rows.Count > 0;
            Log.DebugFormat("Data table has rows: {0}", tableHasRows);
            return tableHasRows;
        }

        protected long GetFolderId(DataRow row)
        {
            Log.InfoFormat("Getting folder id for folder with name '{0}'", row["folderName"]);

            string folderName = row["folderName"].ToString();
            if (_folderIds.ContainsKey(folderName))
            {
                Log.DebugFormat("Folder id was cached: {0}", _folderIds[folderName]);
                return _folderIds[folderName];
            }

            var folder = GetFolderData(folderName);
            if (folder != null)
            {
                _folderIds.Add(folderName, folder.Id);
                Log.DebugFormat("Folder id was found and added to cache: {0}", folder.Id);
                return folder.Id;
            }

            Log.DebugFormat("Did not find folder with name '{0}'", folderName);
            return -1;
        }

        protected bool HasColumn(DataTable data, string columnName, Type columnType)
        {
            return data.Columns[columnName] != null
                   && data.Columns[columnName].DataType == columnType;
        }

        /// <summary>
        ///     Saves a new content item with the values from the data row.
        ///     The 'title' and 'html' fields are saved. The item is placed
        ///     in the folder found using the 'folderName' column value in the data row.
        /// </summary>
        /// <param name="row">Row with content data.</param>
        protected virtual void SaveContent(DataRow row)
        {
            Log.InfoFormat("Saving content with title '{0}' in folder '{1}'", row["title"], row["folderName"]);
            long folderId = GetFolderId(row);

            if (folderId <= 0)
            {
                return;
            }

            ContentData content = new ContentData();
            content.Title = row["title"].ToString();
            content.Html = row["html"].ToString();
            content.FolderId = folderId;

            try
            {
                ContentManager.Add(content);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to add content with title '{0}'", ex, row["title"]);
            }
        }

        /// <summary>
        ///     Ensures the table has the 'html', 'folderName' and 'title' columns
        /// </summary>
        /// <param name="data">Table to validate schema for.</param>
        /// <returns>True if table is valid, false otherwise.</returns>
        protected virtual bool TableHasValidSchema(DataTable data)
        {
            bool tableHasValidSchema = HasColumn(data, "html", typeof(string))
                   && HasColumn(data, "folderName", typeof(string))
                   && HasColumn(data, "title", typeof(string))
                   && HasColumn(data, "contentId", typeof(long));
            Log.DebugFormat("Table has valid schema: {0}", tableHasValidSchema);
            return tableHasValidSchema;
        }

        /// <summary>
        ///     Updates the existing content item as opposed to adding a new one. The
        ///     'title' and 'html' fields are updated.
        /// </summary>
        /// <param name="row">Row containing data to update in existing content item.</param>
        /// <param name="existingItem">Existing content item to update.</param>
        protected virtual void UpdateContent(DataRow row, ContentData existingItem)
        {
            Log.InfoFormat("Updating content with title '{0}'", row["title"]);
            existingItem.Title = row["title"].ToString();
            existingItem.Html = row["html"].ToString();

            try
            {
                ContentManager.Update(existingItem);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to update content with title '{0}' and id {1}", ex, row["title"], row["contentId"]);
            }
        }

        private static string Authenticate()
        {
            string username = ConfigurationManager.AppSettings["EktronAdminUsername"];
            string password = ConfigurationManager.AppSettings["EktronAdminPassword"];

            Log.InfoFormat("Authenticating with username: {0} and password: {1}", username, password);

            UserManager um = new UserManager();
            return um.Authenticate(username, password);
        }

        private static ContentData CheckForExistingItem(DataRow row, IEnumerable<ContentData> existingItems)
        {
            if (row.IsNew())
            {
                string title = row["title"].ToString();
                string folderName = row["folderName"].ToString();

                return existingItems.FirstOrDefault(ei => ei.Title == title && ei.FolderName == folderName);
            }

            long id = (long)row["contentId"];
            return existingItems.FirstOrDefault(ei => ei.Id == id);
        }

        private static Expression GetExistingContentSearchExpression(DataTable data)
        {
            Queue<Expression> expressions = new Queue<Expression>();

            foreach (DataRow row in data.Rows.Cast<DataRow>().Where(dr => !dr.IsNew()))
            {
                Expression idEqual = SearchContentProperty.Id.EqualTo((long)row["contentId"]);
                expressions.Enqueue(idEqual);
            }

            foreach (DataRow row in data.Rows.Cast<DataRow>().Where(dr => dr.IsNew()))
            {
                Expression and = new AndExpression(
                    SearchContentProperty.Title == row["title"].ToString(),
                    SearchContentProperty.FolderName == row["folderName"].ToString());
                expressions.Enqueue(and);
            }

            return RollUpExpressions(expressions);
        }

        private static Expression RollUpExpressions(Queue<Expression> expressions)
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

        private static SearchResponseData SearchForExistingContent(DataTable data)
        {
            ISearchManager sm = ObjectFactory.GetSearchManager();

            AdvancedSearchCriteria criteria = new AdvancedSearchCriteria();
            criteria.ExpressionTree = GetExistingContentSearchExpression(data);
            criteria.PagingInfo.RecordsPerPage = 10000;

            if (criteria.ExpressionTree != null)
            {
                SearchResponseData response = sm.Search(criteria);
                return response;
            }

            return null;
        }

        private List<ContentData> GetContentListFromSearchResults(SearchResponseData response)
        {
            if (response == null)
            {
                return new List<ContentData>();
            }

            ContentCriteria contentCrit = new ContentCriteria();
            foreach (var result in response.Results)
            {
                contentCrit.AddFilter(ContentProperty.Id, CriteriaFilterOperator.EqualTo, result["contentid"]);
            }

            return ContentManager.GetList(contentCrit);
        }

        private List<ContentData> GetExistingContent(DataTable data)
        {
            SearchResponseData response = SearchForExistingContent(data);
            return GetContentListFromSearchResults(response);
        }

        private void SaveOrUpdateContentItems(DataTable data)
        {
            List<ContentData> existingItems = GetExistingContent(data);
            foreach (var row in data.Rows.Cast<DataRow>().Distinct(new ContentRowComparer()))
            {
                var existingItem = CheckForExistingItem(row, existingItems);
                if (existingItem != null)
                {
                    UpdateContent(row, existingItem);
                }
                else
                {
                    SaveContent(row);
                }
            }
        }

        private void ValidateTable(DataTable data)
        {
            if (!TableHasValidSchema(data))
            {
                throw new ArgumentException("data", "Data table does not have all of the required columns.");
            }

            if (!TableHasRows(data))
            {
                throw new ArgumentException("data", "Table has no rows.");
            }
        }

        #endregion
    }
}