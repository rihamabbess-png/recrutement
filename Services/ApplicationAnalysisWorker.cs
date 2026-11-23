using Microsoft.EntityFrameworkCore;
using Recrutement.Data;
using Recrutement.Models;
using UglyToad.PdfPig;

namespace Recrutement.Services;

public class ApplicationAnalysisWorker(
    IServiceScopeFactory scopes,
    IWebHostEnvironment env,
    IConfiguration config,
    ILogger<ApplicationAnalysisWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Erreur dans le traitement des candidatures.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task ProcessPendingAsync(CancellationToken ct)
    {
        // Sans clé Mistral, on ne fait rien : les candidatures restent en attente
        if (string.IsNullOrEmpty(config["Mistral:ApiKey"])) return;

        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mistral = scope.ServiceProvider.GetRequiredService<MistralService>();

        var pending = await db.JobApplications
            .Include(a => a.JobOffer)
            .Where(a => a.AnalyzedAt == null && a.AiError == null)
            .OrderBy(a => a.SubmittedAt)
            .Take(5)
            .ToListAsync(ct);

        foreach (var app in pending)
        {
            try
            {
                var cvPath = Path.Combine(env.ContentRootPath, app.CvFilePath);
                var cvText = ExtractText(cvPath);

                var result = await mistral.EvaluateAsync(app.JobOffer!, app, cvText, ct);

                app.AiScore = result.Score;
                app.AiBudgetFit = result.BudgetFit;
                app.AiCriteriaMet = string.Join("\n", result.CriteriaMet);
                app.AiCriteriaMissing = string.Join("\n", result.CriteriaMissing);
                app.AiSummary = result.Summary;
                app.AnalyzedAt = DateTime.UtcNow;
                app.Status = ApplicationStatus.Analysee;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // L'erreur reste visible dans le dossier. Le RH peut relancer l'analyse.
                app.AiError = ex.Message;
                logger.LogWarning(ex, "Analyse échouée pour la candidature {Id}.", app.Id);
            }

            await db.SaveChangesAsync(ct);
        }
    }

    public static string ExtractText(string path)
    {
        using var pdf = PdfDocument.Open(path);
        return string.Join("\n", pdf.GetPages().Select(p => p.Text));
    }
}