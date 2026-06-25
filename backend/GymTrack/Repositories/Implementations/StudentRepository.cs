using GymTrack.Data;
using GymTrack.Entities;
using GymTrack.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymTrack.Repositories.Implementations;

public sealed class StudentRepository : IStudentRepository
{
    private readonly AppDbContext _dbContext;

    public StudentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Student?> GetByIdAsync(int studentId, bool asNoTracking, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Students.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.SingleOrDefaultAsync(student => student.Id == studentId, cancellationToken);
    }

    public void Add(Student student) =>
        _dbContext.Students.Add(student);
}
