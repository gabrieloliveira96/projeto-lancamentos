using Cashflow.Lancamentos.API.Application.Commands.Lancamentos.CreateLancamento;
using FluentValidation;

namespace Cashflow.Lancamentos.API.Application.Validators;

public class CreateLancamentoCommandValidator : AbstractValidator<CreateLancamentoCommand>
{
    public CreateLancamentoCommandValidator()
    {
        RuleFor(x => x.Data)
            .NotEmpty().WithMessage("Data é obrigatória.");

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero.");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("Tipo de lançamento inválido. Use 'Credito' ou 'Debito'.");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória.");
    }
}
