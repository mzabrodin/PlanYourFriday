using System.ComponentModel.DataAnnotations;

namespace PlanYourFriday.Api.Groups;

public sealed record InviteRequest(
    [Required]
    [Length(1, 50)]
    IReadOnlyList<string> Emails) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var email = new EmailAddressAttribute();
        foreach (var address in Emails ?? [])
        {
            if (string.IsNullOrWhiteSpace(address) || !email.IsValid(address.Trim()))
            {
                yield return new ValidationResult($"'{address}' is not a valid email address.", [nameof(Emails)]);
            }
        }
    }
}
