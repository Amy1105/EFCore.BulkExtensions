using EFCore.BulkExtensions.SqlAdapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Xunit;
using static Azure.Core.HttpHeader;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EFCore.BulkExtensions.Tests;

public class EFCoreBulkTest
{
    protected static int EntitiesNumber => 10000;
    
    private static readonly Func<TestContext, int> ItemsCountQuery = EF.CompileQuery<TestContext, int>(ctx => ctx.Items.Count());
    private static readonly Func<TestContext, Item?> LastItemQuery = EF.CompileQuery<TestContext, Item?>(ctx => ctx.Items.LastOrDefault());
    private static readonly Func<TestContext, IEnumerable<Item>> AllItemsQuery = EF.CompileQuery<TestContext, IEnumerable<Item>>(ctx => ctx.Items.AsNoTracking());

    [Theory]
    [InlineData(SqlType.SqlServer)]
    public void insertorupdateordeleteDemo(SqlType sqlType)
    {
        ContextUtil.DatabaseType = sqlType;
        using var context = new TestContext(ContextUtil.GetOptions());
        var count = context.Courses.Count();
        List<Course> Courses = new List<Course>();
        foreach (var i in Enumerable.Range(1, 10))
        {
            var emodel = new Course
            {
                Title = "bulkLiterature" + i.ToString(),
                Credits = i,
            };
            //这门课
            Courses.Add(emodel);
        }
        var bulkConfig = new BulkConfig() { SetOutputIdentity = true }; //从数据库中返回id
        context.BulkInsert(Courses, bulkConfig);
        List<Course> newCourses = context.Courses.ToList();
        foreach (var c in newCourses.Take(5))
        {
            c.Title = "update new bulkLiterature";
        }
        foreach (var i in Enumerable.Range(1, 6))
        {
            //这门课
            newCourses.Add(new Course
            {
                Title = "add new bulkLiterature" + i.ToString(),
                Credits = 4,
            });
        }
        var bulkConfigSoftDel = new BulkConfig();
        bulkConfigSoftDel.SetOutputIdentity = true;
        bulkConfigSoftDel.CalculateStats = true;
        bulkConfigSoftDel.SetSynchronizeSoftDelete<Course>(a => new Course {Credits=0 }); // 它没有从数据库中删除，而是将Quantity更新为0（通常的用例是：IsDeleted为True）
        context.BulkInsertOrUpdateOrDelete(newCourses, bulkConfigSoftDel);
        Console.WriteLine($"after insert bulk 课程:{context.Courses.Count()}条");
        //查询
        var list = context.Courses.ToList();//.Take(10);
        foreach (var cource in list)
        {
            Console.WriteLine($"课程:{cource.CourseID},{cource.Title}");
        }
        Console.WriteLine(bulkConfigSoftDel.StatsInfo?.StatsNumberInserted);
        Console.WriteLine(bulkConfigSoftDel.StatsInfo?.StatsNumberUpdated);
        Console.WriteLine(bulkConfigSoftDel.StatsInfo?.StatsNumberDeleted);

    }

        [Theory]
    [InlineData(SqlType.SqlServer)]
    public void WeiYi(SqlType sqlType)
    {
        ContextUtil.DatabaseType = sqlType;
        using var context = new TestContext(ContextUtil.GetOptions());
        var entities = new List<Course>();
        for (int i = 1; i < 10; i++)
        {
            var emodel = new Course
            {
                Title = "bulkLiterature"+i,
                Credits = 5,
            };
            entities.Add(emodel);
        }
        context.BulkInsert(entities);
        var dbEntities = context.Courses.AsNoTracking().ToList();
        foreach (var entity in dbEntities)
        {
            Console.WriteLine($"Course:{entity.CourseID},Title:{entity.Title}");
        }
        var updateEntities = new List<Course>();
        for (int i = 1; i < 6; i++)
        {
            var emodel = new Course
            {
                Title = "bulkLiterature" + i,
                Credits = 4,
            };
            entities.Add(emodel);
        }
        context.BulkInsertOrUpdate(updateEntities,
            new BulkConfig
            {
                UpdateByProperties = new List<string> { nameof(Course.Title) }
            }
        );
        var dbEntities2 = context.Courses.AsNoTracking().ToList();
        foreach (var entity in dbEntities2)
        {
            Console.WriteLine($"Course:{entity.CourseID},Title:{entity.Title}");
        }
    }

