#region Usings

using System;
using System.Data;
using System.Linq;
using Common.Logging;
using Ektron.Cms;

#endregion

namespace GoodlyFere.Import.Ektron.Destination
{
    public class SmartFormDestination : ContentDestination
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<SmartFormDestination>();

        #endregion

        #region Methods

        protected override void SaveContent(DataRow row)
        {
            Log.InfoFormat("Saving content with title '{0}' in folder '{1}' with smart form ID {2}", row["title"], row["folderName"], row["smartFormId"]);

            long folderId = GetFolderId(row);

            if (folderId <= 0)
            {
                return;
            }

            ContentData content = new ContentData();
            content.ContType = 1;
            content.Title = row["title"].ToString();
            content.Html = row["html"].ToString();
            content.XmlConfiguration = new XmlConfigData { Id = (long)row["smartFormId"] };
            content.FolderId = folderId;

            try
            {
                ContentManager.Add(content);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to add content with title '{0}'", ex, row["title"]);
                throw;
            }
        }

        protected override bool TableHasValidSchema(DataTable data)
        {
            bool hasValidSchema = base.TableHasValidSchema(data)
                   && HasColumn(data, "smartFormId", typeof(long));

            Log.DebugFormat("Data table has valid schema: {0}", hasValidSchema);
            return hasValidSchema;
        }

        protected override void UpdateContent(DataRow row, ContentData existingItem)
        {
            Log.InfoFormat("Updating content with title '{0}' in folder '{1}' with smart form ID {2}", row["title"], row["folderName"], row["smartFormId"]);

            existingItem.Title = row["title"].ToString();
            existingItem.Html = row["html"].ToString();
            existingItem.XmlConfiguration.Id = (long)row["smartFormId"];

            try
            {
                ContentManager.Update(existingItem);
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Failed to update content with title '{0}' and id {1}", ex, row["title"], row["contentId"]);
                throw;
            }
        }

        #endregion
    }
}