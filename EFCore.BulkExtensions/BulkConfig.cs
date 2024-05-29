using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace EFCore.BulkExtensions;

/// <summary>
/// Provides configration for EFCore BulkExtensions
/// ΪEFCore BulkExtensions�ṩ����
/// </summary>
public class BulkConfig
{
    /// <summary>
    ///     Makes sure that entites are inserted to Db as ordered in entitiesList.
    ///     ȷ��ʵ�尴��ʵ���б��е�˳�����Db��
    /// </summary>
    /// <value>
    ///     Default value is <c>true</c>, if table has Identity column (autoincrement) and IDs being 0 in list they will temporarily be changed automatically from 0s into range -N:-1.
    ///     Ĭ����true���������С��������У��Զ�����������ʵ���е�id��0�����Զ��ı�
    /// </value>
    public bool PreserveInsertOrder { get; set; } = true;

    /// <summary>
    ///     When set IDs zero values will be updated to new ones from database (Have function only when PK has Identity)
    ///     ������IDʱ����ֵ������Ϊ���ݿ��е���ֵ��ֻ�е�PK���б�ʶʱ�ž��й��ܣ�
    /// </summary>
    /// <remarks>
    ///     Useful when BulkInsert is done to multiple related tables, to get PK of table and to set it as FK for second one.
    ///     ���Զ����ر�ִ��BulkInsertʱ����ȡ�����������������Ϊ�ڶ������FK�ǳ����á�
    /// </remarks>
    public bool SetOutputIdentity { get; set; }

    /// <summary>
    ///    Used only when SetOutputIdentity is set to true, and if this remains True (which is default) all columns are reloaded from Db.
    ///    When changed to false only Identity column is loaded.
    ///    ����SetOutputIdentity����Ϊtrueʱʹ�ã������ֵ����ΪTrue��Ĭ��ֵ�������Db���¼��������У�
    ///    ������Ϊfalseʱ�������ء���ʶ���С�
    /// </summary>
    /// <remarks>
    ///     Used for efficiency to reduce load back from DB.
    ///     �������Ч���Լ��ٴ����ݿⷵ�صĸ��ء�
    /// </remarks>
    public bool SetOutputNonIdentityColumns { get; set; } = true;

    /// <summary>
    ///    Used only when SetOutputIdentity is set to true, and when changed to True then columns that were no included in Upsert are not loaded.
    ///    ����SetOutputIdentity����Ϊtrueʱʹ�ã�������ΪTrueʱ��������Upsert��δ�������С�
    /// </summary>
    public bool LoadOnlyIncludedColumns { get; set; } = false;

    /// <summary>
    ///     Propagated to SqlBulkCopy util object.
    ///     �Ѵ�����SqlBulkCopy util����
    /// </summary>
    /// <value>
    ///     Defalut value is 2000.
    /// </value>
    public int BatchSize { get; set; } = 2000;

    /// <summary>
    ///     Propagated to SqlBulkCopy util object. When not set will have same value of BatchSize, each batch one notification.
    ///     �Ѵ�����SqlBulkCopy util�������δ���ã��򽫾�����ͬ��BatchSizeֵ��ÿ������һ��֪ͨ��
    /// </summary>
    public int? NotifyAfter { get; set; }

    /// <summary>
    ///     Propagated to SqlBulkCopy util object. When not set has SqlBulkCopy default which is 30 seconds and if set to 0 it indicates no limit.
    ///     �Ѵ�����SqlBulkCopy util�������δ���ã���SqlBulkCopyĬ��ֵΪ30�룬�������Ϊ0�����ʾû�����ơ�
    /// </summary>
    public int? BulkCopyTimeout { get; set; }

    /// <summary>
    ///     When set to <c>true</c> Temp tables are created as #Temporary. More info: <c>https://www.sqlservertutorial.net/sql-server-basics/sql-server-temporary-tables/</c>
    ///     ������Ϊtrueʱ���ᴴ����ʱ��
    /// </summary>
    /// <remarks>
    ///     If used then BulkOperation has to be inside Transaction, otherwise destination table gets dropped too early because transaction ends before operation is finished.
    ///     ��ʹ��BulkOperation���Ǹ�ôBulkOperation������Transaction�ڲ�������Ŀ�������ر���������Ϊ�����ڲ������֮ǰ�ͽ����ˡ�
    /// </remarks>
    public bool UseTempDB { get; set; }

