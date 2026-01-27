using CsvHelper.Configuration.Attributes;

namespace schedule_slot.Models
{
    public class TeleERCsvRecord
    {
        [Name("Sched_ID")] public string Sched_ID { get; set; } = string.Empty;
        [Name("Pract_ID")] public string Pract_ID { get; set; } = string.Empty;
        [Name("Dept_ID")] public string Dept_ID { get; set; } = string.Empty;
        [Name("Slot_Date")] public string Slot_Date { get; set; } = string.Empty;
        [Name("Time_Start")] public string Time_Start { get; set; } = string.Empty;
        [Name("Time_End")] public string Time_End { get; set; } = string.Empty;
        [Name("Channel")] public string Channel { get; set; } = string.Empty;
        [Name("Capacity")] public int Capacity { get; set; }
    }
}