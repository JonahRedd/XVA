using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using NgDataSync.Core.Model;
using NgDataSync.Core.Interfaces.Data;
using NgDataSync.Core.Interfaces.Paging;
using NgDataSync.Core.Common.Paging;
using NgDataSync.Data.Helpers;

namespace NgDataSync.Data
{
    /// <summary>
    /// An abstract baseclass handling basic CRUD operations against the context.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseRepository<T> : IDisposable, IRepository<T> where T : PersistentEntity
    {
        private IDataContext _context;
        private readonly IDbSet<T> _dbset;
        private readonly IDatabaseFactory _databaseFactory;

        protected BaseRepository(IDatabaseFactory databaseFactory)
        {
            this._databaseFactory = databaseFactory;
            this._dbset = this.DataContext.DbSet<T>();
        }

        public IDataContext DataContext
        {
            get { return this._context ?? (this._context = this._databaseFactory.Get()); }
        }

        /// <summary>
        /// The name of the Generic entity using the repository.
        /// Used for smoother queries.
        /// </summary>
        protected string EntitySetName { get; set; }

        /// <summary>
        /// Saves a new entity of T or updates an in the context existing entity (if it´s changed).
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual void SaveOrUpdate(T entity)
        {
            this.DataContext.Entry(entity).State = CurrentState(entity);
        }

        private EntityState CurrentState(T entity)
        {
            return UnitOfWork.IsPersistent(entity) ? EntityState.Modified : EntityState.Added;
        }

        /// <summary>
        /// Get one entity of T
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public virtual T GetById(int id)
        {
            return this._dbset.Find(id);
        }

        /// <summary>
        /// Get all entities of T
        /// </summary>
        /// <returns></returns>
        public virtual IQueryable<T> GetAll()
        {
            return this._dbset;
        }

        /// <summary>
        /// Get all entities of T without tracking
        /// </summary>
        /// <returns></returns>
        public virtual IQueryable<T> GetAllReadOnly()
        {
            return this._dbset.AsNoTracking();
        }

        /// <summary>
        /// Removes an entity T from the context and persist the change.
        /// </summary>
        /// <param name="entity"></param>
        public virtual void Delete(T entity)
        {
            this.DataContext.Entry(entity).State = EntityState.Deleted;
        }

        public virtual void Delete(int id)
        {
            this.Delete(this._dbset.Find(id));
        }

        /// <summary>
        /// The LinqExpression will give us the opportunity to write strongly typed object queries to this methodsignature.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="maxHits"></param>
        /// <returns></returns>
        public virtual IEnumerable<T> Find(Func<T, bool> expression, int maxHits = 100)
        {
            return this._dbset.Where(expression).Take(maxHits);
        }

        /// <summary>
        /// Dynamic search in every public property on the current DbSet (entity)
        /// </summary>
        /// <param name="searchKey"></param>
        /// <param name="exactMatch"></param>
        /// <returns></returns>
        public IQueryable<T> Search(string searchKey, bool exactMatch)
        {
            IQueryable<T> set = this._dbset.AsQueryable() as IQueryable<T>;
            return set.FullTextSearch(searchKey, exactMatch, SqlOperators.Or);
        }

        /// <summary>
        /// Provides paging possibilites
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public IPage<T> Page(int page = 1, int pageSize = 10)
        {
            var internalPage = page - 1;
            var data = this._dbset.OrderBy(k => k.Id).Skip(pageSize * internalPage).Take(pageSize).AsEnumerable();
            return new Page<T>(data, this._dbset.Count(), pageSize, page);
        }

        public void Dispose()
        {
            this.DataContext.Dispose();
        }
    }
}