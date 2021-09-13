using NewSchoolLive.IOC;
using NewSchoolLive.IRepository;
using NewSchoolLive.IService;
using NewSchoolLive.Model;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NewSchoolLive.Service
{
    public class DbFirstBaseService<TEntity> : IDbFirstBaseService<TEntity>, IDbScoped where TEntity : class, new()
    {
        public IBaseDbFirstRepository<TEntity> _baseRepository;
        public DbFirstBaseService(IBaseDbFirstRepository<TEntity> baseRepository)
        {
            this._baseRepository = baseRepository;
        }

        public async Task<int> DeleteAsync(TEntity entity)
        {
            return await _baseRepository.DeleteAsync(entity);
        }

        public async Task<int> DeleteAsync(IEnumerable<TEntity> entities)
        {
            return await _baseRepository.DeleteAsync(entities);
        }

        public async Task<int> DeleteAsync(string id)
        {
            return await _baseRepository.DeleteAsync(id);
        }

        public async Task<int> DeleteAsync(IEnumerable<string> ids)
        {
            return await _baseRepository.DeleteAsync(ids);
        }

        public async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> where)
        {
            return await _baseRepository.DeleteAsync(where);
        }

        public async Task<TEntity> InsertAsync(TEntity entity)
        {
            return await _baseRepository.InsertAsync(entity);
        }
        public async Task<bool> InsertBoolAsync(TEntity entity)
        {
            return await _baseRepository.InsertBoolAsync(entity);
        }
        public async Task<int> InsertAsync(IEnumerable<TEntity> entities)
        {
            return await _baseRepository.InsertAsync(entities);
        }

        public async Task<List<TEntity>> QueryByListAsync(Expression<Func<TEntity, bool>> where)
        {
            return await _baseRepository.QueryByListAsync(where);
        }

        public async Task<List<TEntity>> QueryByPageListAsync(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, object>> orderByColumn, OrderByType orderByType, int pageIndex, int pageSize, RefAsync<int> total)
        {
            return await _baseRepository.QueryByPageListAsync(where, orderByColumn, orderByType, pageIndex, pageSize, total);
        }

        public async Task<TEntity> QueryBySingleAsync(Expression<Func<TEntity, bool>> where)
        {
            return await _baseRepository.QueryBySingleAsync(where);
        }

        /// <summary>
        /// 删除数据（软删，指设置IsDelete为True）
        /// </summary>
        /// <param name="entity">实体对象（Id值必须大于0）</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> DeleteSignAsync(TEntity entity)
        {
            return await _baseRepository.UpdateAsync(entity);
        }
        public async Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> updateColumns = null, Expression<Func<TEntity, bool>> where = null)
        {
            return await _baseRepository.UpdateAsync(entity, updateColumns, where);
        }

        public async Task<int> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> whereColumns, Expression<Func<TEntity, object>> updateColumns)
        {
            return await _baseRepository.UpdateAsync(entities, whereColumns, updateColumns);
        }
        public ISugarQueryable<TEntity> GetSugarQueryable()
        {
            return _baseRepository.DbBase().Queryable<TEntity>();
        }

        public SqlSugarScope DbBase() => _baseRepository.DbBase();
    }
}
