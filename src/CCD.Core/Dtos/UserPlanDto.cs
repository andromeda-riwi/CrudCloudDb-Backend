namespace CCD.Core.Dtos;

/// <summary>
/// DTO con información del plan actual del usuario
/// </summary>
public class UserPlanDto
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int DatabaseLimitPerEngine { get; set; }
    public decimal MonthlyPrice { get; set; }
    public Dictionary<string, int> DatabaseCountByEngine { get; set; } = new();
    public int TotalDatabases { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public bool CanUpgrade { get; set; }
}

