namespace Recrutement.Controllers;

[Authorize(Roles = "Admin,RH")]
public class JobOffersController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index()
        => View(await db.JobOffers
            .Include(j => j.Applications)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var offer = await db.JobOffers
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Id == id);
        if (offer is null) return NotFound();

        ViewBag.PublicLink = Url.Action("Apply", "Public", new { token = offer.PublicToken }, Request.Scheme);
        return View(offer);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,Requirements,BudgetMin,BudgetMax,ClosesAt")] JobOffer offer)
    {
        if (offer.BudgetMax < offer.BudgetMin)
            ModelState.AddModelError(nameof(offer.BudgetMax), "Le budget maximum doit être égal ou supérieur au minimum.");

        if (!ModelState.IsValid) return View(offer);

        offer.CreatedById = users.GetUserId(User)!;
        db.JobOffers.Add(offer);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = offer.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var offer = await db.JobOffers.FindAsync(id);
        if (offer is null) return NotFound();

        offer.IsOpen = false;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
}