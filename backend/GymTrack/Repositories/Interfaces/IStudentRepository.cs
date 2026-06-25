using GymTrack.Entities;

namespace GymTrack.Repositories.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int studentId, bool asNoTracking, CancellationToken cancellationToken = default);

    void Add(Student student);
}
