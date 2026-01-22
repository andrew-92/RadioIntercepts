using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.CodeAnalysis
{
    public enum CodeTermCategory
    {
        Unknown,
        Military,           // Военные термины
        Slang,              // Сленг
        CodeWord,           // Кодовые слова
        Abbreviation,       // Аббревиатуры
        Technical,          // Технические термины
        Location,           // Обозначения мест
        Equipment,          // Оборудование и техника
        Operation,          // Оперативные термины
        CallSignSpecific,   // Специфичные для позывных
        Urgency             // Термины срочности
    }
}
