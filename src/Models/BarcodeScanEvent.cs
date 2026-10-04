using System;

namespace SVLL_IT_Workstation;

public class BarcodeScanEvent
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string BarcodeValue { get; set; } = "";
    public int Length => BarcodeValue.Length;
    public string TimeFormatted => Timestamp.ToString("HH:mm:ss.fff");
}
