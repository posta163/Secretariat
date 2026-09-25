using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Secretariat.Api.CurrentUser;
using Secretariat.Api.Data;
using Secretariat.Api.Dtos;
using Secretariat.Api.Models;

namespace Secretariat.Api.Controllers
{
    [ApiController]
    [Route("api/contracts")]
    public class ContractsController : ControllerBase
    {
        private readonly SecretariatDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ContractsController(
            SecretariatDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

  
        
        
        
        // POST /api/contracts
        [HttpPost]
        public async Task<IActionResult> Create(
            CreateContractRequest request)
        {
            // Ustalamy autora na podstawie aktualnego użytkownika.
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            // Nie pozwalamy na puste nazwy i tematy.
            if (string.IsNullOrWhiteSpace(request.Contractor) ||
                string.IsNullOrWhiteSpace(request.Subject))
            {
                return BadRequest(
                    "Kontrahent i przedmiot umowy są wymagane.");
            }

            // Sprawdzamy, czy przekazano datę umowy.
            if (request.ContractDate == default)
            {
                return BadRequest(
                    "Należy podać datę zawarcia umowy.");
            }

            // Sprawdzamy okres obowiązywania.
            if (request.ValidFrom.HasValue &&
                request.ValidTo.HasValue &&
                request.ValidFrom > request.ValidTo)
            {
                return BadRequest(
                    "Data rozpoczęcia nie może być późniejsza od daty zakończenia.");
            }

            // Sprawdzamy, czy osoba odpowiedzialna istnieje.
            var responsibleUserExists = await _context.AppUsers
                .AnyAsync(u => u.Id == request.ResponsibleUserId);

            if (!responsibleUserExists)
            {
                return BadRequest(
                    "Wskazana osoba odpowiedzialna nie istnieje.");
            }


            // Kierownik i dyrektor muszą być różnymi osobami.
            if (request.ManagerUserId == request.DirectorUserId)
            {
                return BadRequest(
                    "Kierownik i dyrektor muszą być różnymi użytkownikami.");
            }

            // Obie osoby muszą istnieć i posiadać rolę Approver.
            var validApproversCount = await _context.AppUsers
                .CountAsync(u =>
                    (u.Id == request.ManagerUserId ||
                     u.Id == request.DirectorUserId) &&
                    u.Role == UserRole.Approver);

            if (validApproversCount != 2)
            {
                return BadRequest(
                    "Kierownik i dyrektor muszą istnieć i posiadać rolę Approver.");
            }

            // Generujemy numer umowy.
            var now = DateTime.UtcNow;
            var year = now.Year;

            var count = await _context.Contracts
                .CountAsync(c => c.CreatedAt.Year == year);

            var contract = new Contract
            {
                Number = $"UM/{year}/{count + 1:D4}",
                Contractor = request.Contractor.Trim(),
                Subject = request.Subject.Trim(),
                ContractDate = request.ContractDate,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo,
                ContractualPenalties = request.ContractualPenalties,
                Comment = request.Comment,
                CreatedAt = now,
                CreatedByUserId = currentUser.Id,
                ResponsibleUserId = request.ResponsibleUserId,
                Status = ContractStatus.New,
                
                
                Approvers = new List<ContractApprover>
                    {
                        new ContractApprover
                            {
                                ApproverUserId = request.ManagerUserId,
                                Role = ContractApproverRole.Manager,
                                Status = ContractApprovalStatus.Pending
                                },

                        new ContractApprover
                            {
                                ApproverUserId = request.DirectorUserId,
                                Role = ContractApproverRole.Director,
                                Status = ContractApprovalStatus.Pending
                            }
}

            };

            _context.Contracts.Add(contract);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = contract.Id },
                new
                {
                    contract.Id,
                    contract.Number,
                    contract.Contractor,
                    contract.Subject,
                    contract.ContractDate,
                    contract.ValidFrom,
                    contract.ValidTo,
                    contract.CreatedAt,
                    contract.CreatedByUserId,
                    contract.ResponsibleUserId,
                    contract.Status
                });
        }

 
        
        
        
        
        
        // GET /api/contracts/{id}
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

