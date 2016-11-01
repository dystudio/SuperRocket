using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard;
using System.Collections;

namespace Orchard.Accenture.Event.Services
{
    public interface IContentTypesService : IDependency
    {
        IEnumerable<string> GetContentTypes();
        Models.ContentItem Get(int id, string[] properties = null);
        dynamic Publish(string name);
        dynamic Query(string name);
        IEnumerable<Models.ContentItem> Query(string name, string[] properties = null);
        IEnumerable<Models.ContentItem> Query(string name, string[] searchColumns, string[] searchOperators, object[] searchValues, string[] logicalOperators, string[] sortColumns, string[] sortOrdering, int start, int rows, out int count, string[] properties = null);
        //int Save(Models.ContentItem value, string name = null, string[] properties = null);
        //int Update(int id, Models.ContentItem value, string[] properties = null);
        //int Delete(int id);
    }
}