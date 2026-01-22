using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Alerts
{
    public class AlertNotification
    {
        public Alert Alert { get; set; } = null!;
        public bool ShowPopup { get; set; }
        public bool SendEmail { get; set; }
        public bool SendTelegram { get; set; }
        public bool SendWebhook { get; set; }
        public string SoundAlert { get; set; } = string.Empty;
    }
}
