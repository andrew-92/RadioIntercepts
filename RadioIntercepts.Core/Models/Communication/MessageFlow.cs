using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Communication
{
    public class MessageFlow
    {
        public long MessageId { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> Path { get; set; } = new(); // Путь сообщения через узлы
        public double Duration { get; set; } // Длительность обработки (в секундах)
        public string MessageType { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool IsComplete { get; set; } // Полный ли поток (есть ответы)
        public List<string> Callsigns { get; set; } = new();
    }
}
