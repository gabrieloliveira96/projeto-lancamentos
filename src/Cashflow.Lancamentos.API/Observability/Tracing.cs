using System.Diagnostics;

namespace Cashflow.Lancamentos.API.Observability;

public static class Tracing
{
    public static readonly ActivitySource Source = new("Cashflow.Lancamentos");

}