    /// <summary>
    ///     When set to false temp table name will be only 'Temp' without random numbers.
    ///     ������Ϊfalseʱ����ʱ�����ƽ���Ϊ����������ġ�temp����
    /// </summary>
    /// <value>
    ///     Default value is <c>true</c>.
    /// </value>
    public bool UniqueTableNameTempDb { get; set; } = true;

    /// <summary>
    ///     When set it appends 'OPTION (LOOP JOIN)' for SqlServer, to reduce potential deadlocks on tables that have FKs.
    ///     ����ʱ������ΪSqlServer���ӡ�OPTION��LOOP JOIN�������Լ��پ���FK�ı���Ǳ�ڵ�������
    /// </summary>
    /// <remarks>
    ///     Use this hint as a last resort for experienced devs and db admins.
    ///     ������ʾ��Ϊ����ḻ�Ŀ�����Ա�����ݿ����Ա������ֶΡ�
    /// </remarks>
    /// <value>
    ///     Default value is <c>false</c>.
    /// </value>
    public bool UseOptionLoopJoin { get; set; } = false;

    /// <summary>
    ///     Enables specifying custom name of table in Db that does not have to be mapped to Entity.
    ///     ����ָ��Db�в���ӳ�䵽ʵ��ı���Զ������ơ�
    /// </summary>
    /// <value>
    ///     Can be set with 'TableName' only or with 'Schema.TableName'.
    ///     ����������tableName��Schema.TableName
    /// </value>
    public string? CustomDestinationTableName { get; set; }

    /// <summary>
    ///     Source data from specified table already in Db, so input list not used and can be empty.
    ///     ָ�����е�Դ��������Db�У����δʹ�������б����ҿ���Ϊ�ա�
    /// </summary>
    /// <value>
    ///     Can be set with 'TableName' only or with 'Schema.TableName' (Not supported for Sqlite).
    ///     ����������TableName��Schema.tableName ����֧��sqlite��
    /// </value>
    public string? CustomSourceTableName { get; set; }

    /// <summary>
    ///     Only if CustomSourceTableName is set and used for specifying Source - Destination column names when they are not the same.
    ///     ����CustomSourceTableName�����ò�����ָ��Դ-Ŀ��������ʱ�������ǲ���ͬʱ����
    /// </summary>
    public Dictionary<string, string>? CustomSourceDestinationMappingColumns { get; set; }

    /// <summary>
    ///     When configured data is loaded from this object instead of entity list which should be empty
    ///     ���Ӹö��������Ӧ��Ϊ�յ�ʵ���б�������õ�����ʱ
    /// </summary>
    public IDataReader? DataReader { get; set; }

    /// <summary>
    ///     Can be used when DataReader is also configured and when set it is propagated to SqlBulkCopy util object, useful for big field like blob, binary column.
    ///     Ҳ����������DataReaderʱʹ�ã�����������ʱ���䴫����SqlBulkCopy util��������ڴ��ֶΣ���blob���������У��ǳ����á�
    /// </summary>
    public bool EnableStreaming { get; set; }

    /// <summary>
    ///     Can be set to True if want to have tracking of entities from BulkRead or when SetOutputIdentity is set.
    ///     ���Ҫ��BulkRead����ʵ�壬����������SetOutputIdentity�����������ΪTrue��
    /// </summary>
    public bool TrackingEntities { get; set; }

    /// <summary>
    ///     Sql MERGE Statement contains 'WITH (HOLDLOCK)', otherwise if set to <c>false</c> it is removed.
    ///     Sql MERGE��������WITH��HOLDLOCK�����������������Ϊ<c>false</c>��������ɾ����
    /// </summary>
    /// <value>
    ///     Default value is <c>true</c>.
    /// </value>
    public bool WithHoldlock { get; set; } = true;

    /// <summary>
    ///     When set to <c>true</c> the result is return in <c>BulkConfig.StatsInfo { StatsNumberInserted, StatsNumberUpdated}</c>.
    ///     ������Ϊtrueʱ���������BulkConfig.StatsInfo { StatsNumberInserted, StatsNumberUpdated}������
    /// </summary>
    /// <remarks>
    ///     If used for pure Insert (with Batching) then SetOutputIdentity should also be configured because Merge have to be used.
    ///     ������ڴ����루������������Ӧ����SetOutputIdentity����Ϊ����ʹ��Merge��
    /// </remarks>
    public bool CalculateStats { get; set; }

