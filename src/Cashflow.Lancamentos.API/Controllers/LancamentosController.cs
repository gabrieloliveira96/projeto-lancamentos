using Cashflow.Lancamentos.API.Application.Commands.Lancamentos.CreateLancamento;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cashflow.Lancamentos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LancamentosController : ControllerBase
{
    private readonly IMediator _mediator;

    public LancamentosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateLancamentoCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(Post), new { id }, command);
    }
}
