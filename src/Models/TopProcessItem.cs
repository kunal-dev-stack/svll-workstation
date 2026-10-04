namespace SVLL_IT_Workstation;

public class TopProcessItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double MemoryMb { get; set; }
    public string MemoryFormatted => $"{MemoryMb:F1} MB";
    public double MemoryPercent { get; set; }
}
