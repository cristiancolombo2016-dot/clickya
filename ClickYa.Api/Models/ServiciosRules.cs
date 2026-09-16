namespace ClickYa.Api.Models;

public static class PremiumMembership
{
    public const int DuracionDias = 30;

    public static bool IsVigente(Tecnico tecnico, DateTime utcNow) =>
        tecnico.Activo && tecnico.EsPremium && tecnico.FechaPremium.HasValue &&
        tecnico.FechaPremium.Value <= utcNow &&
        tecnico.FechaPremium.Value > utcNow.AddDays(-DuracionDias);
}

public static class UrgenciaEstados
{
    public const string Publicada = "Publicada";
    public const string Seleccionada = "Seleccionada";
}
