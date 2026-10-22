using System.ComponentModel.DataAnnotations;

using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.Api.Groups;

public sealed record VoteRequest([Required] VoteAnswer? Answer);
