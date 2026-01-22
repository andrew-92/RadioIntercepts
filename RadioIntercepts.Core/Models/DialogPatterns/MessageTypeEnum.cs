using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.DialogPatterns
{
    public enum MessageType
    {
        Unknown,
        Command,        // Приказ/команда
        Request,        // Запрос
        Report,         // Отчет/доклад
        Confirmation,   // Подтверждение
        Query,          // Вопрос
        Coordination,   // Координация
        Technical,      // Техническое сообщение
        Greeting,       // Приветствие/обращение
        Farewell        // Прощание
    }
}