    [Theory]
    [InlineData(SqlType.SqlServer)]
    public void InsertCourseTables(SqlType sqlType)
    {
        ContextUtil.DatabaseType = sqlType;
        using var context = new TestContext(ContextUtil.GetOptions());

        Console.WriteLine($"before insert bulk 课程:{context.Courses.Count()}条");
        List<Course> Courses = new List<Course>();
        List<Instructor> subEntities = new List<Instructor>();
        foreach (var i in Enumerable.Range(1, 5))
        {
            //这门课下的3个老师
            List<Instructor> instructors = new List<Instructor>();
            foreach (var j in Enumerable.Range(1, 3))
            {
                instructors.Add(new Instructor
                {
                    FirstMidName = "bulkKim" + i.ToString() + "-" + j.ToString(),
                    LastName = "Abercrombie" + i.ToString() + "-" + j.ToString(),
                    HireDate = DateTime.Parse("1995-03-11"),
                });
            }
            var emodel = new Course
            {
                Title = "bulkLiterature" + i.ToString(),
                Credits = 5,
            };
            emodel.Instructors = instructors;
            //这门课
            Courses.Add(emodel);
        }
        var bulkConfig = new BulkConfig() { SetOutputIdentity = true }; //从数据库中返回id
        context.BulkInsert(Courses, bulkConfig);
        foreach (var entity in Courses)
        {
            foreach (var subEntity in entity.Instructors!)
            {
                subEntity.CourseID = entity.CourseID; // 设置外键
            }
            subEntities.AddRange(entity.Instructors);
        }
        bulkConfig.SetOutputIdentity = false;
        context.BulkInsert(subEntities, bulkConfig);

        Console.WriteLine($"after insert bulk 课程:{context.Courses.Count()}条");
        //查询
        var courses = context.Courses.Include(x => x.Instructors);//.Take(10);
        foreach (var cource in courses)
        {
            Console.WriteLine($"课程:{cource.CourseID},{cource.Title}");
            foreach (var instructor in cource.Instructors!)
            {
                Console.WriteLine($"----教师 :{instructor.ID}-{instructor.FullName}");
            }
        }
        var oldCoures = context.Courses.ToList();
        List<Course> newCourses = new List<Course>();
        foreach (var i in Enumerable.Range(1, 5))
        {
            //这门课
            newCourses.Add(new Course
            {
                Title = "add new bulkLiterature" + i.ToString(),
                Credits = 4,
            });
        }
        oldCoures.AddRange(newCourses);
        var configUpdateBy = new BulkConfig
        {
            SetOutputIdentity = true,
            CalculateStats=true,
        };
        context.BulkInsertOrUpdate(courses, configUpdateBy);
        Assert.Equal(EntitiesNumber - 1, configUpdateBy.StatsInfo?.StatsNumberInserted);
        Assert.Equal(0, configUpdateBy.StatsInfo?.StatsNumberUpdated);
        Assert.Equal(0, configUpdateBy.StatsInfo?.StatsNumberDeleted);
        foreach (var cource in oldCoures)
        {
            Console.WriteLine($"课程:{cource.CourseID},{cource.Title}");
            foreach (var instructor in cource.Instructors!)
            {
                Console.WriteLine($"----教师 :{instructor.ID}-{instructor.FullName}");
            }
        }
    }




    [Theory]
    [InlineData(SqlType.Sqlite)]
    public void InsertTables(SqlType sqlType)
    {
        ContextUtil.DatabaseType = sqlType;
        using var context = new TestContext(ContextUtil.GetOptions());
        List<Order> orderInfos = new List<Order>();
        foreach (var i in Enumerable.Range(1, 50000))
        {
            Order orderInfo = new Order();
            var image = new ImageTB() { MainImage = "www.baidu.com" + i.ToString(), Images = new List<string>() { "www.baidu.com1", "www.baidu.com2", "www.baidu.com3" } };
            var product = new Product() { ProductName = "凉席" + i.ToString(), Description = "2024夏季新款", Price = 19.9M, Count = 3000, Images = image };
            var address = new AddressTB() { Name = "小明" + i.ToString(), Phone = "15996478657", City = "南京", Province = "江苏省", District = "雨花区", Street = "郁金香16号", PostalCode = "21000" };
            var orderdetail = new OrderDetail() { product = product, ProductID = product.Id, Count = 1, Price = 16.98M, Description = "时尚单品", ProductName = "凉席" + i.ToString(), Amount = 16.98M, order = orderInfo };
            orderInfo = new Order()
            {
                CreateTime = DateTime.Now,
                IsPay = true,
                PayTime = DateTime.Now,
                Address = address,
                AddressID = address.Id,
                OrderDetails = new List<OrderDetail>() { orderdetail }
            };
            orderInfos.Add(orderInfo);
        }
        context.BulkInsert(orderInfos);
    }

    [Theory]
    [InlineData(SqlType.Sqlite)]
    public void InsertEnumStringValue(SqlType sqlType)
    {
        ContextUtil.DatabaseType = sqlType;
        using var context = new TestContext(ContextUtil.GetOptions());
        context.Database.ExecuteSqlRaw($@"DELETE FROM ""{nameof(Wall)}""");
        context.Database.ExecuteSqlRaw($@"DELETE FROM ""{nameof(TimeRecord)}""");

        var walls = new List<Wall>();
        for (int i = 1; i <= 10; i++)
        {
            walls.Add(new Wall
            {
                Id = i,
                WallTypeValue = WallType.Brick,
                WallCategory = WallCategory.High,
            });
        }

        //context.Walls.AddRange(walls);
        //context.SaveChanges();
        // INSERT
        context.BulkInsert(walls);

        var addedWall = context.Walls.AsNoTracking().First(x => x.Id == walls[0].Id);
         
        Assert.True(addedWall.WallTypeValue == walls[0].WallTypeValue);


        var timeRecord = new TimeRecord()
        {
            Source = new TimeRecordSource
            {
                Name = "Abcd",
                Type = TimeRecordSourceType.Operator // for PG required Converter explicitly configured in OnModelCreating
                                                     // 对于在OnModelCreating中显式配置的PG所需转换器
            },
        };

        context.BulkInsert(new List<TimeRecord> { timeRecord });
    }

