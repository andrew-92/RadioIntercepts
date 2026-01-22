using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Alerts
{
    public enum AlertSeverity
    {
        Info,       // Информационное
        Low,        // Низкая важность
        Medium,     // Средняя важность
        High,       // Высокая важность
        Critical    // Критическая важность
    }
}
