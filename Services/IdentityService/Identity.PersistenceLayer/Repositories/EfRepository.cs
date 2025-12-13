using Identity.ApplicationLayer.Abstractions;
using Identity.PersistenceLayer.Db;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Identity.PersistenceLayer.Repositories
{
    public class EfRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _db;
        private readonly DbSet<T> _set;

        public EfRepository(AppDbContext db)
        {
            _db = db;
            _set = _db.Set<T>();
        }

        public Task<T?> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
            => _set.FirstOrDefaultAsync(predicate, ct);

        public async Task AddAsync(T entity, CancellationToken ct = default)
            => await _set.AddAsync(entity, ct);

        public Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            _set.Update(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
            => _db.SaveChangesAsync(ct);
    }
}
