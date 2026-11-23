namespace Recrutement.ViewModels;

public class DashboardViewModel
{
    public int TotalApplications { get; set; }
    public int OpenOffers { get; set; }
    public int Retained { get; set; }
    public int Rejected { get; set; }
    public int InInterview { get; set; }
    public int PendingAnalysis { get; set; }

    // Taux en pourcentage
    public double SelectionRate => TotalApplications == 0 ? 0 : Math.Round(100.0 * Retained / TotalApplications, 1);
    public double RejectionRate => TotalApplications == 0 ? 0 : Math.Round(100.0 * Rejected / TotalApplications, 1);

    public Dictionary<ApplicationStatus, int> ByStatus { get; set; } = new();
    public List<OfferStat> ByOffer { get; set; } = new();
    public List<JobApplication> Recent { get; set; } = new();
    public List<MonthStat> ByMonth { get; set; } = new();
    public List<ScoreBucket> ScoreBuckets { get; set; } = new();

    // Budget : résultat de l'analyse IA
    public int BudgetFitYes { get; set; }
    public int BudgetFitNo { get; set; }
    public int BudgetUnknown { get; set; }

    public double? AverageScore { get; set; }
}

public record OfferStat(string Title, int Applications, int Retained, int Rejected);
public record MonthStat(string Label, int Count);
public record ScoreBucket(string Label, int Count);