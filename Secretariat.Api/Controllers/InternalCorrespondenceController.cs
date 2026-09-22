using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Secretariat.Api.CurrentUser;
using Secretariat.Api.Data;
using Secretariat.Api.Models;
using Secretariat.Api.Dtos;

namespace Secretariat.Api.Controllers
{
    [ApiController]
    [Route("api/internal-correspondence")]
    public class InternalCorrespondenceController : ControllerBase
    {
        private readonly SecretariatDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public InternalCorrespondenceController(
            SecretariatDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        public async Task<ActionResult> Create(
        CreateInternalCorrespondenceRequest request)
        {
            // Ustalenie aktualnego użytkownika
            var currentUser = await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized("Nie udało się ustalić użytkownika.");
            }

            // Wymagamy przynajmniej jednego akceptującego
            if (request.ApproverUserIds == null ||
                request.ApproverUserIds.Count == 0)
            {
                return BadRequest(
                    "Należy wskazać przynajmniej jednego akceptującego.");
            }

            // Ten sam użytkownik nie może zostać wskazany dwukrotnie
            var approverIds = request.ApproverUserIds.Distinct().ToList();

            if (approverIds.Count != request.ApproverUserIds.Count)
            {
                return BadRequest(
                    "Lista akceptujących zawiera powtarzających się użytkowników.");
            }

            // Wszyscy wskazani użytkownicy muszą mieć rolę Approver
            var validApproversCount = await _context.AppUsers
                .CountAsync(u =>
                    approverIds.Contains(u.Id) &&
                    u.Role == UserRole.Approver);

            if (validApproversCount != approverIds.Count)
            {
                return BadRequest(
                    "Wskazani użytkownicy muszą istnieć i posiadać rolę Approver.");
            }

            // Numer i autor są nadawani przez backend
            var year = DateTime.UtcNow.Year;

            var count = await _context.InternalCorrespondences
                .CountAsync(c => c.CreatedAt.Year == year);

            var document = new InternalCorrespondence
            {
                Number = $"KWN/{year}/{count + 1:D4}",
                Subject = request.Subject,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = currentUser.Id,
                Status = InternalCorrespondenceStatus.New,

                Approvers = approverIds.Select(id =>
                    new InternalCorrespondenceApprover
                    {
                        ApproverUserId = id,
                        Status = InternalApprovalStatus.Pending
                    }).ToList()
            };

            _context.InternalCorrespondences.Add(document);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = document.Id },
                new
                {
                    document.Id,
                    document.Number,
                    document.Subject,
                    document.Description,
                    document.CreatedAt,
                    document.CreatedByUserId,
                    document.Status,
                    ApproverUserIds = approverIds
                });
        }



        [HttpGet]
        public async Task<ActionResult<IEnumerable<InternalCorrespondence>>> GetAll()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized("Nie udało się ustalić użytkownika.");
            }

            var internalCorrespondences = await _context.InternalCorrespondences
                .Include(c => c.CreatedByUser)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(internalCorrespondences);
        }

        
        
        
        
        
        
        
        
        
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            var document = await _context.InternalCorrespondences
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Number,
                    c.Subject,
                    c.Description,
                    c.CreatedAt,
                    c.Status,
                    c.CreatedByUserId,

                    Approvers = c.Approvers.Select(a => new
                    {
                        a.ApproverUserId,
                        a.Status,
                        a.ReviewedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (document == null)
            {
                return NotFound(
                    "Korespondencja wewnętrzna nie istnieje.");
            }

            return Ok(document);
        }

       
        
        
        
        
        
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            // Ustalamy aktualnego użytkownika
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            // Tylko użytkownik z rolą Approver
            // może wykonywać operację akceptacji
            if (currentUser.Role != UserRole.Approver)
            {
                return StatusCode(
                    403,
                    "Tylko użytkownik z rolą Approver może zatwierdzać dokumenty.");
            }

            // Pobieramy dokument wraz z akceptującymi
            var document = await _context.InternalCorrespondences
                .Include(c => c.Approvers)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (document == null)
            {
                return NotFound(
                    "Korespondencja wewnętrzna nie istnieje.");
            }

            // Sprawdzamy, czy użytkownik został
            // przypisany do akceptacji tego dokumentu
            var approval = document.Approvers
                .FirstOrDefault(a =>
                    a.ApproverUserId == currentUser.Id);

            if (approval == null)
            {
                return StatusCode(
                    403,
                    "Nie jesteś przypisany do akceptacji tego dokumentu.");
            }

            // Nie zmieniamy zakończonych dokumentów
            if (document.Status == InternalCorrespondenceStatus.Approved ||
                document.Status == InternalCorrespondenceStatus.Rejected)
            {
                return Conflict(
                    "Proces akceptacji tego dokumentu został już zakończony.");
            }

            // Nie pozwalamy ponownie podjąć decyzji
            if (approval.Status != InternalApprovalStatus.Pending)
            {
                return Conflict(
                    "Ten użytkownik podjął już decyzję.");
            }

            // Zapisujemy decyzję użytkownika
            approval.Status = InternalApprovalStatus.Approved;
            approval.ReviewedAt = DateTime.UtcNow;

            // Aktualizujemy status całego dokumentu
            var everyoneApproved = document.Approvers
                .All(a => a.Status == InternalApprovalStatus.Approved);

            document.Status = everyoneApproved
                ? InternalCorrespondenceStatus.Approved
                : InternalCorrespondenceStatus.InProgress;

            // Zapisujemy zmiany w bazie
            await _context.SaveChangesAsync();

            return Ok(new
            {
                document.Id,
                document.Number,
                DocumentStatus = document.Status,
                ApproverUserId = currentUser.Id,
                ApprovalStatus = approval.Status,
                approval.ReviewedAt
            });
        }
       
        
        
        
        
        
        
        
        [HttpPost("{id:int}/reject")]
       
        public async Task<IActionResult> Reject(int id)
        {
            // Ustalamy aktualnego użytkownika
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            // Tylko użytkownicy z rolą Approver
            if (currentUser.Role != UserRole.Approver)
            {
                return StatusCode(
                    403,
                    "Tylko użytkownik z rolą Approver może odrzucać dokumenty.");
            }

            // Pobieramy dokument wraz z przypisanymi akceptującymi
            var document = await _context.InternalCorrespondences
                .Include(c => c.Approvers)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (document == null)
            {
                return NotFound(
                    "Korespondencja wewnętrzna nie istnieje.");
            }

            // Użytkownik musi być przypisany do tego dokumentu
            var approval = document.Approvers
                .FirstOrDefault(a =>
                    a.ApproverUserId == currentUser.Id);

            if (approval == null)
            {
                return StatusCode(
                    403,
                    "Nie jesteś przypisany do akceptacji tego dokumentu.");
            }

            // Zakończonego procesu nie można zmieniać
            if (document.Status == InternalCorrespondenceStatus.Approved ||
                document.Status == InternalCorrespondenceStatus.Rejected)
            {
                return Conflict(
                    "Proces akceptacji tego dokumentu został już zakończony.");
            }

            // Nie można ponownie podjąć decyzji
            if (approval.Status != InternalApprovalStatus.Pending)
            {
                return Conflict(
                    "Ten użytkownik podjął już decyzję.");
            }

            // Zapisujemy odrzucenie
            approval.Status = InternalApprovalStatus.Rejected;
            approval.ReviewedAt = DateTime.UtcNow;

            // Jedno odrzucenie kończy cały proces
            document.Status = InternalCorrespondenceStatus.Rejected;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                document.Id,
                document.Number,
                DocumentStatus = document.Status,
                ApproverUserId = currentUser.Id,
                ApprovalStatus = approval.Status,
                approval.ReviewedAt
            });
        }
    }
}