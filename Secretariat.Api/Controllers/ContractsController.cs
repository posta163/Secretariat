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
                Status = ContractStatus.New
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
                        : null
                })
                .FirstOrDefaultAsync();

            if (contract == null)
            {
                return NotFound("Umowa nie istnieje.");
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
    }

}