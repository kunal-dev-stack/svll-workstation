using System;

namespace SVLL_IT_Workstation;

public class EventLogEntryItem
{
    public DateTime TimeGenerated { get; set; }
    public string Level { get; set; } = "Error";
    public int EventId { get; set; }
    public string Source { get; set; } = "";
    public string Message { get; set; } = "";
    public string Category { get; set; } = "System";

    public string TimeFormatted => TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss");
}
