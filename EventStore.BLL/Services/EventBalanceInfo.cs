namespace EventStore.BLL.Services;

public class EventBalanceInfo
{
    public decimal TotalBudget { get; set; }
    public decimal PlannedCost { get; set; }
    public decimal ActuallyPaid { get; set; }
    public decimal RemainingToPay { get; set; }
    public decimal LeftoverBudget { get; set; }
}