    /// <summary>
    ///     Ignore handling RowVersion column.
    ///     ���Դ���RowVersion�С�
    /// </summary>
    /// <value>
    ///     Default value is <c>false</c>, if table have any RowVersion column, it will have special handling and needs to be binary.
    ///     �������κ�RowVersion�У�����������Ĵ���������Ҫ�Ƕ����Ƶġ�
    /// </value>
    public bool IgnoreRowVersion { get; set; } = false;

    /// <summary>
    ///     Used as object for returning Stats Info when <c>BulkConfig.CalculateStats = true</c>.
    ///     ��BulkConfig.CalculateStats = trueʱ�����صĶ���
    /// </summary>
    /// <value>
    ///     Contains info in Properties: <c>StatsNumberInserted, StatsNumberUpdated, StatsNumberDeleted</c>
    ///     ���������ԣ�����ļ�¼�������µļ�¼����ɾ���ļ�¼��
    /// </value>
    public StatsInfo? StatsInfo { get; internal set; }

    /// <summary>
    ///     Used as object for returning TimeStamp Info when <c>BulkConfig.DoNotUpdateIfTimeStampChanged = true</c>.
    ///     ��BulkConfig.DoNotUpdateIfTimeStampChanged = trueʱ������TimeStamp��Ϣ
    /// </summary>
    public TimeStampInfo? TimeStampInfo { get; internal set; }

    /// <summary>
    ///     When doing Insert/Update properties to affect can be explicitly selected by adding their names into PropertiesToInclude.
    ///     ��ִ�С�����/���¡�����ʱ������ͨ����ҪӰ������Ե�������ӵ���Ҫ���������ԡ�������ʽѡ��ҪӰ������ԡ�
    /// </summary>
    /// <remarks>
    ///     If need to change more then half columns then PropertiesToExclude can be used. Setting both Lists are not allowed.
    ///     �����Ҫ���ĳ������У������ʹ��Properties ToExclude��������ͬʱ�����������б�
    /// </remarks>
    public List<string>? PropertiesToInclude { get; set; }

    /// <summary>
    ///     By adding a column name to this list, will allow it to be inserted and updated but will not update the row if any of the these columns in that row did not change.
    ///     ͨ������б���������������������͸��¸��У����������е��κ���δ���ģ��򲻻���¸��С�
    /// </summary>
    /// <remarks>
    ///     For example, if importing data and want to keep an internal UpdateDate, add all columns except that one, or use PropertiesToExcludeOnCompare.
    ///     ���磬����������ݲ�ϣ�������ڲ�UpdateDate������ӳ�����֮��������У�����ʹ��PropertiesToExcludeOnCompare��
    /// </remarks>
    public List<string>? PropertiesToIncludeOnCompare { get; set; }


    /// <summary>
    ///     By adding a column name to this list, will allow it to be inserted and updated but will not update the row if any of the others columns in that row did not change.
    ///     ͨ����������ӵ����б��У����������͸�������������������е��κ�������û�и��ģ��򲻻���¸��С�
    /// </summary>
    /// <remarks>
    ///     For example, if importing data and want to keep an internal UpdateDate, add that columns to the UpdateDate.
    ///     ���磬����������ݲ�ϣ�������ڲ�UpdateDate���뽫������ӵ�UpdateDate��
    /// </remarks>
    public List<string>? PropertiesToExcludeOnCompare { get; set; }

    /// <summary>
    ///     Ensures that only certain columns with selected properties are Updated. Can differ from PropertiesToInclude that can that be used for Insert config only.
    ///     ȷ�������¾���ѡ�����Ե�ĳЩ�С�������ֻ�����ڲ������õ�PropertiesToInclude��ͬ��
    /// </summary>
    /// <remarks>
    ///     When need to Insert only new and skip existing ones in Db (Insert_if_not_Exist) then use BulkInsertOrUpdate with this list set to empty: <c>new List<string> { "" }</string></c>
    ///     ����Ҫ��Db��ֻ�����µĲ��������е�ʱ��Insert_if_not_Exist������ʹ��BulkInsertOrUpdate�������б�����Ϊ�գ�<c>���б�<string>��������</string></c>
    /// </remarks>
    public List<string>? PropertiesToIncludeOnUpdate { get; set; }

