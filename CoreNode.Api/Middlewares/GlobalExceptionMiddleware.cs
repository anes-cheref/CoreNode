using System.Net;
using System.Text.Json;
using CoreNode.Domain.Exceptions; // N'oublie pas l'using vers ton projet Domain

namespace CoreNode.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (QuotaExceededException ex)
        {
            // 🛑 Règle métier enfreinte : On loggue en Warning (pas une erreur critique du serveur)
            _logger.LogWarning("Action refusée (Quota) : {Message}", ex.Message);

            // On renvoie un code 400 (Mauvaise requête du client)
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            // On affiche le message métier à l'utilisateur
            var mess = new { Message = ex.Message };
            var messSerialized = JsonSerializer.Serialize(mess);
            await context.Response.WriteAsync(messSerialized);
        }
        catch (Exception ex)
        {
            // 💥 Erreur technique inattendue (Base de données crashée, NullReference, etc.)
            _logger.LogError(ex, "Une erreur inattendue s'est produite lors du traitement de la requête.");

            // On renvoie un code 500 (Erreur interne du serveur)
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var mess = new { Message = "Une erreur interne est survenue sur le serveur." };
            var messSerialized = JsonSerializer.Serialize(mess);
            await context.Response.WriteAsync(messSerialized);
        }
    }
}