    [Theory]
    [InlineData(SqlType.Sqlite)]
    public void InsertTestPostgreSql(SqlType sqlType)
    {
        ContextUtil.DatabaseType = sqlType;

        using var context = new TestContext(ContextUtil.GetOptions());
        
        context.Database.ExecuteSqlRaw($@"DELETE FROM ""{nameof(Item)}""");
        //context.Database.ExecuteSqlRaw($@"ALTER SEQUENCE ""{nameof(Item)}_{nameof(Item.ItemId)}_seq"" RESTART WITH 1");

        context.Database.ExecuteSqlRaw($@"DELETE FROM ""{nameof(ItemHistory)}""");

        context.Database.ExecuteSqlRaw($@"DELETE FROM ""{nameof(Box)}""");
        //context.Database.ExecuteSqlRaw($@"ALTER SEQUENCE ""{nameof(Box)}_{nameof(Box.BoxId)}_seq"" RESTART WITH 1");

        context.Database.ExecuteSqlRaw($@"DELETE FROM ""{nameof(UserRole)}""");
        
        var currentTime = DateTime.UtcNow; // default DateTime type: "timestamp with time zone"; DateTime.Now goes with: "timestamp without time zone"

        var entities = new List<Item>();
        for (int i = 1; i <= 2; i++)
        {
            var entity = new Item
            {
                //ItemId = i,
                Name = "Name " + i,
                Description = "info " + i,
                Quantity = i,
                Price = 0.1m * i,
                TimeUpdated = currentTime,
            };
            entities.Add(entity);
        }
        
        var entities2 = new List<Item>();
        for (int i = 3; i <= 4; i++)
        {
            var entity = new Item
            {
                //ItemId = i,
                Name = "Name " + i,
                Description = "UPDATE " + i,
                Quantity = i,
                Price = 0.1m * i,
                TimeUpdated = currentTime,
            };
            entities2.Add(entity);
        }

        var entities3 = new List<Item>();
        for (int i = 4; i <= 4; i++)
        {
            var entity = new Item
            {
                //ItemId = i,
                Name = "Name " + i,
                Description = "CHANGE " + i,
                Quantity = i,
                Price = 0.1m * i,
                TimeUpdated = currentTime,
            };
            entities3.Add(entity);
        }
        
        var entities56 = new List<Item>();
        for (int i = 5; i <= 6; i++)
        {
            var entity = new Item
            {
                //ItemId = i,
                Name = "Name " + i,
                Description = "CHANGE " + i,
                Quantity = i,
                Price = 0.1m * i,
                TimeUpdated = currentTime,
            };
            entities56.Add(entity);
        }
        
        var entities78 = new List<Item>();
        for (int i = 7; i <= 8; i++)
        {
            var entity = new Item
            {
                //ItemId = i,
                Name = "Name " + i,
                Description = "CHANGE " + i,
                Quantity = i,
                Price = 0.1m * i,
                TimeUpdated = currentTime,
            };
            entities78.Add(entity);
        }

        // INSERT
        context.BulkInsert(entities);

        Assert.Equal("info 1", context.Items.Where(a => a.Name == "Name 1").AsNoTracking().FirstOrDefault()?.Description);
        Assert.Equal("info 2", context.Items.Where(a => a.Name == "Name 2").AsNoTracking().FirstOrDefault()?.Description);

        // UPDATE
        var config = new BulkConfig
        {
            UpdateByProperties = new List<string> { nameof(Item.Name) },//根据唯一索引去判断
            NotifyAfter = 1,
            SetOutputIdentity = true,  //pk不用填写，自增
            CalculateStats = true, //
        };
        //context.BulkInsertOrUpdate(entities2, config, (a) => WriteProgress(a));
        context.BulkInsert(entities2, config, (a) => WriteProgress(a));
        Assert.Equal("UPDATE 3", context.Items.Where(a => a.Name == "Name 3").AsNoTracking().FirstOrDefault()?.Description);
        Assert.Equal("UPDATE 4", context.Items.Where(a => a.Name == "Name 4").AsNoTracking().FirstOrDefault()?.Description);

        var configUpdateBy = new BulkConfig {
            SetOutputIdentity = true,
            UpdateByProperties = new List<string> { nameof(Item.Name) },
           // PropertiesToInclude = new List<string> {nameof(Item.ItemId), nameof(Item.Name), nameof(Item.Description) }, // "Name" in list not necessary since is in UpdateBy
        };
#pragma warning disable 0618
        context.Items.BatchUpdate(x=> new Item { Price = x.Price + 100 });
#pragma warning restore 0618

        //查看修改的结果
        var s=context.Items.ToList();
           // READ
           var secondEntity = new List<Item>() { new Item { Quantity = entities[1].Quantity } };
        context.BulkRead(secondEntity);  //, configUpdateBy
        Assert.Equal(2, secondEntity.FirstOrDefault()?.ItemId);
        Assert.Equal("info 2", secondEntity.FirstOrDefault()?.Description);
        // Test Multiple KEYS
        var userRoles = new List<UserRole> { new UserRole { Description = "Info" } };
        context.BulkInsertOrUpdate(userRoles);
        // DELETE
        context.BulkDelete(new List<Item>() { entities2[0] }, configUpdateBy);      
        // SAVE CHANGES
        context.AddRange(entities56);
        context.BulkSaveChanges();
        Assert.Equal(5, entities56[0].ItemId);

        // Test PropIncludeOnUpdate (supported with: 'applySubqueryLimit')
        var bulkConfig = new BulkConfig
        {
            UpdateByProperties = new List<string> { nameof(Item.Name) },
            PropertiesToIncludeOnUpdate = new List<string> { "" },
            SetOutputIdentity = true
        };
        context.BulkInsertOrUpdate(entities78, bulkConfig);

        context.BulkInsert(new List<ItemHistory> { new ItemHistory { ItemHistoryId = Guid.NewGuid(), Remark = "Rx", ItemId = 1 } });

        // BATCH
        var query = context.Items.AsQueryable().Where(a => a.ItemId <= 1);
#pragma warning disable
        query.BatchUpdate(new Item { Description = "UPDATE N", Price = 1.5m }); //, updateColumns);
#pragma warning disable
        var ids = new[] { Guid.Empty };
        context.ItemHistories.Where(o => ids.Contains(o.ItemHistoryId)).BatchDelete();

        var queryJoin = context.ItemHistories.Where(p => p.Item.Description == "UPDATE 2");
        queryJoin.BatchUpdate(new ItemHistory { Remark = "Rx", });

        var query2 = context.Items.AsQueryable().Where(a => a.ItemId > 1 && a.ItemId < 3);
        query.BatchDelete();

        var quants = new[] { 1, 2, 3 };
        int qu = 5;
        query.Where(a => quants.Contains(a.Quantity)).BatchUpdate(o => new Item { Quantity = qu });

        var descriptionsToDelete = new List<string> { "info" };
        var query3 = context.Items.Where(a => descriptionsToDelete.Contains(a.Description ?? ""));
        query3.BatchDelete();

        // for type 'jsonb'
        JsonDocument jsonbDoc = JsonDocument.Parse(@"{ ""ModelEL"" : ""Square""}");
        var box = new Box { DocumentContent = jsonbDoc, ElementContent = jsonbDoc.RootElement };
        context.BulkInsert(new List<Box> { box });

        JsonDocument jsonbDoc2 = JsonDocument.Parse(@"{ ""ModelEL"" : ""Circle""}");
        var boxQuery = context.Boxes.AsQueryable().Where(a => a.BoxId <= 1);
        boxQuery.BatchUpdate(new Box { DocumentContent = jsonbDoc2, ElementContent = jsonbDoc2.RootElement });


        var graphQLModels = new List<GraphQLModel>();
        for (int i = 1; i <= 2; i++)
        {
            var graphQLModel = new GraphQLModel
            {
                //ItemId = i,
                Name = "Name " + i,
            };
            graphQLModels.Add(graphQLModel);
        }

        var cnfg = new BulkConfig() { PropertiesToExclude = new List<string> { nameof(GraphQLModel.Id) } };
        context.BulkInsert(graphQLModels, cnfg);

        //var incrementStep = 100;
        //var suffix = " Concatenated";
        //query.BatchUpdate(a => new Item { Name = a.Name + suffix, Quantity = a.Quantity + incrementStep }); // example of BatchUpdate Increment/Decrement value in variable
    }
    