    /// <summary>
    ///     When doing Insert/Update one or more properties can be exclude by adding their names into PropertiesToExclude.
    ///     ִ�С�����/���¡�ʱ������ͨ����һ���������Ե�������ӵ���PropertiesToExclude�������ų���Щ���ԡ�
    /// </summary>
    /// <remarks>
    ///     If need to change less then half column then PropertiesToInclude can be used. Setting both Lists are not allowed.
    ///     �����Ҫ�������ڰ��У������ʹ��PropertiesToInclude��������ͬʱ�����������б�
    /// </remarks>
    public List<string>? PropertiesToExclude { get; set; }


    /// <summary>
    ///     Selected properties are excluded from being updated, can differ from PropertiesToExclude that can be used for Insert config only.
    ///     ��ѡ���Ա��ų��ڸ���֮�⣬���Բ�ͬ��PropertiesToExclude���������ڲ������õģ�
    /// </summary>
    public List<string>? PropertiesToExcludeOnUpdate { get; set; }

    /// <summary>
    ///     Used for specifying custom properties, by which we want update to be done.
    ///     ����ָ���Զ������ԣ�����ϣ��ͨ�������Խ��и��¡�
    /// </summary>
    /// <remarks>
    ///     If Identity column exists and is not added in UpdateByProp it will be excluded automatically.
    ///     �������ʶ���д��ڲ���δ��ӵ�UpdateByProp�У�����Զ������ų����⡣
    /// </remarks>
    public List<string>? UpdateByProperties { get; set; }

    /// <summary>
    ///     Used for specifying a function that returns custom SQL to use for conditional updates on merges.
    ///     ����ָ��һ���������ú����������ںϲ��������µ��Զ���SQL��
    /// </summary>
    /// <remarks>
    ///     Function receives (existingTablePrefix, insertedTablePrefix) and should return the SQL of the WHERE clause.
    ///     �������գ�existingTablePrefix��insertedTablePrefix������Ӧ����WHERE�Ӿ��SQL��
    ///     The SQLite implementation uses UPSERT functionality added in SQLite 3.24.0 (https://www.sqlite.org/lang_UPSERT.html).
    /// </remarks>
    public Func<string, string, string>? OnConflictUpdateWhereSql { get; set; }

    /// <summary>
    ///     When set to <c>true</c> it will adding (normal) Shadow Property and persist value. It Disables automatic discrimator, so it shoud be set manually.
    ///     ������Ϊtrueʱ��������ӣ���������Ӱ���Ժͳ־�ֵ�����������Զ��б���������Ӧ���ֶ����á�
    /// </summary>
    public bool EnableShadowProperties { get; set; }


    /// <summary>
    ///     Returns value for shadow properties, EnableShadowProperties = true
    ///     ������Ӱ���Ե�ֵ��EnableShadowProperties=true
    /// </summary>
    public Func<object, string, object?>? ShadowPropertyValue { get; set; }


    /// <summary>
    ///    Shadow columns used for Temporal table. Has defaults elements: 'PeriodStart' and 'PeriodEnd'. Can be changed if temporal columns have custom names.
    ///    ������ʱ�����Ӱ�С���Ĭ�ϵ�Ԫ�أ�PeriodStart��PeriodEnd�������ʱ�о����Զ������ƣ�����Ը���
    /// </summary>
    public List<string> TemporalColumns { get; set; } = new List<string> { "PeriodStart", "PeriodEnd" };

    /// <summary>
    ///     When set all entites that have relations with main ones from the list are also merged into theirs tables.
    ///     ����ʱ�����б��е���Ҫʵ���й�ϵ������ʵ��Ҳ��ϲ������ǵı��С�
    /// </summary>
    /// <remarks>
    ///     Essentially enables with one call bulk ops on multiple tables that are connected, like parent-child relationship with FK
    ///     �����ϣ������ӵĶ����������һ�ε�����������������FK�ĸ��ӹ�ϵ
    /// </remarks>
    public bool IncludeGraph { get; set; }

    /// <summary>
    ///     Removes the clause 'EXISTS ... EXCEPT' from Merge statement which then updates even same data, useful when need to always active triggers.
    ///     ɾ���Ӿ�'EXISTS������EXCEPT����Merge��䣬���������������ͬ�����ݣ�����Ҫʼ�ռ������ʱ�ǳ����á�
    /// </summary>
    public bool OmitClauseExistsExcept { get; set; }

