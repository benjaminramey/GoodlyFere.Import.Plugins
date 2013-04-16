#region Usings

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Common.Logging;
using Ektron.Cms;
using Ektron.Cms.Common;
using Ektron.Cms.Framework.Organization;

#endregion

namespace GoodlyFere.Import.Ektron.Tools
{
    public class FolderUtil
    {
        #region Constants and Fields

        private static readonly object FolderIdCacheLock = new object();
        private static readonly Dictionary<string, long> FolderIds;
        private static readonly ILog Log = LogManager.GetLogger<FolderUtil>();

        #endregion

        #region Constructors and Destructors

        static FolderUtil()
        {
            FolderIds = new Dictionary<string, long>();
        }

        #endregion

        #region Public Methods

        public FolderData GetFolderData(string folderPath)
        {
            Log.InfoFormat("Getting folder with path '{0}'", folderPath);

            FolderManager fm = new FolderManager();
            FolderCriteria folderCrit = new FolderCriteria();
            folderCrit.AddFilter(
                FolderProperty.FolderPath,
                CriteriaFilterOperator.EqualTo,
                folderPath);

            FolderData folder = fm.GetList(folderCrit).FirstOrDefault();
            return folder;
        }

        public long GetFolderId(DataRow row, string folderPathColumnName = "folderPath")
        {
            Log.InfoFormat("Getting folder id with path '{0}'", row[folderPathColumnName]);

            string folderPath = row[folderPathColumnName].ToString();
            lock (FolderIdCacheLock)
            {
                if (FolderIds.ContainsKey(folderPath))
                {
                    Log.InfoFormat("Folder id was cached: {0}", FolderIds[folderPath]);
                    return FolderIds[folderPath];
                }

                var folder = GetFolderData(folderPath);
                if (folder != null)
                {
                    FolderIds.Add(folderPath, folder.Id);
                    Log.InfoFormat("Folder id was found and added to cache: {0}", folder.Id);
                    return folder.Id;
                }
            }

            Log.ErrorFormat("Did not find folder with path '{0}'", folderPath);
            return -1;
        }

        #endregion
    }
}