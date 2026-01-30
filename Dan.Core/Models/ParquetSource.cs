using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Core.Models
{
    public class ParquetSource
    {
        public List<ParquetSourceRecord> Records { get; set; } = new List<ParquetSourceRecord>();
    }

    public class ParquetSourceRecord
    {
        public DateTime TimeStamp { get; set; }
        public string Product { get; set; } = "data.altinn.no";
        public string Environment { get; set; } 
        public string ServiceOwner { get; set; }
        public string ServiceName { get; set; }
        public long ApiCalls { get; set; }
        public long ConsentRequests { get; set; }
        public long NotificationsSent { get; set; }
        public long DatasetsRetrieved { get; set; }

        public string ConsumerHash { get; set; }

        public string Dataset { get; set; }
    }   
}
