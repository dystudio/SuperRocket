using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard;
using System.Collections;

namespace Orchard.Accenture.Event.Services
{
    public interface IOEventService : IDependency
    {
        dynamic LoadApp(string app);
        dynamic LoadEvents(string app, string eid, IEnumerable<string> adGroups);
        dynamic LoadEvents(int? id, IEnumerable<string> adGroups);
        dynamic LoadParticipants();
        dynamic LoadParticipants(string eid, int eventId);
        dynamic LoadParticipants(int? participantId);
        dynamic LoadParticipants(int? id, int? groupId);
        dynamic LoadParticipants(string eid);
        dynamic LoadMultipleParticipants(string eid);
        dynamic LoadAvatar(int? id);
        dynamic LoadSessions(int? id,string eid,IEnumerable<string> adGroups);
        dynamic LoadInfoCards(int? id);
        dynamic LoadInfoCards();
        dynamic LoadPolls(int? id);
        dynamic LoadEvaluationList(int? id);
        dynamic LoadDocuments(int? id, int? groupId,IEnumerable<string> adGroups);
        dynamic LoadParticipantsLayout(int? id);
        dynamic LoadDocumentsLayout(int? id,string eid, IEnumerable<string> adGroups);
        dynamic LoadTerms(string taxonomyName);
        dynamic LoadChildren(int id,int level);
        dynamic LoadParticipantsByGroup(int? id, string name);
    }
}