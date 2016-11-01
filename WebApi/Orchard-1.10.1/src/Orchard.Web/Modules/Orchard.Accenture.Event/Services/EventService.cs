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
//using RaisingStudio.Contents.RepositoryFactory.Services;

namespace Orchard.Accenture.Event.Services
{
    public class EventService : IEventService
    {
        private const string SignalName = CacheAndSignals.EventSignal;
        private const string CacheName = CacheAndSignals.EventCache;

        public const string EventContentTypeName = "Event";

        private readonly IContentManager _contentManager;
        private readonly ICacheManager _cacheManager;
        private readonly ISignals _signals;
        private readonly IOrchardServices _orchardServices;

        public EventService(
            IContentManager contentManager,
            ICacheManager cacheManager,
            ISignals signals,
            IOrchardServices orchardServices
            //IContentsRepositoryFactory repositoryFactory
            )
        {
            _contentManager = contentManager;
            _orchardServices = orchardServices;
            _cacheManager = cacheManager;
            _signals = signals;

            //SetupRepository(repositoryFactory.GetRepository<EventPartRecord>(EventContentTypeName));

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public Localizer T { get; set; }
        public ILogger Logger { get; set; }

        private IRepository<EventPartRecord> _EventRepository;
        public void SetupRepository(
            IRepository<EventPartRecord> EventRepository)
        {
            _EventRepository = EventRepository;
        }


        private void MonitorSignal(AcquireContext<string> ctx)
        {
            ctx.Monitor(_signals.When(SignalName));
        }

        public void TriggerSignal()
        {
            _signals.Trigger(SignalName);
        }


        private EventModel Convert(EventPartRecord record)
        {
            if (record != null)
            {
                return new EventModel
                {
                    Id = record.Id,
                };
            }
            return null;
        }

        private EventPartRecord Convert(EventModel model, EventPartRecord record)
        {
            if (model != null && record != null)
            {
              return record;
            }
            return null;
        }


        private IEnumerable<EventModel> InternalGetEventList()
        {
            return from r in _EventRepository.Table
                    select Convert(r);
        }

        private IEnumerable<EventModel> InternalGetEventList(bool allowCache)
        {
            try
            {
                if (!allowCache)
                {
                    return InternalGetEventList();
                }
                else
                {
                    return _cacheManager.Get(CacheName, ctx =>
                    {
                        MonitorSignal(ctx);
                        return InternalGetEventList().ToList();
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Get Event List failed.");
            }
            return null;
        }

        public IEnumerable<EventModel> GetEventList(EventSearch search = null, EventSort sort = null, bool allowCache = true)
        {

            try
            {
                var q = InternalGetEventList(allowCache);

                #region Search
                if (search != null)
                {
                    q = q.Where
                        (
                            p =>
                                (search.Id == null || p.Id == search.Id)
                        );
                }
                #endregion
                #region Sort
                if (sort != null)
                {
                    if (sort.Id != null)
                    {
                        q = (sort.Id == OrderByDirection.Ascending) ? q.OrderBy(t => t.Id) : q.OrderByDescending(t => t.Id);
                    }
                }
                #endregion
                return q;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Get Event List failed.");
            }
            return null;
        }

        public EventModel GetEvent(int id, bool allowCache = true)
        {
            var q = InternalGetEventList(allowCache);
            return q.SingleOrDefault(p => p.Id == id);
        }


        public int Create(EventModel entity)
        {
            try
            {
                if (entity != null)
                {
                    var record = new EventPartRecord();
                    _EventRepository.Create(Convert(entity, record));
                    int id = record.Id;
                    if (id > 0)
                    {
                        TriggerSignal();
                    }
                    return id;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Create Event failed, entity: {0}", JObject.FromObject(entity));
            }
            return -1;
        }

        public int Update(EventModel entity)
        {
            try
            {
                if (entity != null)
                {
                    int id = entity.Id;
                    var record = _EventRepository.Get(id);
                    _EventRepository.Update(Convert(entity, record));
                    TriggerSignal();
                    return id;
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Update Event failed, entity: {0}", JObject.FromObject(entity));
            }
            return -1;
        }

        public int Delete(int id)
        {
            try
            {
                var record = _EventRepository.Get(id);
                if (record != null)
                {
                    _EventRepository.Delete(record);
                    TriggerSignal();
                    return id;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Delete Event failed, Id: {0}", id);
            }
            return -1;
        }
    }
}