#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EktronTestHelper.cs">
// GoodlyFere.Import.Ektron.Tests
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

        public static string TestFolderPath
        {
            get
            {
                return "ProductsImportTesting/";
            }
        }

        #endregion

        #region Methods

        internal static ContentData AddContent(ContentData content)
        {
            string token = AuthenticateAdmin();
            IContentManager cm = ObjectFactory.GetContent();
            cm.RequestInformation.AuthenticationToken = token;

            content.FolderId = GetFolderData(TestFolderPath).Id;
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

        internal static List<ContentData> GetContentByFolderPath(string folderPath)
        {
            IContentManager cm = ObjectFactory.GetContent();
            ContentCriteria criteria = new ContentCriteria();
            criteria.AddFilter(ContentProperty.Path, CriteriaFilterOperator.EqualTo, folderPath);
            return cm.GetList(criteria);
        }

        internal static FolderData GetFolderData(string folderPath)
        {
            FolderManager fm = new FolderManager();
            FolderCriteria folderCrit = new FolderCriteria();
            folderCrit.AddFilter(
                FolderProperty.FolderPath,
                CriteriaFilterOperator.EqualTo,
                folderPath);

            FolderData folder = fm.GetList(folderCrit).FirstOrDefault();
            return folder;
        }

        #endregion
    }
}