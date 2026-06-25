using System.ComponentModel.DataAnnotations;

namespace GymTrack.Entities;

public sealed class Student
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Ime { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Prezime { get; set; } = string.Empty;
}
