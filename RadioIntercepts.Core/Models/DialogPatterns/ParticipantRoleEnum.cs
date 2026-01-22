using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.DialogPatterns
{
    public enum ParticipantRole
    {
        Unknown,
        Commander,      // Командир (отдает приказы)
        Executor,       // Исполнитель (подтверждает, докладывает)
        Observer,       // Наблюдатель (докладывает о ситуации)
        Coordinator,    // Координатор (согласует действия)
        Technician      // Технический специалист
    }
}
