#region Usings

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Ektron.Cms;
using Ektron.Cms.Common;
using Ektron.Cms.Content;
using Ektron.Cms.Framework.Organization;
using Ektron.Cms.Framework.User;

#endregion

namespace GoodlyFere.Import.Ektron.Tests
{
    internal static class EktronTestHelper
    {
        #region Constants and Fields

        private static string _authToken;

        #endregion

        #region Public Properties

        public static string TestFolderName
        {
            get
            {
                return "Import Testing";
            }
        }

        #endregion

        #region Methods

        internal static ContentData AddContent(ContentData content)
        {
            string token = AuthenticateAdmin();
            IContentManager cm = ObjectFactory.GetContent();
            cm.RequestInformation.AuthenticationToken = token;

            content.FolderId = GetFolderData(TestFolderName).Id;
            return cm.Add(content);
        }

        internal static string AuthenticateAdmin()
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                string username = ConfigurationManager.AppSettings["EktronAdminUsername"];
                string password = ConfigurationManager.AppSettings["EktronAdminPassword"];
                UserManager um = new UserManager();
                _authToken = um.Authenticate(username, password);
            }

            return _authToken;
        }

        internal static void DeleteContent(IEnumerable<ContentData> contentItems)
        {
            string token = AuthenticateAdmin();

            IContentManager cm = ObjectFactory.GetContent();
            cm.RequestInformation.AuthenticationToken = token;
            foreach (var contentData in contentItems)
            {
                cm.Delete(contentData.Id);
            }
        }

        internal static ContentData GetContent(long id)
        {
            IContentManager cm = ObjectFactory.GetContent();
            return cm.GetItem(id, true);
        }

        internal static List<ContentData> GetContentByFolderName(string folderName)
        {
            IContentManager cm = ObjectFactory.GetContent();
            ContentCriteria criteria = new ContentCriteria();
            criteria.AddFilter(ContentProperty.FolderName, CriteriaFilterOperator.EqualTo, folderName);
            return cm.GetList(criteria);
        }

        internal static FolderData GetFolderData(string folderName)
        {
            FolderManager fm = new FolderManager();
            FolderCriteria folderCrit = new FolderCriteria();
            folderCrit.AddFilter(
                FolderProperty.FolderName,
                CriteriaFilterOperator.EqualTo,
                folderName);

            FolderData folder = fm.GetList(folderCrit).FirstOrDefault();
            return folder;
        }

        #endregion
    }
}