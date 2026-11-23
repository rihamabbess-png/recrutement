using Microsoft.AspNetCore.Hosting;

namespace Recrutement.Controllers;

[Authorize(Roles = "Admin,RH")]
public class ApplicationsController(
    ApplicationDbContext db,
    UserManager<ApplicationUser> users,
    IWebHostEnvironment env,
    MistralService mistral) : Controller
{
    // Tableau de bord : meilleurs scores IA en premier
    public async Task<IActionResult> Index(ApplicationStatus? status)
    {
        var query = db.JobApplications.Include(a => a.JobOffer).AsQueryable();
        if (status is not null) query = query.Where(a => a.Status == status);

        ViewBag.Status = status;
        return View(await query
            .OrderByDescending(a => a.AiScore)
            .ThenByDescending(a => a.SubmittedAt)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var app = await db.JobApplications
            .Include(a => a.JobOffer)
            .Include(a => a.ReviewedBy)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (app is null) return NotFound();
        return View(app);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, ApplicationStatus status, string? hrNotes)
    {
        var app = await db.JobApplications.FindAsync(id);
        if (app is null) return NotFound();

        app.Status = status;
        app.HrNotes = hrNotes;
        app.ReviewedById = users.GetUserId(User);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    // Relance l'analyse IA après une erreur (par exemple une clé API mal configurée)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Retry(int id)
    {
        var app = await db.JobApplications.FindAsync(id);
        if (app is null) return NotFound();

        app.AiError = null;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    // Lance l'analyse IA à la demande du RH
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(int id)
    {
        var app = await db.JobApplications
            .Include(a => a.JobOffer)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (app is null) return NotFound();

        try
        {
            var path = Path.Combine(env.ContentRootPath, app.CvFilePath);
            var cvText = ApplicationAnalysisWorker.ExtractText(path);

            var result = await mistral.EvaluateAsync(app.JobOffer!, app, cvText, HttpContext.RequestAborted);

            app.AiScore = result.Score;
            app.AiBudgetFit = result.BudgetFit;
            app.AiCriteriaMet = string.Join("\n", result.CriteriaMet);
            app.AiCriteriaMissing = string.Join("\n", result.CriteriaMissing);
            app.AiSummary = result.Summary;
            app.AnalyzedAt = DateTime.UtcNow;
            app.AiError = null;
            app.Status = ApplicationStatus.Analysee;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            app.AiError = ex.Message;
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Cv(int id)
    {
        var app = await db.JobApplications.FindAsync(id);
        if (app is null) return NotFound();

        var path = Path.Combine(env.ContentRootPath, app.CvFilePath);
        return PhysicalFile(path, "application/pdf");
    }
}