    /// <summary>
    ///     When set to <c>true</c> rows with concurrency conflict, meaning TimeStamp column is changed since read, 
    ///     will not be updated their entities will be loaded into <c>BulkConfig.TimeStampInfo { NumberOfSkippedForUpdate, EntitiesOutput }</c>.
    ///     ������Ϊtrue�ǣ��ڲ����г�ͻʱ������ζ��TimeStamp���Զ�ȡ�����˸��ģ�
    ///     ��������£����ǵ�ʵ�彫���ص���c��BulkConfig.TimeStampInfo��NumberOfSkippedForUpdate��EntitiesOutput����
    /// </summary>
    /// <remarks>
    ///     After reading skipped from EntitiesOutput, they can either be left skipped, or updated again, or thrown exception or rollback entire Update. (example Tests.EFCoreBulkTestAtypical.TimeStampTest)
    ///     ��EntitiesOutput��ȡ���������ǿ��Ա������������ٴθ��£����������쳣��ع��������¡�������ʾ����EFCoreBulkTest�ǵ��͡�ʱ������ԣ�
    /// </remarks>
    public bool DoNotUpdateIfTimeStampChanged { get; set; }

    /// <summary>
    ///     Default is zero '0'. When set to larger value it appends: LIMIT 'N', to generated query
    ///     Ĭ��ֵ��0��������Ϊ�����ֵʱ�����������ɵĲ�ѯ�и��ӣ�LIMIT'N'
    /// </summary>
    /// <remarks>
    ///     Used only with PostgreSql.
    ///     ������PostgreSql���ݿ�
    /// </remarks>
    public int ApplySubqueryLimit { get; set; } = 0;

    /// <summary>
    ///     Spatial Reference Identifier - for SQL Server with NetTopologySuite. Default value is <c>4326</c>.
    ///     �ռ����ñ�ʶ��-�����ڴ���NetTopologySuite��SQL Server��Ĭ��ֵΪ4326
    /// </summary>
    /// <remarks>
    ///     More info: <c>https://docs.microsoft.com/en-us/sql/relational-databases/spatial/spatial-reference-identifiers-srids</c>
    /// </remarks>
    public int SRID { get; set; } = 4326;

    /// <summary>
    ///     When type dbtype datetime2 has precision less then default 7, 
    ///     for example 'datetime2(3)' SqlBulkCopy does Floor instead of Round so Rounding done in memory to make sure inserted values are same as with regular SaveChanges.
    ///     ������dbtype datetime2�ľ���С��Ĭ��ֵ7ʱ�����确datetime2(3)����SqlBulkCopyʹ��Floor����Round��
    ///     ������ڴ���ִ��Rounding��ȷ�������ֵ�볣��SaveChanges��ͬ��
    /// </summary>
    /// <remarks>
    ///     Only for SqlServer.
    ///     ������sqlserver���ݿ�
    /// </remarks>
    public bool DateTime2PrecisionForceRound { get; set; }

    /// <summary>
    ///     When using BulkSaveChanges with multiply entries that have FK relationship which is Db generated, this set proper value after reading parent PK from Db.
    ///     IF PK are generated in memory like are some Guid then this can be set to false for better efficiency.
    ///    ��ʹ��BulkSaveChanges������������ݿ����������ʵ��ʱ����DB��ȡ��������������ȷ��ֵ
    ///    ����������ڴ������ɣ�����guid���������ó�false�Ի�ø��õ�Ч��
    /// </summary>
    /// <remarks>
    ///     Only used with BulkSaveChanges.
    ///     ����BulkSaveChangesһ��ʹ�á�
    /// </remarks>
    public bool OnSaveChangesSetFK { get; set; } = true;

    /// <summary>
    ///     When set to True it ignores GlobalQueryFilters if they exist on the DbSet.
    ///     �������ΪTrue�����������ݿ⼯�д��ڵ�GlobalQueryFilters��
    /// </summary>
    public bool IgnoreGlobalQueryFilters { get; set; }

