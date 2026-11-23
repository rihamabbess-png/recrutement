using System.Globalization;

namespace Recrutement.Controllers;

[Authorize(Roles = "Admin,RH")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var apps = db.JobApplications.AsNoTracking();

        var byStatus = await apps
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var statusMap = Enum.GetValues<ApplicationStatus>()
            .ToDictionary(s => s, s => byStatus.FirstOrDefault(x => x.Status == s)?.Count ?? 0);

        var offers = await db.JobOffers
            .AsNoTracking()
            .Select(o => new
            {
                o.Title,
                Total = o.Applications.Count(),
                Retained = o.Applications.Count(a => a.Status == ApplicationStatus.Retenue),
                Rejected = o.Applications.Count(a => a.Status == ApplicationStatus.Refusee)
            })
            .OrderByDescending(o => o.Total)
            .ToListAsync();

        // Données légères, chargées une fois pour les graphiques par mois, score et budget
        var rows = await apps
            .Select(a => new { a.SubmittedAt, a.AiScore, a.AiBudgetFit })
            .ToListAsync();

        // Six derniers mois, avec les mois vides à zéro
        var since = DateTime.UtcNow.AddMonths(-5);
        var sinceMonth = new DateTime(since.Year, since.Month, 1);
        var culture = new CultureInfo("fr-CA");

        var byMonth = Enumerable.Range(0, 6)
            .Select(i => sinceMonth.AddMonths(i))
            .Select(m => new MonthStat(
                m.ToString("MMM yyyy", culture),
                rows.Count(r => r.SubmittedAt.Year == m.Year && r.SubmittedAt.Month == m.Month)))
            .ToList();

        // Distribution des scores IA par tranche de 20 points
        var scores = rows.Where(r => r.AiScore is not null).Select(r => r.AiScore!.Value).ToList();
        var bands = new (string Label, int Min, int Max)[]
        {
            ("0-19", 0, 19), ("20-39", 20, 39), ("40-59", 40, 59), ("60-79", 60, 79), ("80-100", 80, 100)
        };
        var scoreBuckets = bands
            .Select(b => new ScoreBucket(b.Label, scores.Count(s => s >= b.Min && s <= b.Max)))
            .ToList();

        var model = new DashboardViewModel
        {
            TotalApplications = statusMap.Values.Sum(),
            OpenOffers = await db.JobOffers.CountAsync(o => o.IsOpen),
            Retained = statusMap[ApplicationStatus.Retenue],
            Rejected = statusMap[ApplicationStatus.Refusee],
            InInterview = statusMap[ApplicationStatus.Entrevue],
            PendingAnalysis = statusMap[ApplicationStatus.EnAnalyse],
            ByStatus = statusMap,
            ByOffer = offers.Select(o => new OfferStat(o.Title, o.Total, o.Retained, o.Rejected)).ToList(),
            Recent = await apps.Include(a => a.JobOffer)
                .OrderByDescending(a => a.SubmittedAt)
                .Take(6)
                .ToListAsync(),
            ByMonth = byMonth,
            ScoreBuckets = scoreBuckets,
            BudgetFitYes = rows.Count(r => r.AiBudgetFit == true),
            BudgetFitNo = rows.Count(r => r.AiBudgetFit == false),
            BudgetUnknown = rows.Count(r => r.AiBudgetFit == null),
            AverageScore = scores.Count == 0 ? null : Math.Round(scores.Average(), 1)
        };

        return View(model);
    }
}