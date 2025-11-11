using Microsoft.EntityFrameworkCore;

namespace CoinCraft.Infrastructure.Repositories;

public abstract class RepositoryBase<T> where T : class
{
    protected readonly CoinCraftDbContext _ctx;
    protected RepositoryBase(CoinCraftDbContext ctx) => _ctx = ctx;

    public Task<List<T>> GetAllAsync() => _ctx.Set<T>().AsNoTracking().ToListAsync();
    public Task<T?> FindAsync(int id) => _ctx.Set<T>().FindAsync(id).AsTask();
    public async Task<T> AddAsync(T entity)
    {
        _ctx.Set<T>().Add(entity);
        await _ctx.SaveChangesAsync();
        return entity;
    }
    public async Task UpdateAsync(T entity)
    {
        _ctx.Set<T>().Update(entity);
        await _ctx.SaveChangesAsync();
    }
    public async Task DeleteAsync(T entity)
    {
        _ctx.Set<T>().Remove(entity);
        await _ctx.SaveChangesAsync();
    }
}
