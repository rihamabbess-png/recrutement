using Microsoft.AspNetCore.Hosting;

namespace Recrutement.Controllers;

public class PublicController(ApplicationDbContext db, IWebHostEnvironment env) : Controller
{
    private const long MaxCvBytes = 5 * 1024 * 1024;

    [HttpGet("postuler/{token:guid}")]
    public async Task<IActionResult> Apply(Guid token)
    {
        var offer = await FindOpenOfferAsync(token);
        if (offer is null) return NotFound();

        ViewBag.Offer = offer;
        return View(new ApplyViewModel());
    }

    [HttpPost("postuler/{token:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(Guid token, ApplyViewModel model)
    {
        var offer = await FindOpenOfferAsync(token);
        if (offer is null) return NotFound();

        if (!model.Consent)
            ModelState.AddModelError(nameof(model.Consent), "Vous devez accepter l'analyse de votre candidature.");

        if (model.Cv is null || model.Cv.Length == 0)
            ModelState.AddModelError(nameof(model.Cv), "Le CV est requis.");
        else if (Path.GetExtension(model.Cv.FileName).ToLowerInvariant() != ".pdf" || model.Cv.ContentType != "application/pdf")
            ModelState.AddModelError(nameof(model.Cv), "Le CV doit être un fichier PDF.");
        else if (model.Cv.Length > MaxCvBytes)
            ModelState.AddModelError(nameof(model.Cv), "Le CV ne doit pas dépasser 5 Mo.");

        if (!ModelState.IsValid)
        {
            ViewBag.Offer = offer;
            return View(model);
        }

        if (await db.JobApplications.AnyAsync(a => a.JobOfferId == offer.Id && a.Email == model.Email))
        {
            ModelState.AddModelError(string.Empty, "Une candidature avec ce courriel existe déjà pour cette offre.");
            ViewBag.Offer = offer;
            return View(model);
        }

        var folder = Path.Combine(env.ContentRootPath, "App_Data", "cvs");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid()}.pdf";
        var fullPath = Path.Combine(folder, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await model.Cv!.CopyToAsync(stream);
        }

        db.JobApplications.Add(new JobApplication
        {
            JobOfferId = offer.Id,
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            CurrentTitle = model.CurrentTitle,
            ExpectedSalary = model.ExpectedSalary,
            CvFilePath = Path.Combine("App_Data", "cvs", fileName),
            Status = ApplicationStatus.EnAnalyse
        });
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Thanks));
    }

    [HttpGet("postuler/merci")]
    public IActionResult Thanks() => View();

    private Task<JobOffer?> FindOpenOfferAsync(Guid token)
        => db.JobOffers.FirstOrDefaultAsync(j => j.PublicToken == token && j.IsOpen);
}