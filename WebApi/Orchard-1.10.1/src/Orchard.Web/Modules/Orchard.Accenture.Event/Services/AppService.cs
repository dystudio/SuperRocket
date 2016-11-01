using Orchard.Accenture.Event.Common;
using Orchard.Accenture.Event.Interfaces;
using Orchard.Accenture.Event.Models;
using Orchard.Accenture.Event.ServiceModels;
using Orchard.Accenture.Event.ViewModels;
using Orchard;
using Orchard.Caching;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using Orchard.Data;
using System.Net.Http;
using Orchard.Settings;
using Orchard.Accenture.Event.Orchard.Odata.Services;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System.Drawing;
using System.IO;

namespace Orchard.Accenture.Event.Services
{
    public class AppService : IAppService
    {

        private readonly IContentManager _contentManager;
        private readonly ICacheManager _cacheManager;
        private readonly IOrchardServices _orchardServices;
        private readonly ISiteService _siteService;
        private readonly IRepository<AppPartRecord> _repository;



        public AppService(
            IRepository<AppPartRecord> repository

            )
        {

            _repository = repository;


            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        public AppPartRecord GetApp(string clientId)
        {
            var app = _repository.Table.FirstOrDefault();
            return app;
        }
    }
}