    [Theory]
    [InlineData(SqlType.MySql)]
    // -- Before first run following command should be executed on mysql server:
    //    SET GLOBAL local_infile = true;
    // -- otherwise exception: "Loading local data is disabled; this must be enabled on both the client and server sides"
    // -- For client side connection string is already set with: "AllowLoadLocalInfile=true"
    public void InsertTestMySql(SqlType sqlType)
    {
        ContextUtil.DatabaseType = sqlType;

        using var context = new TestContext(ContextUtil.GetOptions());

        var currentTime = DateTime.UtcNow; // default DateTime type: "timestamp with time zone"; DateTime.Now goes with: "timestamp without time zone"

        context.Items.RemoveRange(context.Items.ToList());
        context.SaveChanges();
        context.Database.ExecuteSqlRaw("ALTER TABLE " + nameof(Item) + " AUTO_INCREMENT = 1");
        context.SaveChanges();

        var entities1 = new List<Item>();
        for (int i = 1; i <= 10; i++)
        {
            var entity = new Item
            {
                ItemId = i,
                Name = "Name " + i,
                Description = "info " + i,
                Quantity = i,
                Price = 0.1m * i,
                TimeUpdated = currentTime,
            };
            entities1.Add(entity);
        }

        var entities2 = new List<Item>();
        
        for (int i = 6; i <= 15; i++)
        {
            var entity = new Item
            {
                ItemId = i,
                Name = "Name " + i,
                Description = "v2 info " + i,
                Quantity = i,
                Price = 0.1m * i,
                TimeUpdated = currentTime,
            };
            entities2.Add(entity);
        }
        var entities3 = new List<Item>();
        var entities4 = new List<Item>();

        // INSERT

        context.BulkInsertOrUpdate(entities1, bc => bc.SetOutputIdentity = true);
        Assert.Equal(1, entities1[0].ItemId);
        Assert.Equal("info 1", context.Items.Where(a => a.Name == "Name 1").AsNoTracking().FirstOrDefault()?.Description);
        Assert.Equal("info 2", context.Items.Where(a => a.Name == "Name 2").AsNoTracking().FirstOrDefault()?.Description);

        var query = context.Items.AsQueryable().Where(a => a.ItemId <= 1);
        query.BatchUpdate(new Item { Description = "UPDATE N", Price = 1.5m }); //, updateColumns);

        // INSERT Or UPDATE
        //mysql automatically detects unique or primary key mysql自动检测唯一密钥或主键
        //用于指定自定义属性，我们希望通过该属性进行更新。
        context.BulkInsertOrUpdate(entities2, new BulkConfig { UpdateByProperties  = new List<string> { nameof(Item.ItemId) } });
        Assert.Equal("info 5", context.Items.Where(a => a.Name == "Name 5").AsNoTracking().FirstOrDefault()?.Description);
        Assert.Equal("v2 info 6", context.Items.Where(a => a.Name == "Name 6").AsNoTracking().FirstOrDefault()?.Description);
        Assert.Equal("v2 info 15", context.Items.Where(a => a.Name == "Name 15").AsNoTracking().FirstOrDefault()?.Description);
        
        entities3.AddRange(context.Items.Where(a => a.ItemId <= 2).AsNoTracking());
        foreach (var entity in entities3)
        {
            entity.Description = "UPDATED";
        }
        context.BulkUpdate(entities3);
        Assert.Equal("UPDATED", context.Items.Where(a => a.Name == "Name 1").AsNoTracking().FirstOrDefault()?.Description);

        // TODO Custom UpdateBy column not working
        entities4.AddRange(context.Items.Where(a => a.ItemId >= 3 && a.ItemId <= 4).AsNoTracking());
        foreach (var entity in entities4)
        {
            entity.ItemId = 0; // should be matched by Name
            entity.Description = "UPDATED 2";
        }
        var configUpdateBy = new BulkConfig { UpdateByProperties = new List<string> { nameof(Item.Name) } }; // SetOutputIdentity = true;
        context.BulkUpdate(entities4, configUpdateBy);
        Assert.Equal("UPDATED 2", context.Items.Where(a => a.Name == "Name 3").AsNoTracking().FirstOrDefault()?.Description);

        context.BulkDelete(new List<Item> { new Item { ItemId = 11 } });
        Assert.False(context.Items.Where(a => a.Name == "Name 11").AsNoTracking().Any());

        var entities5 = context.Items.Where(a => a.ItemId == 15).AsNoTracking().ToList();
        entities5[0].Description = "SaveCh upd";
        entities5.Add(new Item { ItemId = 16, Name = "Name 16", Description = "info 16" }); // when BulkSaveChanges with Upsert 'ItemId' has to be set(EX.My1), and with Insert only it skips one number, Id becomes 17 instead of 16
        context.AddRange(entities5);
        context.BulkSaveChanges();
        //Assert.Equal(16, entities5[1].ItemId); // TODO Check Id is 2 instead of 16
        Assert.Equal("info 16", context.Items.Where(a => a.Name == "Name 16").AsNoTracking().FirstOrDefault()?.Description);

        var entities6 = new List<Item>();
        for (int i = 16; i <= 17; i++)
        {
            var entity = new Item
            {
                ItemId = i,
                Name = "Name " + i,
                Description = "info " + i,
                Quantity = i,
                Price = 0.1m * i,
                TimeUpdated = currentTime,
            };
            entities6.Add(entity);
        }
        var bulkConfig = new BulkConfig
        {
            UpdateByProperties = new List<string> { nameof(Item.Name) },
            PropertiesToIncludeOnUpdate = new List<string> { "" },
            SetOutputIdentity = true
        };
        context.BulkInsertOrUpdate(entities6, bulkConfig);
    }
    
