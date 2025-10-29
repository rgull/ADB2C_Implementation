using ADB2C.Application.Interfaces;
using ADB2C.Domain.Entities;
using ADB2C.Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ADB2C.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {

        public UserRepository(ApplicationDbContext context)
            : base(context)
        {
        }

    }
}

