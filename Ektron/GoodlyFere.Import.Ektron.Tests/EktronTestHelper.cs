#region Usings

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Ektron.Cms;
using Ektron.Cms.Common;
using Ektron.Cms.Content;
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

        internal static List<ContentData> GetContentByFolderName(string folderName)
        {
            IContentManager cm = ObjectFactory.GetContent();
            ContentCriteria criteria = new ContentCriteria();
            criteria.AddFilter(ContentProperty.FolderName, CriteriaFilterOperator.EqualTo, folderName);
            return cm.GetList(criteria);
        }

        #endregion
    }
}