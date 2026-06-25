using System.ComponentModel.DataAnnotations;

namespace GymTrack.DTOs.Student;

public sealed class CreateStudentRequest
{
    [Required]
    [MaxLength(100)]
    public string Ime { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Prezime { get; init; } = string.Empty;
}
