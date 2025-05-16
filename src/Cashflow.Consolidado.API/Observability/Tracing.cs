using System.Diagnostics;

namespace Cashflow.Consolidado.API.Observability;

public static class Tracing
{
    public static readonly ActivitySource Source = new("Cashflow.Consolidado");
}