    [Theory]
    [InlineData(SqlType.SqlServer, true)]
    [InlineData(SqlType.Sqlite, true)]
    //[InlineData(DbServer.SqlServer, false)] // for speed comparison with Regular EF CUD operations
    public void OperationsTest(SqlType sqlType, bool isBulk)
    {
        ContextUtil.DatabaseType = sqlType;

        //DeletePreviousDatabase();
        new EFCoreBatchTest().RunDeleteAll(sqlType);

        RunInsert(isBulk);
        RunInsertOrUpdate(isBulk, sqlType);
        RunUpdate(isBulk, sqlType);

        RunRead();

        if (sqlType == SqlType.SqlServer)
        {
            RunInsertOrUpdateOrDelete(isBulk); // Not supported for Sqlite (has only UPSERT), instead use BulkRead, then split list into sublists and call separately Bulk methods for Insert, Update, Delete.
        }
        RunDelete(isBulk, sqlType);

        //CheckQueryCache();
    }

    [Theory]
    [InlineData(SqlType.SqlServer)]
    [InlineData(SqlType.Sqlite)]
    public void SideEffectsTest(SqlType sqlType)
    {
        BulkOperationShouldNotCloseOpenConnection(sqlType, context => context.BulkInsert(new[] { new Item() }));
        BulkOperationShouldNotCloseOpenConnection(sqlType, context => context.BulkUpdate(new[] { new Item() }));
    }

