using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Alerts
{
    public enum AlertStatus
    {
        Active,     // Активное (не обработанное)
        Acknowledged, // Подтверждено оператором
        Resolved,   // Решено
        FalseAlarm  // Ложное срабатывание
    }
}
