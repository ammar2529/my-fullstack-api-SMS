using SchoolMS.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchoolMS.Core.Interfaces
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(int id);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(int id);   // Soft delete — IsActive = false
    }
}
