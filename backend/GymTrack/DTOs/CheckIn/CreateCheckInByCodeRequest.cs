using System.ComponentModel.DataAnnotations;

namespace GymTrack.DTOs.CheckIn;

public sealed class CreateCheckInByCodeRequest
{
    [MaxLength(1000)]
    public string? Note { get; init; }
}
