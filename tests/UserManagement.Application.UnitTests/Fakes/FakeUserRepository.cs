using UserManagement.Domain.Entities;
using UserManagement.Domain.Factories;
using UserManagement.Domain.Repositories;
using UserManagement.Domain.ValueObjects;

namespace UserManagement.Application.UnitTests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Email.ToString() == email.ToString() && !u.IsDeleted));

    public Task<User?> GetByCognitoSubAsync(string cognitoSub, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.CognitoSub == cognitoSub));

    public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<User>>(_users.ToList());

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Any(u => u.Email.ToString() == email.ToString() && !u.IsDeleted));

    public void SeedExistingEmail(Email email)
    {
        var user = UserFactory.CreatePending(email, "Existing");
        _users.Add(user);
    }
}