            var contract = await _context.Contracts
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Number,
                    c.Contractor,
                    c.Subject,
                    c.ContractDate,
                    c.ValidFrom,
                    c.ValidTo,
                    c.ContractualPenalties,
                    c.Comment,
                    c.CreatedAt,
                    c.Status,
                    c.CreatedByUserId,
                    CreatedByName = c.CreatedByUser != null
                        ? c.CreatedByUser.DisplayName
                        : null,
                    c.ResponsibleUserId,
                    ResponsibleUserName = c.ResponsibleUser != null
                        ? c.ResponsibleUser.DisplayName
                        : null,
                    Approvers = c.Approvers
                        .OrderBy(a => a.Role)
                        .Select(a => new
                            {
                                a.ApproverUserId,

                                ApproverName = a.ApproverUser != null
                                ? a.ApproverUser.DisplayName
                                : null,

                                a.Role,
                                a.Status,
                                a.ReviewedAt
    })

    .ToList()
                })
                .FirstOrDefaultAsync();


            if (contract == null)
            {
                return NotFound("Umowa nie istnieje.");
            }

            // Sprawdzamy, czy użytkownik ma dostęp do umowy.
            var canRead =
                currentUser.Role == UserRole.Administrator ||
                currentUser.Role == UserRole.Secretariat ||
                contract.CreatedByUserId == currentUser.Id ||
                contract.ResponsibleUserId == currentUser.Id ||
                contract.Approvers.Any(a =>
                    a.ApproverUserId == currentUser.Id);

            if (!canRead)
            {
                return StatusCode(
                    403,
                    "Nie masz dostępu do tej umowy.");
            }

            return Ok(contract);
        }

     
        
        
        
        
        [HttpGet]
        public async Task<IActionResult> GetAll(
    [FromQuery] string? search)
        {
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            // Przygotowujemy zapytanie bez wykonywania go.
            var query = _context.Contracts
                .AsNoTracking()
                .AsQueryable();

            if (currentUser.Role != UserRole.Administrator &&
                currentUser.Role != UserRole.Secretariat)
            {
                query = query.Where(c =>
                    c.CreatedByUserId == currentUser.Id ||
                    c.ResponsibleUserId == currentUser.Id ||
                    c.Approvers.Any(a => a.ApproverUserId == currentUser.Id));
            }

            // Opcjonalne wyszukiwanie.
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();

                query = query.Where(c =>
                    c.Number.Contains(searchTerm) ||
                    c.Contractor.Contains(searchTerm) ||
                    c.Subject.Contains(searchTerm));
            }

            // Pobieramy i sortujemy wyniki.
            var contracts = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Number,
                    c.Contractor,
                    c.Subject,
                    c.ContractDate,
                    c.CreatedAt,
                    c.Status,
                    c.ResponsibleUserId,

                    ResponsibleUserName =
                        c.ResponsibleUser != null
                            ? c.ResponsibleUser.DisplayName
                            : null
                })
                .ToListAsync();

            return Ok(contracts);
        }





        // PUT /api/contracts/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            UpdateContractRequest request)
        {
            // Ustalamy aktualnego użytkownika.
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            // Pobieramy umowę.
            var contract = await _context.Contracts.FindAsync(id);

            if (contract == null)
            {
                return NotFound("Umowa nie istnieje.");
            }

            // Sprawdzamy uprawnienia do edycji.
            var canEdit =
                contract.CreatedByUserId == currentUser.Id ||
                currentUser.Role == UserRole.Secretariat ||
                currentUser.Role == UserRole.Administrator;

            if (!canEdit)
            {
                return StatusCode(
                    403,
                    "Nie masz uprawnień do edycji tej umowy.");
            }

            // Edytować można tylko nowy wniosek.
            if (contract.Status != ContractStatus.New)
            {
                return Conflict(
                    "Nie można edytować umowy, której proces akceptacji już się rozpoczął.");
            }

            // Sprawdzamy wymagane pola.
            if (string.IsNullOrWhiteSpace(request.Contractor) ||
                string.IsNullOrWhiteSpace(request.Subject))
            {
                return BadRequest(
                    "Kontrahent i przedmiot umowy są wymagane.");
            }

            if (request.ContractDate == default)
            {
                return BadRequest(
                    "Należy podać datę zawarcia umowy.");
            }

            // Sprawdzamy poprawność okresu obowiązywania.
            if (request.ValidFrom.HasValue &&
                request.ValidTo.HasValue &&
                request.ValidFrom > request.ValidTo)
            {
                return BadRequest(
                    "Data rozpoczęcia nie może być późniejsza od daty zakończenia.");
            }

            // Sprawdzamy osobę odpowiedzialną.
            var responsibleUserExists = await _context.AppUsers
                .AnyAsync(u => u.Id == request.ResponsibleUserId);

            if (!responsibleUserExists)
            {
                return BadRequest(
                    "Wskazana osoba odpowiedzialna nie istnieje.");
            }

            // Aktualizujemy wyłącznie dozwolone pola.
            contract.Contractor = request.Contractor.Trim();
            contract.Subject = request.Subject.Trim();
            contract.ContractDate = request.ContractDate;
            contract.ValidFrom = request.ValidFrom;
            contract.ValidTo = request.ValidTo;
            contract.ContractualPenalties = request.ContractualPenalties;
            contract.Comment = request.Comment;
            contract.ResponsibleUserId = request.ResponsibleUserId;

            // Zapisujemy zmiany.
            await _context.SaveChangesAsync();

            return NoContent();
        }











        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            // Ustalamy aktualnego użytkownika.
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            // Użytkownik musi mieć rolę Approver.
            if (currentUser.Role != UserRole.Approver)
            {
                return StatusCode(
                    403,
                    "Tylko użytkownik z rolą Approver może zatwierdzać umowy.");
            }

            // Pobieramy umowę wraz z jej akceptującymi.
            var contract = await _context.Contracts
                .Include(c => c.Approvers)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return NotFound("Umowa nie istnieje.");
            }

            // Sprawdzamy, czy użytkownik został przypisany.
            var approval = contract.Approvers
                .FirstOrDefault(a =>
                    a.ApproverUserId == currentUser.Id);

            if (approval == null)
            {
                return StatusCode(
                    403,
                    "Nie jesteś przypisany do akceptacji tej umowy.");
            }

            // Nie pozwalamy zmieniać zakończonego procesu.
            if (contract.Status == ContractStatus.Approved ||
                contract.Status == ContractStatus.Rejected)
            {
                return Conflict(
                    "Proces akceptacji tej umowy został już zakończony.");
            }

            // Każdy akceptujący może podjąć decyzję tylko raz.
            if (approval.Status != ContractApprovalStatus.Pending)
            {
                return Conflict(
                    "Ten użytkownik podjął już decyzję.");
            }

            // Zapisujemy akceptację i datę decyzji.
            approval.Status = ContractApprovalStatus.Approved;
            approval.ReviewedAt = DateTime.UtcNow;

            // Sprawdzamy, czy obie osoby zatwierdziły umowę.
            var everyoneApproved = contract.Approvers.Count == 2 &&
                contract.Approvers.All(a =>
                    a.Status == ContractApprovalStatus.Approved);

            contract.Status = everyoneApproved
                ? ContractStatus.Approved
                : ContractStatus.InProgress;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                contract.Id,
                contract.Number,
                ContractStatus = contract.Status,
                ApproverUserId = currentUser.Id,
                ApproverRole = approval.Role,
                ApprovalStatus = approval.Status,
                approval.ReviewedAt
            });
        }






        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            // Ustalenie aktualnego użytkownika.
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            // Tylko użytkownik z rolą Approver.
            if (currentUser.Role != UserRole.Approver)
            {
                return StatusCode(
                    403,
                    "Tylko użytkownik z rolą Approver może odrzucać umowy.");
            }

            // Pobranie umowy razem z przypisanymi akceptującymi.
            var contract = await _context.Contracts
                .Include(c => c.Approvers)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
            {
                return NotFound("Umowa nie istnieje.");
            }

            // Sprawdzenie przypisania użytkownika do tej umowy.
            var approval = contract.Approvers
                .FirstOrDefault(a =>
                    a.ApproverUserId == currentUser.Id);

            if (approval == null)
            {
                return StatusCode(
                    403,
                    "Nie jesteś przypisany do akceptacji tej umowy.");
            }

            // Zakończonego procesu nie można zmienić.
            if (contract.Status == ContractStatus.Approved ||
                contract.Status == ContractStatus.Rejected)
            {
                return Conflict(
                    "Proces akceptacji tej umowy został już zakończony.");
            }

            // Akceptujący nie może ponownie podejmować decyzji.
            if (approval.Status != ContractApprovalStatus.Pending)
            {
                return Conflict(
                    "Ten użytkownik podjął już decyzję.");
            }

            // Zapisujemy odrzucenie i datę decyzji.
            approval.Status = ContractApprovalStatus.Rejected;
            approval.ReviewedAt = DateTime.UtcNow;

            // Jedno odrzucenie kończy cały proces akceptacji.
            contract.Status = ContractStatus.Rejected;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                contract.Id,
                contract.Number,
                ContractStatus = contract.Status,
                ApproverUserId = currentUser.Id,
                ApproverRole = approval.Role,
                ApprovalStatus = approval.Status,
                approval.ReviewedAt
            });
        }
    }

}
