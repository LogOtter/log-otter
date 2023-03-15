using System.ComponentModel.DataAnnotations;

namespace CustomerApi.Controllers.Customers.Create;

public record CreateCustomerRequest(
    [Required]
    [EmailAddress]
    string EmailAddress,
    [Required]
    string FirstName,
    [Required]
    string LastName);
