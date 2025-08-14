namespace Cauldron.Server.Dtos;

public sealed record LoginResponse(string AccessToken, string TokenType, int ExpiresIn);
public sealed record WhoAmIResponse(string? Sub, string? Email);