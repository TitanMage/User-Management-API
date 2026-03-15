using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagementAPI.Models;

namespace UserManagementAPI.Repositories
{
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<Guid, User> _store = new();

        public InMemoryUserRepository()
        {
            // seed with sample user
            var u = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                DateOfBirth = new DateTime(1990, 1, 1)
            };
            _store[u.Id] = u;
        }

        public Task<IEnumerable<User>> GetAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            var users = _store.Values
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult<IEnumerable<User>>(users);
        }

        public Task<User?> GetAsync(Guid id)
        {
            _store.TryGetValue(id, out var user);
            return Task.FromResult(user);
        }

        public Task<User?> GetByEmailAsync(string email)
        {
            var user = _store.Values.FirstOrDefault(u => 
                u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(user);
        }

        public Task<User> CreateAsync(User user)
        {
            user.Id = Guid.NewGuid();
            _store[user.Id] = user;
            return Task.FromResult(user);
        }

        public Task<bool> UpdateAsync(User user)
        {
            if (!_store.ContainsKey(user.Id)) return Task.FromResult(false);
            _store[user.Id] = user;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(Guid id)
        {
            var removed = _store.TryRemove(id, out _);
            return Task.FromResult(removed);
        }
    }
}