    private static void BulkOperationShouldNotCloseOpenConnection(SqlType sqlType, Action<TestContext> bulkOperation)
    {
        ContextUtil.DatabaseType = sqlType;
        using var context = new TestContext(ContextUtil.GetOptions());

        var sqlHelper = context.GetService<ISqlGenerationHelper>();
        context.Database.OpenConnection();

        try
        {
            // we use a temp table to verify whether the connection has been closed (and re-opened) inside BulkUpdate(Async)
            var columnName = sqlHelper.DelimitIdentifier("Id");
            var tableName = sqlHelper.DelimitIdentifier("#MyTempTable");
            var createTableSql = $" TABLE {tableName} ({columnName} INTEGER);";

            createTableSql = sqlType switch
            {
                SqlType.Sqlite => $"CREATE TEMPORARY {createTableSql}",
                SqlType.SqlServer => $"CREATE {createTableSql}",
                _ => throw new ArgumentException($"Unknown database type: '{sqlType}'.", nameof(sqlType)),
            };

            context.Database.ExecuteSqlRaw(createTableSql);

            bulkOperation(context);

            var sql = $"SELECT {columnName} FROM {tableName}";
            context.Database.ExecuteSqlRaw(sql);
        }
        catch (Exception)
        {
            // Table already exist
        }
        finally
        {
            context.Database.CloseConnection();
        }
    }

    private static void DeletePreviousDatabase()
    {
        using var context = new TestContext(ContextUtil.GetOptions());
        context.Database.EnsureDeleted();
    }

    private static void CheckQueryCache()
    {
        using var context = new TestContext(ContextUtil.GetOptions());
        var compiledQueryCache = ((MemoryCache)context.GetService<IMemoryCache>());

        Assert.Equal(0, compiledQueryCache.Count);
    }

    private static void WriteProgress(decimal percentage)
    {
        Debug.WriteLine(percentage);
    }

    private static void RunInsert(bool isBulk)
    {
        using var context = new TestContext(ContextUtil.GetOptions());
        var categores  = new List<ItemCategory> { new ItemCategory { Id = 1, Name = "Some 1" }, new ItemCategory { Id = 2, Name = "Some 2" } };
        var entities = new List<Item>();
        var subEntities = new List<ItemHistory>();
        for (int i = 1, j = -(EntitiesNumber - 1); i < EntitiesNumber; i++, j++)
        {
            var entity = new Item
            {
                ItemId = 0, //isBulk ? j : 0, // no longer used since order(Identity temporary filled with negative values from -N to -1) is set automaticaly with default config PreserveInsertOrder=TRUE
                Name = "name " + i,
                Description = string.Concat("info ", Guid.NewGuid().ToString().AsSpan(0, 3)),
                Quantity = i % 10,
                Price = i / (i % 5 + 1),
                TimeUpdated = DateTime.Now,
                ItemHistories = new List<ItemHistory>()
            };

            entity.Category = categores[i%categores.Count];

            var subEntity1 = new ItemHistory
            {
                ItemHistoryId = SeqGuid.Create(),
                Remark = $"some more info {i}.1"
            };
            var subEntity2 = new ItemHistory
            {
                ItemHistoryId = SeqGuid.Create(),
                Remark = $"some more info {i}.2"
            };
            entity.ItemHistories.Add(subEntity1);
            entity.ItemHistories.Add(subEntity2);

            entities.Add(entity);
        }

        if (isBulk)
        {
            context.BulkInsertOrUpdate(categores);
            if (ContextUtil.DatabaseType == SqlType.SqlServer)
            {
                using var transaction = context.Database.BeginTransaction();
                var bulkConfig = new BulkConfig
                {
                    //PreserveInsertOrder = true, // true is default
                    SetOutputIdentity = true,
                    BatchSize = 4000,
                    UseTempDB = true,
                    CalculateStats = true
                };
                context.BulkInsert(entities, bulkConfig, (a) => WriteProgress(a));
                Assert.Equal(EntitiesNumber - 1, bulkConfig.StatsInfo?.StatsNumberInserted);
                Assert.Equal(0, bulkConfig.StatsInfo?.StatsNumberUpdated);
                Assert.Equal(0, bulkConfig.StatsInfo?.StatsNumberDeleted);

                foreach (var entity in entities)
                {
                    foreach (var subEntity in entity.ItemHistories)
                    {
                        subEntity.ItemId = entity.ItemId; // setting FK to match its linked PK that was generated in DB
                    }
                    subEntities.AddRange(entity.ItemHistories);
                }
                context.BulkInsert(subEntities);

                transaction.Commit();
            }
            else if (ContextUtil.DatabaseType == SqlType.Sqlite)
            {
                using var transaction = context.Database.BeginTransaction();
                var bulkConfig = new BulkConfig() { SetOutputIdentity = true };
                context.BulkInsert(entities, bulkConfig);

                var courses = context.Items.Include(x => x.ItemHistories).Take(10);
                foreach (var cource in courses)
                {
                    Console.WriteLine($":{cource.Name},{cource.ItemId}");

                    foreach (var instructor in cource.ItemHistories!)
                    {
                        Console.WriteLine($"----教师 :{instructor.ItemId}-{instructor.ItemHistoryId}");
                    }
                }

                foreach (var entity in entities)
                {
                    foreach (var subEntity in entity.ItemHistories)
                    {
                        subEntity.ItemId = entity.ItemId; // setting FK to match its linked PK that was generated in DB
                    }
                    subEntities.AddRange(entity.ItemHistories);
                }
                bulkConfig.SetOutputIdentity = false;
                context.BulkInsert(subEntities, bulkConfig);

                transaction.Commit();
                var courses2 = context.Items.Include(x => x.ItemHistories).Take(10);
                foreach (var cource in courses2)
                {
                    Console.WriteLine($":{cource.Name},{cource.ItemId}");

                    foreach (var instructor in cource.ItemHistories!)
                    {
                        Console.WriteLine($"----教师 :{instructor.ItemId}-{instructor.ItemHistoryId}");
                    }
                }
            }
        }
        else
        {
            context.Items.AddRange(entities);
            context.SaveChanges();
        }

        // TEST
        int entitiesCount = context.Items.Count();
        Item? lastEntity = context.Items.OrderByDescending(a => a.ItemId).FirstOrDefault();

        Assert.Equal(EntitiesNumber - 1, entitiesCount);
        Assert.NotNull(lastEntity);
        Assert.Equal("name " + (EntitiesNumber - 1), lastEntity?.Name);
    }

