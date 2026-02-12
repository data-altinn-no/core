using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Core.Models
{

    public class Altinn3ConsentRequestResponse
    {
        public string id { get; set; }
        public string? from { get; set; }
        public string? to { get; set; }
        public string? requiredDelegator { get; set; }
        public string? handledBy { get; set; }
        public DateTime validTo { get; set; }
        public Consentright[] consentRights { get; set; }
        public Requestmessage? requestMessage { get; set; }
        public string status { get; set; }
        public DateTime? consented { get; set; }
        public string? redirectUrl { get; set; }
        public Consentrequestevent[]? consentRequestEvents { get; set; }
        public string? viewUri { get; set; }
        public string? portalViewMode { get; set; }
    }

    public class Requestmessage
    {
        public string additionalProp1 { get; set; }
        public string additionalProp2 { get; set; }
        public string additionalProp3 { get; set; }
    }

    public class Consentright
    {
        public string[] action { get; set; }
        public Resource[] resource { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Consentrequestevent
    {
        public string consentEventID { get; set; }
        public DateTime created { get; set; }
        public string performedBy { get; set; }
        public string eventType { get; set; }
        public string consentRequestID { get; set; }
    }

}