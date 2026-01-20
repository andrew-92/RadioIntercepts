using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models
{
    public partial class Message
    {
        [NotMapped]
        public string CallsignsText
        {
            get
            {
                if (MessageCallsigns == null || MessageCallsigns.Count == 0)
                    return string.Empty;

                return string.Join(", ",
                    MessageCallsigns
                        .Select(x => x.Callsign.Name)
                        .Distinct());
            }
        }
    }
}
