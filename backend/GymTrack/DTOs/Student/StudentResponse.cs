namespace GymTrack.DTOs.Student;

public sealed class StudentResponse
{
    public int Id { get; init; }

    public string Ime { get; init; } = string.Empty;

    public string Prezime { get; init; } = string.Empty;
}
