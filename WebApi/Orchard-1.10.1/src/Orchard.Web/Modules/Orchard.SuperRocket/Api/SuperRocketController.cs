using Newtonsoft.Json;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using Orchard.SuperRocket.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace Orchard.SuperRocket.Api
{

    public class SuperRocketController : ApiController
    {
        
        private static readonly char[] separator = new[] { '{', '}', ',' };
        private readonly IContentManager _contentManager;
        private readonly IContentTypesService _contentTypesService;

        public SuperRocketController(
            IContentManager contentManager,
            IContentTypesService contentTypesService)
        {
            _contentManager = contentManager;
            _contentTypesService = contentTypesService;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        /// <summary>
        /// GET Event/GetContentTypeDefinition
        /// example: http://localhost/api/SuperRocket/GetContentTypeDefinition?type=User
        /// </summary>
        /// <param name="type">Content Type Name</param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetContentTypeDefinition")]
        public ContentTypeDefinition GetContentTypeDefinition(string type)
        {
            return (from t in _contentManager.GetContentTypeDefinitions()
                    where t.Name == type
                    select t).FirstOrDefault();
        }

        /// <summary>
        /// GET Event/GetContentTypes
        /// example : http://localhost/api/SuperRocket/GetContentTypes
        /// </summary>
        /// <returns>All content type names</returns>
        [HttpGet]
        [Route("GetContentTypes")]
        public IEnumerable<string> GetContentTypes()
        {
            return _contentTypesService.GetContentTypes();
        }
    }
}
