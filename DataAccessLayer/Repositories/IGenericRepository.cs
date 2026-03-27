namespace DataAccessLayer.Repositories
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity?> GetByIdAsync(object id);
        Task InsertAsync(TEntity entity);
        void Update(TEntity entity);
        Task DeleteAsync(object id);
        Task<int> SaveChangesAsync();
    }
}