    private static void RunInsertOrUpdate(bool isBulk, SqlType sqlType)
    {
        using var context = new TestContext(ContextUtil.GetOptions());

        var entities = new List<Item>();
        var dateTimeNow = DateTime.Now;
        for (int i = 2; i <= EntitiesNumber; i += 2)
        {
            entities.Add(new Item
            {
                ItemId = isBulk ? i : 0,
                Name = "name InsertOrUpdate " + i,
                Description = "info",
                Quantity = i + 100,
                Price = i / (i % 5 + 1),
                TimeUpdated = dateTimeNow
            });
        }
        if (isBulk)
        {
            var bulkConfig = new BulkConfig() {
                SetOutputIdentity = true,
                CalculateStats = true,
                SqlBulkCopyOptions = Microsoft.Data.SqlClient.SqlBulkCopyOptions.KeepIdentity
            };
            context.BulkInsertOrUpdate(entities, bulkConfig, (a) => WriteProgress(a));
            if (sqlType == SqlType.SqlServer)
            {
                Assert.Equal(1, bulkConfig.StatsInfo?.StatsNumberInserted);
                Assert.Equal(EntitiesNumber / 2 - 1, bulkConfig.StatsInfo?.StatsNumberUpdated);
                Assert.Equal(0, bulkConfig.StatsInfo?.StatsNumberDeleted);
            }
        }
        else
        {
            var lastEntity1 = entities[^1]; // Last
            context.Items.Add(lastEntity1);
            context.SaveChanges();
        }

        // TEST
        int entitiesCount = context.Items.Count();
        Item? lastEntity = context.Items.OrderByDescending(a => a.ItemId).FirstOrDefault();

        Assert.Equal(EntitiesNumber, entitiesCount);
        Assert.NotNull(lastEntity);
        Assert.Equal("name InsertOrUpdate " + EntitiesNumber, lastEntity?.Name);
    }

    private static void RunInsertOrUpdateOrDelete(bool isBulk)
    {
        using var context = new TestContext(ContextUtil.GetOptions());

        var entities = new List<Item>();
        var dateTimeNow = DateTime.Now;
        for (int i = 2; i <= EntitiesNumber; i += 2)
        {
            entities.Add(new Item
            {
                ItemId = i,
                Name = "name InsertOrUpdateOrDelete " + i,
                Description = "info",
                Quantity = i,
                Price = i / (i % 5 + 1),
                TimeUpdated = dateTimeNow
            });
        }

        int? keepEntityItemId = null;
        if (isBulk)
        {
            var bulkConfig = new BulkConfig() { SetOutputIdentity = true, CalculateStats = true };
            keepEntityItemId = 3;
            bulkConfig.SetSynchronizeFilter<Item>(e => e.ItemId != keepEntityItemId.Value);
            //可以使用名称bacause在这种情况下属性名称与列名相同
            // can use nameof bacause in this case property name is same as column name 
            bulkConfig.OnConflictUpdateWhereSql = (existing, inserted) => $"{inserted}.{nameof(Item.TimeUpdated)} > {existing}.{nameof(Item.TimeUpdated)}";

            context.BulkInsertOrUpdateOrDelete(entities, bulkConfig, (a) => WriteProgress(a));

            Assert.Equal(0, bulkConfig.StatsInfo?.StatsNumberInserted);
            Assert.Equal(EntitiesNumber / 2, bulkConfig.StatsInfo?.StatsNumberUpdated);
            Assert.Equal(EntitiesNumber / 2 - 1, bulkConfig.StatsInfo?.StatsNumberDeleted);
        }
        else
        {
            var existingItems = context.Items;
            var removedItems = existingItems.Where(x => !entities.Any(y => y.ItemId == x.ItemId));
            context.Items.RemoveRange(removedItems);
            context.Items.AddRange(entities);
            context.SaveChanges();
        }

        // TEST
        int entitiesCount = context.Items.Count();
        Item? firstEntity = context.Items.OrderBy(a => a.ItemId).FirstOrDefault();
        Item? lastEntity = context.Items.OrderByDescending(a => a.ItemId).FirstOrDefault();

        Assert.Equal(EntitiesNumber / 2 + (keepEntityItemId != null ? 1 : 0), entitiesCount);
        Assert.NotNull(firstEntity);
        Assert.Equal("name InsertOrUpdateOrDelete 2", firstEntity?.Name);
        Assert.NotNull(lastEntity);
        Assert.Equal("name InsertOrUpdateOrDelete " + EntitiesNumber, lastEntity?.Name);

        var bulkConfigSoftDel = new BulkConfig();
        bulkConfigSoftDel.SetSynchronizeSoftDelete<Item>(a => new Item { Quantity = 0 }); // Instead of Deleting from DB it updates Quantity to 0 (usual usecase would be: IsDeleted to True)
        context.BulkInsertOrUpdateOrDelete(new List<Item> { entities[1] }, bulkConfigSoftDel);

        var list = context.Items.Take(2).ToList();
        Assert.True(list[0].Quantity != 0);
        Assert.True(list[1].Quantity == 0);

        // TEST Alias
        context.Entries.Add(new Entry { Name = "Entry_InsertOrUpdateOrDelete" });
        context.SaveChanges();

        int entriesCount = context.Entries.Count();
        
        bulkConfigSoftDel.SetSynchronizeSoftDelete<Entry>(a => new Entry { Name = "Entry_InsertOrUpdateOrDelete_Deleted" });
        context.BulkInsertOrUpdateOrDelete(new List<Entry> { new Entry { Name = "Entry_InsertOrUpdateOrDelete_2" } }, bulkConfigSoftDel);

        Assert.Equal(entriesCount + 1,  context.Entries.Count());
        Assert.True(context.Entries.Any(e => e.Name == "Entry_InsertOrUpdateOrDelete_Deleted"));
    }

