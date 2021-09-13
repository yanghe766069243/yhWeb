using NewSchoolLive.IOC;
using NewSchoolLive.Model;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NewSchoolLive.IRepository
{
    public interface IBaseDbFirstRepository<TEntity> where TEntity : class, new()
    {

        #region 异步方法

        /// <summary>
        /// 数据新增（单个）
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<TEntity> InsertAsync(TEntity entity);

        /// <summary>
        /// 数据新增（单个）
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> InsertBoolAsync(TEntity entity);
        void BeginTran();
        void CommitTran();
        void RollbackTran();
        /// <summary>
        /// 数据新增（批量）
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <returns>受影响的行数</returns>
        Task<int> InsertAsync(IEnumerable<TEntity> entities);


        /// <summary>
        /// 根据实体对象删除，实体对象Id属性必须大于0
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <returns>受影响的行数</returns>
        Task<int> DeleteAsync(TEntity entity);


        /// <summary>
        /// 根据实体对象批量删除，实体对象Id属性必须大于0
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <returns>受影响的行数</returns>
        Task<int> DeleteAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// 根据主键Id删除
        /// </summary>
        /// <param name="id">主键Id</param>
        /// <returns>受影响的行数</returns>
        Task<int> DeleteAsync(string id);

        Task<bool> DeleteBoolAsync(string id);

        /// <summary>
        /// 根据主键Id集合批量删除
        /// </summary>
        /// <param name="ids">主键Id集合</param>
        /// <returns>受影响的行数</returns>
        Task<int> DeleteAsync(IEnumerable<string> ids);


        /// <summary>
        /// 根据条件删除
        /// </summary>
        /// <param name="where">删除条件</param>
        /// <returns>受影响的行数</returns>
        Task<int> DeleteAsync(Expression<Func<TEntity, bool>> where);


        /// <summary>
        /// 单个实体更新
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="updateColumns">要更新的列</param>
        /// <param name="where">更新条件</param>
        /// <returns>受影响的行数</returns>
        Task<int> UpdateAsync(TEntity entity, Expression<Func<TEntity, object>> updateColumns = null, Expression<Func<TEntity, bool>> where = null);

        Task<bool> UpdateBoolAsync(TEntity entity);
        /// <summary>
        /// 集合批量更新
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="whereColumns">每个实体对象中的更新条件</param>
        /// <param name="updateColumns"></param>
        /// <returns></returns>
        Task<int> UpdateAsync(IEnumerable<TEntity> entities, Expression<Func<TEntity, object>> whereColumns, Expression<Func<TEntity, object>> updateColumns);


        /// <summary>
        /// 查询单个
        /// </summary>
        /// <param name="where">查询条件</param>
        /// <returns></returns>
        Task<TEntity> QueryBySingleAsync(Expression<Func<TEntity, bool>> where);


        /// <summary>
        /// 查询集合
        /// </summary>
        /// <param name="where">查询条件</param>
        /// <returns></returns>
        Task<List<TEntity>> QueryByListAsync(Expression<Func<TEntity, bool>> where);

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="where">查询条件</param>
        /// <param name="orderByColumn">排序字段</param>
        /// <param name="orderByType">升序或降序</param>
        /// <param name="pageIndex">页容量</param>
        /// <param name="pageSize">页索引</param>
        /// <param name="totalNumber">总行数</param>
        /// <returns></returns>
        Task<List<TEntity>> QueryByPageListAsync(Expression<Func<TEntity, bool>> where, Expression<Func<TEntity, object>> orderByColumn, OrderByType orderByType, int pageIndex, int pageSize, RefAsync<int> total);
        #endregion
        ISugarQueryable<TEntity> GetSugarQueryable();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        SqlSugarScope DbBase();
    }
}
