using NewSchoolLive.Common;
using NewSchoolLive.IOC;
using NewSchoolLive.IRepository;
using NewSchoolLive.Model;
using Newtonsoft.Json;
using SqlSugar;
using SqlSugar.IOC;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NewSchoolLive.Repository
{
    public class BaseDbFirstRepository<TEntity> : SimpleClient<TEntity>, IBaseDbFirstRepository<TEntity>, IDbScoped where TEntity : class, new()
    {
        public SqlSugarScope _sqlSugarClientService;
        private readonly string RedisKey = "Data";
        private readonly IRedisHelper _redisCache;
        private string TableName;
        public BaseDbFirstRepository(IRedisHelper redisCache, ISqlSugarClient sqlSugarClient=null) : base(sqlSugarClient)
        {
            _sqlSugarClientService = DbScoped.SugarScope;
            sqlSugarClient = DbScoped.SugarScope;
            TableName = typeof(TEntity).Name;
            this._redisCache = redisCache;
        }

        //public async Task<bool> InsertRangeAsync(List<TEntity> insertObjs)
        //{
        //    return await base.InsertRangeAsync(insertObjs);
        //}

        #region 异步方法

        /// <summary>
        /// 数据新增（单个）
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<TEntity> InsertAsync(TEntity entity)
        {
            if (entity == null)
                return null;
            return await _sqlSugarClientService.Insertable<TEntity>(entity).ExecuteReturnEntityAsync();
        }
        /// <summary>
        /// 数据新增（单个）
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<bool> InsertBoolAsync(TEntity entity)
        {
            if (entity == null)
                return false;
            var ins = await _sqlSugarClientService.Insertable<TEntity>(entity).ExecuteCommandIdentityIntoEntityAsync();

            return ins;
        }
        /// <summary>
        /// 新增数据 返回long类型的主键 并且带有新增到Redis（单个 不支持批量插入）
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<long> InsertLongAsync(TEntity entity)
        {
            if (entity == null)
                return 0;
            //新增数据 剔除为null的不插入 返回雪花主键Id long类型
            var Id = await _sqlSugarClientService.Insertable<TEntity>(entity).IgnoreColumns(ignoreNullColumn: true).ExecuteReturnSnowflakeIdAsync();
            if (Id > 0)
            {
                //var data = _sqlSugarClientService.Queryable<TEntity>().InSingle(Id); //根据主键查询
                //新增成功删除缓存 
                //await redis.KeyDeleteAsync($"{RedisKey}:{TableName}");
            }
            return Id;
        }
        public void BeginTran()
        {
            _sqlSugarClientService.BeginTran();
        }

        public void CommitTran()
        {
            _sqlSugarClientService.CommitTran();
        }
        public void RollbackTran()
        {
            _sqlSugarClientService.RollbackTran();
        }
        /// <summary>
        /// 数据新增（批量）
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> InsertAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null || !entities.Any())
                return 0;
            return await _sqlSugarClientService.Insertable<TEntity>(entities.ToList()).ExecuteCommandAsync();
        }

        /// <summary>
        /// 根据实体对象删除，实体对象Id属性必须大于0
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> DeleteAsync(TEntity entity)
        {
            if (entity == null)
                return 0;
            return await _sqlSugarClientService.Deleteable<TEntity>(entity).ExecuteCommandAsync();
        }

        /// <summary>
        /// 根据实体对象批量删除，实体对象Id属性必须大于0
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> DeleteAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null || !entities.Any())
                return 0;
            return await _sqlSugarClientService.Deleteable<TEntity>(entities.ToList()).ExecuteCommandAsync();
        }

        /// <summary>
        /// 根据主键Id删除
        /// </summary>
        /// <param name="id">主键Id</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return 0;
            return await _sqlSugarClientService.Deleteable<TEntity>().In<string>(id).ExecuteCommandAsync();
        }
        /// <summary>
        /// 根据主键Id删除
        /// </summary>
        /// <param name="id">主键Id</param>
        /// <returns>受影响的行数</returns>
        public async Task<bool> DeleteBoolAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;
            return await _sqlSugarClientService.Deleteable<TEntity>().In<string>(id).ExecuteCommandHasChangeAsync();
        }
        /// <summary>
        /// 根据主键Id集合批量删除
        /// </summary>
        /// <param name="ids">主键Id集合</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> DeleteAsync(IEnumerable<string> ids)
        {
            if (ids == null || !ids.Any() || !ids.Any(m => m != ""))
                return 0;
            return await _sqlSugarClientService.Deleteable<TEntity>().In<string>(ids.ToList()).ExecuteCommandAsync();
        }

        /// <summary>
        /// 根据条件删除
        /// </summary>
        /// <param name="where">删除条件</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> DeleteAsync(Expression<Func<TEntity, bool>> where)
        {
            if (where == null)
                return 0;
            return await _sqlSugarClientService.Deleteable<TEntity>().Where(where).ExecuteCommandAsync();
        }

        /// <summary>
        /// 单个实体更新
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="updateColumns">要更新的列</param>
        /// <param name="where">更新条件</param>
        /// <returns>受影响的行数</returns>
        public async Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> updateColumns = null, Expression<Func<TEntity, bool>> where = null)
        {
            IUpdateable<TEntity> updateableService = _sqlSugarClientService.Updateable<TEntity>(entity);
            //仅当按照实体对象更新时，主键Id必须大于0
            if (updateColumns == null && where == null && entity == null)
            {
                return 0;
            }
            if (updateColumns != null)
            {
                updateableService = updateableService.UpdateColumns(updateColumns);
            }
            if (where != null)
            {
                updateableService = updateableService.Where(where);
            }
            return await updateableService.ExecuteCommandAsync();
        }
        /// <summary>
        /// 单个实体更新
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="updateColumns">要更新的列</param>
        /// <param name="where">更新条件</param>
        /// <returns>受影响的行数</returns>
        public async Task<bool> UpdateBoolAsync(TEntity entity)
        {
            return await _sqlSugarClientService.Updateable(entity).ExecuteCommandHasChangeAsync();
        }
        /// <summary>
        /// 集合批量更新
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="whereColumns">每个实体对象中的更新条件</param>
        /// <param name="updateColumns"></param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> whereColumns, Expression<Func<TEntity, object>> updateColumns)
        {
            return await _sqlSugarClientService.Updateable<TEntity>(entities.ToList()).UpdateColumns(updateColumns).WhereColumns(whereColumns).ExecuteCommandAsync();
        }

        /// <summary>
        /// 查询单个
        /// </summary>
        /// <param name="where">查询条件</param>
        /// <returns></returns>
        public async Task<TEntity> QueryBySingleAsync(Expression<Func<TEntity, bool>> where)
        {
            return await _sqlSugarClientService.Queryable<TEntity>().FirstAsync(where);
        }

        /// <summary>
        /// 查询集合
        /// </summary>
        /// <param name="where">查询条件</param>
        /// <returns></returns>
        public async Task<List<TEntity>> QueryByListAsync(Expression<Func<TEntity, bool>> where)
        {
            return await _sqlSugarClientService.Queryable<TEntity>().Where(where).ToListAsync();
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="where">查询条件</param>
        /// <param name="orderByColumn">排序列</param>
        /// <param name="orderByType">升序或降序</param>
        /// <param name="pageIndex">页索引</param>
        /// <param name="pageSize">页容量</param>
        /// <param name="totalNumber">总行数</param>
        /// <returns></returns>
        public async Task<List<TEntity>> QueryByPageListAsync(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, object>> orderByColumn, OrderByType orderByType, int pageIndex, int pageSize, RefAsync<int> total)
        {
            return await _sqlSugarClientService.Queryable<TEntity>().Where(where).OrderBy(orderByColumn, orderByType).ToPageListAsync(pageIndex, pageSize, total);
        }
        #endregion
        public ISugarQueryable<TEntity> GetSugarQueryable()
        {
            return _sqlSugarClientService.Queryable<TEntity>();
        }

        public SqlSugarScope DbBase() => _sqlSugarClientService;
    }
}