    private static void RunUpdate(bool isBulk, SqlType sqlType)
    {
        using var context = new TestContext(ContextUtil.GetOptions());

        int counter = 1;
        var entities = context.Items.AsNoTracking().ToList();
        foreach (var entity in entities)
        {
            entity.Description = "Desc Update " + counter++;
            entity.Quantity += 1000; // will not be changed since Quantity property is not in config PropertiesToInclude
        }
        if (isBulk)
        {
            var bulkConfig = new BulkConfig
            {
                PropertiesToInclude = new List<string> { nameof(Item.Description) },
                UpdateByProperties = sqlType == SqlType.SqlServer ? new List<string> { nameof(Item.Name) } : null,
                CalculateStats = true
            };
            context.BulkUpdate(entities, bulkConfig);
            if (sqlType == SqlType.SqlServer)
            {
                Assert.Equal(0, bulkConfig.StatsInfo?.StatsNumberInserted);
                Assert.Equal(EntitiesNumber, bulkConfig.StatsInfo?.StatsNumberUpdated);
                Assert.Equal(0, bulkConfig.StatsInfo?.StatsNumberDeleted);
            }
        }
        else
        {
            context.Items.UpdateRange(entities);
            context.SaveChanges();
        }

        // TEST
        int entitiesCount = context.Items.Count();
        Item? lastEntity = context.Items.OrderByDescending(a => a.ItemId).FirstOrDefault();

        Assert.Equal(EntitiesNumber, entitiesCount);
        Assert.NotNull(lastEntity);
        Assert.Equal("name InsertOrUpdate " + EntitiesNumber, lastEntity?.Name);
    }

    private static void RunRead()
    {
        using var context = new TestContext(ContextUtil.GetOptions());

        var entities = new List<Item>();
        for (int i = 1; i < EntitiesNumber; i++)
        {
            var entity = new Item
            {
                Name = "name " + i,
            };
            entities.Add(entity);
        }

        context.BulkRead(
            entities,
            new BulkConfig
            {
                UpdateByProperties = new List<string> { nameof(Item.Name) }
            }
        );

        Assert.Equal(1, entities[0].ItemId);
        Assert.Equal(0, entities[1].ItemId);
        Assert.Equal(3, entities[2].ItemId);
        Assert.Equal(0, entities[3].ItemId);
    }

    private void RunDelete(bool isBulk, SqlType sqlType)
    {
        using var context = new TestContext(ContextUtil.GetOptions());

        var entities = context.Items.ToList();
        // ItemHistories will also be deleted because of Relationship - ItemId (Delete Rule: Cascade)
        if (isBulk)
        {
            var bulkConfig = new BulkConfig() { CalculateStats = true };
            context.BulkDelete(entities, bulkConfig);
            if (sqlType == SqlType.SqlServer)
            {
                Assert.Equal(0, bulkConfig.StatsInfo?.StatsNumberInserted);
                Assert.Equal(0, bulkConfig.StatsInfo?.StatsNumberUpdated);
                Assert.Equal(entities.Count, bulkConfig.StatsInfo?.StatsNumberDeleted);
            }
        }
        else
        {
            context.Items.RemoveRange(entities);
            context.SaveChanges();
        }

        // TEST
        int entitiesCount = context.Items.Count();
        Item? lastEntity = context.Items.OrderByDescending(a => a.ItemId).FirstOrDefault();

        Assert.Equal(0, entitiesCount);
        Assert.Null(lastEntity);

        // RESET AutoIncrement
        string deleteTableSql = sqlType switch
        {
            SqlType.SqlServer => $"DBCC CHECKIDENT('[dbo].[{nameof(Item)}]', RESEED, 0);",
            SqlType.Sqlite => $"DELETE FROM sqlite_sequence WHERE name = '{nameof(Item)}';",
            _ => throw new ArgumentException($"Unknown database type: '{sqlType}'.", nameof(sqlType)),
        };
        context.Database.ExecuteSqlRaw(deleteTableSql);
    }
}
