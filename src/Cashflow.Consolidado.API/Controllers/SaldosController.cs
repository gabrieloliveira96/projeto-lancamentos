using Cashflow.Consolidado.API.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cashflow.Consolidado.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SaldosController : ControllerBase
{
    private readonly ConsolidadoDbContext _context;

    public SaldosController(ConsolidadoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime data)
    {
        var saldo = await _context.SaldosConsolidados
            .FirstOrDefaultAsync(s => s.Data.Date == data.Date);

        if (saldo == null)
            return NotFound();

        return Ok(new { data = saldo.Data, saldo = saldo.Saldo });
    }
}
