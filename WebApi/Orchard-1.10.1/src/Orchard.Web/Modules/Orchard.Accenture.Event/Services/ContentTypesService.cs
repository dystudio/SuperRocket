using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using Orchard.Security;
using Orchard.Users.Services;
using Orchard;
using Orchard.Logging;
using Orchard.Localization;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;
using Orchard.Core.Common.Models;
using Orchard.Core.Common.Fields;
using Orchard.Fields.Fields;
using Orchard.ContentPicker.Fields;
using Newtonsoft.Json.Linq;
using System.Text;
using Orchard.MediaLibrary.Fields;
using Dolph.SmartContentPicker.Fields;

namespace Orchard.Accenture.Event.Services
{
    public class ContentTypesService : IContentTypesService
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IMembershipService _membershipService;
        private readonly IUserService _userService;
        private readonly IOrchardServices _orchardServices;
        private readonly IContentManager _contentManager;

        public ContentTypesService(
            IAuthenticationService authenticationService, 
            IMembershipService membershipService,
            IUserService userService, 
            IOrchardServices orchardServices,
            IContentManager contentManager) {
            _authenticationService = authenticationService;
            _membershipService = membershipService;
            _userService = userService;
            _orchardServices = orchardServices;
            _contentManager = contentManager;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        private static bool ContainsProperty(string[] properties, string name)
        {
            var hasProperties = (properties != null) && (properties.Length > 0);
            return hasProperties ? properties.Contains(name) : true;
        }

        private Models.ContentItem ConvertContentItem(ContentItem contentItem, string[] properties = null)
        {
            var item = new Models.ContentItem
              {
                  Id = contentItem.Id,
                  Title = ContainsProperty(properties, "Title") ? contentItem.Is<TitlePart>() ? contentItem.As<TitlePart>().Title : null : null,
                  Body = ContainsProperty(properties, "Body") ? contentItem.Is<BodyPart>() ? contentItem.As<BodyPart>().Text : null : null,
                  Owner = ContainsProperty(properties, "Owner") ? contentItem.Is<CommonPart>() ? contentItem.As<CommonPart>().Owner != null ? contentItem.As<CommonPart>().Owner.UserName : null : null : null,
                  CreateTime = ContainsProperty(properties, "CreateTime") ? contentItem.Is<CommonPart>() ? contentItem.As<CommonPart>().CreatedUtc : null : null
              };
            var contentPart = contentItem.Parts.SingleOrDefault(p => p.PartDefinition.Name == contentItem.TypeDefinition.Name);
            if ((contentPart != null) && (contentPart.Fields.Count() > 0))
            {
                item.Fields = new Dictionary<string, object>();
                foreach (var field in contentPart.Fields)
                {
                    if (ContainsProperty(properties, field.Name))
                    {
                        object value = GetContentFieldValue(field);
                        item.Fields.Add(field.Name, value);
                    }
                }
            }
            return item;
        }

        private object GetContentFieldValue(ContentField field)
        {
            object value = null;
            #region Check Fields
            if (field is TextField)
            {
                value = (field as TextField).Value;
            }
            else if (field is BooleanField)
            {
                value = (field as BooleanField).Value;
            }
            else if (field is DateTimeField)
            {
                value = (field as DateTimeField).DateTime;
            }
            else if (field is EnumerationField)
            {
                value = (field as EnumerationField).Value;
            }
            else if (field is InputField)
            {
                value = (field as InputField).Value;
            }
            else if (field is LinkField)
            {
                value = (field as LinkField).Value;
            }
            else if (field is MediaLibraryPickerField)
            {
                value = (field as MediaLibraryPickerField).FirstMediaUrl;
            }
            else if (field is NumericField)
            {
                value = (field as NumericField).Value;
            }
            else if (field is ContentPickerField)
            {
                //value = (field as ContentPickerField).Ids;
                // for IComparable.
                value = string.Join(",", (field as ContentPickerField).Ids);
            }
            else if (field is SmartContentPickerField)
            {
                //value = (field as ContentPickerField).Ids;
                // for IComparable.
                value = string.Join(",", (field as SmartContentPickerField).Ids);
            }
            #endregion
            return value;
        }

        private void ConvertContentItem(ContentItem contentItem, Models.ContentItem value, string[] properties = null)
        {
            if (contentItem.Is<TitlePart>() && ContainsProperty(properties, "Title"))
            {
                contentItem.As<TitlePart>().Title = value.Title;
            }
            if (contentItem.Is<BodyPart>() && ContainsProperty(properties, "Body"))
            {
                contentItem.As<BodyPart>().Text = value.Body;
            }
            if (contentItem.Is<CommonPart>())
            {
                if (ContainsProperty(properties, "Owner"))
                {
                    if (!string.IsNullOrWhiteSpace(value.Owner))
                    {
                        var user = _membershipService.GetUser(value.Owner);
                        contentItem.As<CommonPart>().Owner = user;
                    }
                    else
                    {
                        contentItem.As<CommonPart>().Owner = null;
                    }
                }
                if (ContainsProperty(properties, "CreateTime"))
                {
                    contentItem.As<CommonPart>().CreatedUtc = value.CreateTime;
                }
                var contentPart = contentItem.Parts.SingleOrDefault(p => p.PartDefinition.Name == contentItem.TypeDefinition.Name);
                if ((contentPart != null) && (contentPart.Fields.Count() > 0))
                {
                    foreach (var fieldName in value.Fields.Keys)
                    {
                        if (ContainsProperty(properties, fieldName))
                        {
                            var contentField = contentPart.Fields.SingleOrDefault(f => f.Name == fieldName);
                            if (contentField != null)
                            {
                                SetContentFieldValue(contentField, value.Fields[fieldName]);
                            }
                        }
                    }
                }
            }
        }

        private void SetContentFieldValue(ContentField field, object value)
        {
            #region Check Fields
            if (field is TextField)
            {
                (field as TextField).Value = Convert.ToString(value);
            }
            else if (field is BooleanField)
            {
                if (value != null)
                {
                    if (value is string)
                    {
                        (field as BooleanField).Value = string.IsNullOrWhiteSpace((string)value) ? null : (bool?)Convert.ToBoolean(value);
                    }
                    else if (value is bool)
                    {
                        (field as BooleanField).Value = (bool)value;
                    }
                    else if (value is bool?)
                    {
                        (field as BooleanField).Value = (bool?)value;
                    }
                    else
                    {
                        (field as BooleanField).Value = Convert.ToBoolean(value);
                    }
                }
                else
                {
                    (field as BooleanField).Value = null;
                }
            }
            else if (field is DateTimeField)
            {
                (field as DateTimeField).DateTime = Convert.ToDateTime(value);
            }
            else if (field is EnumerationField)
            {
                (field as EnumerationField).Value = Convert.ToString(value);
            }
            else if (field is InputField)
            {
                (field as InputField).Value = Convert.ToString(value);
            }
            else if (field is LinkField)
            {
                (field as LinkField).Value = Convert.ToString(value);
            }
            else if (field is MediaLibraryPickerField)
            {
               // (field as MediaLibraryPickerField).FirstMediaUrl = Convert.ToString(value);
            }
            else if (field is NumericField)
            {
                if (value != null)
                {
                    if (value is string)
                    {
                        (field as NumericField).Value = string.IsNullOrWhiteSpace((string)value) ? null : (decimal?)Convert.ToDecimal(value);
                    }
                    else if (value is decimal)
                    {
                        (field as NumericField).Value = (decimal)value;
                    }
                    else if (value is decimal?)
                    {
                        (field as NumericField).Value = (decimal?)value;
                    }
                    else
                    {
                        (field as NumericField).Value = Convert.ToDecimal(value);
                    }
                }
                else
                {
                    (field as NumericField).Value = null;
                }
            }
            else if (field is ContentPickerField)
            {
                if (value != null)
                {
                    if (value is int[])
                    {
                        (field as ContentPickerField).Ids = (int[])value;
                    }
                    else if (value is string)
                    {
                        if (string.IsNullOrWhiteSpace((string)value))
                        {
                            (field as ContentPickerField).Ids = new int[0];
                        }
                        else
                        {
                            char[] separator = new[] { '{', '}', ',' };
                            (field as ContentPickerField).Ids = ((string)value).Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                        }
                    }
                    else if (value is string[])
                    {
                        (field as ContentPickerField).Ids = ((string[])value).Select(int.Parse).ToArray();
                    }
                    else if (value is object[])
                    {
                        (field as ContentPickerField).Ids = ((object[])value).Select(Convert.ToInt32).ToArray();
                    }
                    else if (value is JArray)
                    {
                        (field as ContentPickerField).Ids = ((JArray)value).Select((jValue) => (int)jValue).ToArray();
                    }
                    else
                    {
                        throw new NotSupportedException(string.Format("{0} is not supported for ContentPickerField", value));
                    }
                }
                else
                {
                    (field as ContentPickerField).Ids = null;
                }
            }
            #endregion
        }


        public IEnumerable<string> GetContentTypes()
        {
            try
            {
                var contentTypes = _contentManager.GetContentTypeDefinitions();
                return from c in contentTypes
                       select c.Name;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while get content types.");
                throw;
            }
        }

        public Models.ContentItem Get(int id, string[] properties = null)
        {
            try
            {
                var contentItem = _contentManager.Get(id);
                return ConvertContentItem(contentItem, properties);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while get content item for id: [{0}].", id);
                throw;
            }
        }

        public IEnumerable<Models.ContentItem> Query(string name, string[] properties = null)
        {
            try
            {
                var contentItems = _contentManager.Query(name);
                return from c in contentItems.List()
                       select ConvertContentItem(c, properties);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while query content items for ContentType named: [{0}].", name);
                throw;
            }
        }
        public dynamic Query(string name)
        {
            try
            {
                var contentItems = _contentManager.Query(name);
                return contentItems.List();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while query content items for ContentType named: [{0}].", name);
                throw;
            }

        }
        public dynamic Publish(string name)
        {
            try
            {
                var contentItems = _contentManager.Query(name);
                int count = 0;
                foreach (var item in contentItems.List())
                {
                    _contentManager.Publish(item);
                    count++;
                }
                return count;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while Publish content items for ContentType named: [{0}].", name);
                throw;
            }

        }
        private static bool IsAccepted(Models.ContentItem contentItem, string[] searchColumns, string[] searchOperators, object[] searchValues, string[] logicalOperators)
        {
            var result = false;
            for (int i = 0; i < searchColumns.Length; i++)
            {
                var resultRight = IsAccepted(contentItem, searchColumns[i], searchOperators[i], searchValues[i]);
                if ((logicalOperators != null) && (logicalOperators.Length > i))
                {
                    var logicalOperator = logicalOperators[i];
                    if (!string.IsNullOrWhiteSpace(logicalOperator))
                    {
                        switch (logicalOperator.Trim().ToLower())
                        {
                            case "and":
                                {
                                    result = result && resultRight;
                                    break;
                                }
                            case "or":
                                {
                                    result = result || resultRight;
                                    break;
                                }
                        }
                    }
                    else
                    {
                        result = result && resultRight;
                    }
                }
                else
                {
                    result = resultRight;
                }
                if (result)
                {
                    break;
                }
            }
            return result;
        }

        private static bool IsAccepted(Models.ContentItem contentItem, string searchColumn, string searchOperator, object searchValue)
        {
            if (!string.IsNullOrWhiteSpace(searchOperator))
            {
                object columnValue = GetContentItemValue(contentItem, searchColumn);
                #region
                switch (searchOperator.Trim().ToLower())
                {
                    #region compare
                    case "eq":
                        {
                            return Compare(columnValue, searchValue) == 0;
                            break;
                        }
                    case "ne":
                        {
                            return Compare(columnValue, searchValue) != 0;
                            break;
                        }
                    case "lt":
                        {
                            return Compare(columnValue, searchValue) == -1;
                            break;
                        }
                    case "le":
                        {
                            return Compare(columnValue, searchValue) == -1 || Compare(columnValue, searchValue) == 0;
                            break;
                        }
                    case "gt":
                        {
                            return Compare(columnValue, searchValue) == 1;
                            break;
                        }
                    case "ge":
                        {
                            return Compare(columnValue, searchValue) == 1 || Compare(columnValue, searchValue) == 0;
                            break;
                        }
                    #endregion
                    #region string
                    case "bw":
                        {
                            return Convert.ToString(columnValue).StartsWith(Convert.ToString(searchValue));
                            break;
                        }
                    case "bn":
                        {
                            return !Convert.ToString(columnValue).StartsWith(Convert.ToString(searchValue));
                            break;
                        }
                    case "ew":
                        {
                            return Convert.ToString(columnValue).EndsWith(Convert.ToString(searchValue));
                            break;
                        }
                    case "en":
                        {
                            return !Convert.ToString(columnValue).EndsWith(Convert.ToString(searchValue));
                            break;
                        }
                    case "cn":
                        {
                            return Convert.ToString(columnValue).Contains(Convert.ToString(searchValue));
                            break;
                        }
                    case "nc":
                        {
                            return !Convert.ToString(columnValue).Contains(Convert.ToString(searchValue));
                            break;
                        }
                    #endregion
                    #region null
                    case "nu":
                        {
                            return columnValue == null;
                            break;
                        }
                    case "nn":
                        {
                            return columnValue != null;
                            break;
                        }
                    #endregion
                    #region in
                    case "in":
                        {
                            return Contains(searchValue, columnValue);
                            break;
                        }
                    case "ni":
                        {
                            return !Contains(searchValue, columnValue);
                            break;
                        }
                    #endregion
                }
                #endregion
            }
            return false;
        }

        private static object GetContentItemValue(Models.ContentItem contentItem, string searchColumn)
        {
            if(searchColumn == "Id")
            {
                return contentItem.Id;
            }
            else if (searchColumn == "Title")
            {
                return contentItem.Title;
            }
            else if(searchColumn == "Body")
            {
                return contentItem.Body;
            }
            else if (searchColumn == "Owner")
            {
                return contentItem.Owner;
            }
            else if (searchColumn == "CreateTime")
            {
                return contentItem.CreateTime;
            }
            else if (searchColumn.StartsWith("Fields."))
            {
                var searchField = searchColumn.Substring("Fields.".Length);
                if (contentItem.Fields.ContainsKey(searchField))
                {
                    return contentItem.Fields[searchField];
                }
            }
            else
            {
                if (contentItem.Fields.ContainsKey(searchColumn))
                {
                    return contentItem.Fields[searchColumn];
                }
            }
            return null;
        }

        private static int Compare(object columnValue, object searchValue)
        {
            if (columnValue != null)
            {
                if (columnValue is string)
                {
                    return string.Compare((string)columnValue, Convert.ToString(searchValue));
                }
                else if ((columnValue is int) || (columnValue is int?))
                {
                    return (int)columnValue == Convert.ToInt32(searchValue) ? 0 : (int)columnValue > Convert.ToInt32(searchValue) ? 1 : -1;
                }
                else if ((columnValue is bool) || (columnValue is bool?))
                {
                    return (bool)columnValue == Convert.ToBoolean(searchValue) ? 0 : int.MaxValue;
                }
                else if ((columnValue is DateTime) || (columnValue is DateTime?))
                {
                    return DateTime.Compare((DateTime)columnValue, Convert.ToDateTime(searchValue));
                }
                else if ((columnValue is decimal) || (columnValue is decimal?))
                {
                    return decimal.Compare((decimal)columnValue, Convert.ToDecimal(searchValue));
                }
                else if (columnValue is int[])
                {
                    return Compare((int[])columnValue, searchValue);
                }
                else
                {
                    return columnValue == searchValue ? 0 : int.MaxValue;
                }
            }
            else
            {
                return string.IsNullOrWhiteSpace(Convert.ToString(searchValue)) ? 0 : int.MaxValue;
            }
        }

        private static int Compare(int[] columnValue, object searchValue)
        {
            return string.Compare(string.Join(",", columnValue), Convert.ToString(searchValue));
        }

        private static bool Contains(object searchValue, object columnValue)
        {
            return Convert.ToString(searchValue).Contains(Convert.ToString(columnValue));
        }

        public IEnumerable<Models.ContentItem> Query(string name, string[] searchColumns, string[] searchOperators, object[] searchValues, string[] logicalOperators, string[] sortColumns, string[] sortOrderings, int start, int rows, out int count, string[] properties = null)
        {
            var contentItems = _contentManager.Query(name);
            var query = from c in contentItems.List()
                        select ConvertContentItem(c, properties);
            var filteredQuery = query;
            if ((searchColumns != null) && (searchColumns.Length > 0))
            {
                filteredQuery = query.Where((c) =>
                   {
                       return IsAccepted(c, searchColumns, searchOperators, searchValues, logicalOperators);
                   });
            }
            count = filteredQuery.Count();
            var orderedQuery = filteredQuery.AsQueryable();
            if ((sortColumns != null) && (sortColumns.Length > 0))
            {
                string[] propertyNames  = new []{"Id", "Title", "Body", "Owner", "CreateTime"};
                StringBuilder orderingExpression = new StringBuilder();
                for (int i = 0; i < sortColumns.Length; i++)
                {
                    var sortColumn = sortColumns[i];
                    if ((!sortColumn.StartsWith("Fields.")) && (!propertyNames.Contains(sortColumn)))
                    {
                        sortColumn = "Fields." + sortColumn;
                    }
                    if (sortColumn.StartsWith("Fields"))
                    {
                        if ((sortOrderings != null) && (sortOrderings.Length > i))
                        {
                            var sortOrdering = sortOrderings[i];
                            orderingExpression.AppendFormat("Fields[\"{0}\"] {1}", sortColumn.Substring("Fields.".Length), sortOrdering);
                        }
                        else
                        {
                            orderingExpression.AppendFormat("Fields[\"{0}\"] {1}", sortColumn.Substring("Fields.".Length), "asc");
                        }
                    }
                    else
                    {
                        if ((sortOrderings != null) && (sortOrderings.Length > i))
                        {
                            var sortOrdering = sortOrderings[i];
                            orderingExpression.AppendFormat("{0} {1}", sortColumn, sortOrdering);
                        }
                        else
                        {
                            orderingExpression.AppendFormat("{0} {1}", sortColumn, "asc");
                        }
                    }
                }
                orderedQuery = orderedQuery.OrderBy(orderingExpression.ToString());
            }
            return orderedQuery.Skip(start).Take(rows);
        }

        //public int Save(Models.ContentItem value, string name = null, string[] properties = null)
        //{
        //    try
        //    {
        //        var contentItem = _contentManager.Get(value.Id);
        //        if (contentItem == null)
        //        {
        //            contentItem = _contentManager.New(name);
        //            _contentManager.Create(contentItem);
        //        }
        //        ConvertContentItem(contentItem, value, properties);
        //        _contentManager.Flush();
        //        return contentItem.Id;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex, "An error occurred while save c ontent item for ContentType named: [{0}].", name);
        //        throw;
        //    }
        //}

        //public int Update(int id, Models.ContentItem value, string[] properties = null)
        //{
        //    try
        //    {
        //        var contentItem = _contentManager.Get(id);
        //        ConvertContentItem(contentItem, value, properties);
        //        _contentManager.Flush();
        //        return id;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex, "An error occurred while update content item for id: [{0}].", id);
        //        throw;
        //    }
        //}

        //public int Delete(int id)
        //{
        //    try
        //    {
        //        _contentManager.Remove(_contentManager.Get(id));
        //        _contentManager.Flush();
        //        return id;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex, "An error occurred while delete content item for id: [{0}].", id);
        //        throw;
        //    }
        //}
    }
}