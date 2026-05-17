using System.ComponentModel.DataAnnotations;

namespace BudgetApp.Api.Configuration;

public class MongoDbSettings
{
    [Required(ErrorMessage = "MongoDB ConnectionString is required")]
    [MinLength(10, ErrorMessage = "MongoDB ConnectionString must be at least 10 characters")]
    public string ConnectionString { get; set; } = string.Empty;

    [Required(ErrorMessage = "MongoDB DatabaseName is required")]
    [MinLength(1, ErrorMessage = "MongoDB DatabaseName is required")]
    public string DatabaseName { get; set; } = string.Empty;
}