    /// <summary>
    ///     When set to <c>true</c> result of BulkRead operation will be provided using replace instead of update. Entities list parameter of BulkRead method will be repopulated with obtained data.
    ///     Enables functionality of Contains/IN which will return all entities matching the criteria and only return the first (does not have to be by unique columns).
    ///     ������Ϊtrueʱ��BulkRead�����Ľ����ʹ���滻�����Ǹ������ṩ��BulkRead������ʵ���б�������û�õ�����������䡣
    ///     ����Contains/IN�Ĺ��ܣ��ù��ܽ��������з���������ʵ�壬����ֻ���ص�һ��ʵ�壨���ذ�Ψһ�У���
    /// </summary>
    public bool ReplaceReadEntities { get; set; }

    /// <summary>
    ///     Enum with [Flags] attribute which enables specifying one or more options.
    ///     ����[Flags]���Ե�ö�٣�����������ָ��һ������ѡ�
    /// </summary>
    /// <value>
    ///     <c>Default, KeepIdentity, CheckConstraints, TableLock, KeepNulls, FireTriggers, UseInternalTransaction</c>
    /// </value>
    public Microsoft.Data.SqlClient.SqlBulkCopyOptions SqlBulkCopyOptions { get; set; } // is superset of System.Data.SqlClient.SqlBulkCopyOptions, gets converted to the desired type

    /// <summary>
    ///     List of column order hints for improving performance.
    ///     Ϊ������ʾ��ʵ����˳����ʾ
    /// </summary>
    public List<SqlBulkCopyColumnOrderHint>? SqlBulkCopyColumnOrderHints { get; set; }

    /// <summary>
    ///     A filter on entities to delete when using BulkInsertOrUpdateOrDelete.
    ///     ʹ��BulkInsertOrUpdateOrDeleteʱɾ��ʵ��Ĺ�����
    /// </summary>
    public void SetSynchronizeFilter<T>(Expression<Func<T, bool>> filter) where T : class
    {
        SynchronizeFilter = filter;
    }
    /// <summary>
    ///     Clears SynchronizeFilter
    /// </summary>
    public void ClearSynchronizeFilter()
    {
        SynchronizeFilter = null;
    }

    /// <summary>
    ///     A filter on entities to delete when using BulkInsertOrUpdateOrDelete.
    ///     ʹ��BulkInsertOrUpdateOrDeleteʱҪɾ����ʵ���ϵ�ɸѡ����
    /// </summary>
    public void SetSynchronizeSoftDelete<T>(Expression<Func<T, T>> softDelete) where T : class
    {
        SynchronizeSoftDelete = softDelete;
    }
    /// <summary>
    ///     Clear SoftDelete
    /// </summary>
    public void ClearSoftDelete()
    {
        SynchronizeSoftDelete = null;
    }

    /// <summary>
    /// A func to set the underlying DB connection.
    /// �������û������ݿ����ӵĺ�����
    /// </summary>
    public Func<DbConnection, DbConnection>? UnderlyingConnection { get; set; }

    /// <summary>
    /// A func to set the underlying DB transaction.
    /// �������û���DB����ĺ�����
    /// </summary>
    public Func<DbTransaction, DbTransaction>? UnderlyingTransaction { get; set; }

    internal OperationType OperationType { get; set; }

    internal object? SynchronizeFilter { get; private set; }

    internal object? SynchronizeSoftDelete { get; private set; }
}

/// <summary>
/// Class to provide information about how many records have been updated, deleted and inserted.
/// �࣬���ṩ�й��Ѹ��¡�ɾ���Ͳ���ļ�¼������Ϣ��
/// </summary>
public class StatsInfo
{
    /// <summary>
    /// Indicates the number of inserted records.
    /// ָʾ����ļ�¼����
    /// </summary>
    public int StatsNumberInserted { get; set; }

    /// <summary>
    /// Indicates the number of updated records.
    /// ָʾ���µļ�¼����
    /// </summary>
    public int StatsNumberUpdated { get; set; }

    /// <summary>
    /// Indicates the number of deleted records.
    /// ָʾ��ɾ����¼��������
    /// </summary>
    public int StatsNumberDeleted { get; set; }
}

/// <summary>
/// Provides information about entities.
/// �ṩ�й�ʵ�����Ϣ��
/// </summary>
public class TimeStampInfo
{
    /// <summary>
    /// Indicates the number of entities skipped for an update.
    /// ָʾ�������µ�ʵ������
    /// </summary>
    public int NumberOfSkippedForUpdate { get; set; }

    /// <summary>
    /// Output the entities.
    /// ���ʵ�塣
    /// </summary>
    public List<object> EntitiesOutput { get; set; } = null!;
}
