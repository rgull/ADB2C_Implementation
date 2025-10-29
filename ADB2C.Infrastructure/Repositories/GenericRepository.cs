using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = _context.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(object id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
    }

    public async Task<T> AddAsync(T entity)
    {
        var dbEntity = await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return dbEntity.Entity;
    }


    public async Task<T> UpdateAsync(T entity)
    {
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            _context.Attach(entity);
        }
        entry.State = EntityState.Modified;

        await _context.SaveChangesAsync();

        return entry.Entity;
    }

    public async Task<bool> DeleteAsync(object id)
    {
        // Try to find a property that could represent the primary key
        var idProp = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                              .FirstOrDefault(p =>
                                  string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(p.Name, $"{typeof(T).Name}Id", StringComparison.OrdinalIgnoreCase));

        if (idProp == null)
            throw new InvalidOperationException($"No suitable Id property found for type {typeof(T).Name}");



        // Build a dynamic LINQ expression: x => x.Id == id
        //var parameter = Expression.Parameter(typeof(T), "x");
        //var propertyAccess = Expression.Property(parameter, idProp);
        //var equals = Expression.Equal(propertyAccess, Expression.Constant(id));
        //var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);

        // Check how many entities match
        var count = await _dbSet.AsNoTracking().CountAsync(x => idProp.GetValue(x) == id);

        if (count == 0)
        {
            // Nothing to delete
            return false;
        }

        if (count > 1)
        {
            throw new InvalidOperationException(
                $"Multiple entities found for {typeof(T).Name} with ID {id}. Aborting delete.");
        }

        // Exactly one match — use ExecuteDeleteAsync for efficiency
        await _dbSet.Where(x => idProp.GetValue(x) == id).ExecuteDeleteAsync();
        return true;
    }


}
