using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadioIntercepts.Core.Models.Communication
{
    public class FlowLink
    {
        public string SourceId { get; set; } = null!;
        public string TargetId { get; set; } = null!;
        public double Value { get; set; } // Вес связи (количество сообщений)
        public string Color { get; set; } = "#666666";
        public double Strength { get; set; } = 1.0; // Сила связи (нормированная)
        public List<DateTime> InteractionTimes { get; set; } = new();
        public TimeSpan AverageInterval { get; set; }
        public double Consistency { get; set; } // Консистентность взаимодействий
    }
}
