namespace Cauldron.Cli.Models; 

public sealed record FoodItem(
  Guid Id,
  string Name,
  DateOnly? Expires,
  DateTime CreatedAt
);

    
