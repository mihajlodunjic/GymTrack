using GymTrack.Application.Students;
using GymTrack.DTOs.Student;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GymTrack.Tests.Services;

public sealed class StudentRequestsTests
{
    [Fact]
    public async Task CreateStudentCommand_CreatesStudent()
    {
        await using var dbContext = TestDbContextFactory.Create();
        using var provider = TestServiceProviderFactory.Create(dbContext);
        var mediator = provider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new CreateStudentCommand(new CreateStudentRequest
        {
            Ime = "Marko",
            Prezime = "Markovic"
        }));

        var student = await dbContext.Students.FindAsync(response.Id);

        Assert.NotNull(student);
        Assert.Equal("Marko", response.Ime);
        Assert.Equal("Markovic", response.Prezime);
        Assert.Equal("Marko", student!.Ime);
        Assert.Equal("Markovic", student.Prezime);
    }
}
