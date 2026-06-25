using GymTrack.DTOs.Student;
using GymTrack.Entities;
using GymTrack.Repositories.Interfaces;
using MediatR;

namespace GymTrack.Application.Students;

public sealed record CreateStudentCommand(CreateStudentRequest Request) : IRequest<StudentResponse>;

public sealed class CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, StudentResponse>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStudentCommandHandler(IStudentRepository studentRepository, IUnitOfWork unitOfWork)
    {
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<StudentResponse> Handle(CreateStudentCommand command, CancellationToken cancellationToken)
    {
        var student = new Student
        {
            Ime = command.Request.Ime.Trim(),
            Prezime = command.Request.Prezime.Trim()
        };

        _studentRepository.Add(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return StudentRequestMappings.MapResponse(student);
    }
}

file static class StudentRequestMappings
{
    public static StudentResponse MapResponse(Student student) =>
        new()
        {
            Id = student.Id,
            Ime = student.Ime,
            Prezime = student.Prezime
        };
}
