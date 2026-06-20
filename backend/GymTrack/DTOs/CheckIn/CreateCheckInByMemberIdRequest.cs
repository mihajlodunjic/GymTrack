using System.ComponentModel.DataAnnotations;

namespace GymTrack.DTOs.CheckIn;

public sealed class CreateCheckInByMemberIdRequest
{
    [MaxLength(1000)]
    public string? Note { get; init; }
}
