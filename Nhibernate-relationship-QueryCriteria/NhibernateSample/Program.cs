using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using HibernatingRhinos.Profiler.Appender.NHibernate;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Criterion;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Linq;
using NHibernate.Type;
namespace NhibernateSample
{
    class Program
    {
        static void Main(string[] args)
        {
            NHibernateProfiler.Initialize();

            var cfg = new Configuration();
            //load the config file  hibernate.cfg.xml
            //cfg.Configure();

            //loquacious (talkative) config introduced in 3.0
            // Customer.hbm.xml file should be included as Embedded Resource
            cfg.DataBaseIntegration(x =>
            {
                //x.ConnectionString = @"Server=localhost;Data Source =.\; Database=CRMDB;Integrated Security=SSPI";
                x.ConnectionStringName = "default"; //from App.config
                x.Driver<SqlClientDriver>();
                x.Dialect<MsSql2012Dialect>();
                x.LogSqlInConsole = false;
                x.IsolationLevel = IsolationLevel.RepeatableRead;
                //default is 20, how many insert/update to push in group to the db, supported only by Oracle and Sqlserver
                x.BatchSize = 200;
            });

            //use that for statistics along with nhibernate profiler
            cfg.SessionFactory().GenerateStatistics();

            //high level cashing, we could also do it in config file or in individual config files
            //cfg.SessionFactory().Caching;
            cfg.AddAssembly(Assembly.GetExecutingAssembly());
            //End loquacious (talkative) config introduced in 3.0

            //load the config file  hibernate.cfg.xml and overrides loquacious config
            //cfg.Configure();

            var sessionFactory = cfg.BuildSessionFactory();

            //Call demo methods here
            SessionDemo19(sessionFactory);

            Console.WriteLine($"Press any key to continue...");
            Console.ReadKey();
        }



        //      W A R N I N G
        //1- we have to use only Object chaining OR Method Syntax , the query comprehension syntax OR Query syntax is not allowed
        //2- //2- we can't mix  Linq style method chain syntax and Classic criteria syntax  
        //Linq style method chain syntax 
        private static void SessionDemo20(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                {
                    var query = session.QueryOver<Customer>()
                        .Where(x=> x.FirstName.StartsWith("J"));

                    Console.WriteLine("Press 'D' to query for Doe");
                    var read = Console.ReadLine();
                    if (read == "D")
                    {
                        //Adding dynamically queries : based on I/O's (files, UI, ...)
                        //query.Where(x => x.LastName== "Doe3");
                        
                        // OR more broadly feeding a restriction
                        query.Where(Restrictions.On<Customer>(c => c.FirstName).IsLike("J%"));
                    }

                    var customers = query.List<Customer>();
                    foreach (var customer in customers)
                    {
                        Console.WriteLine(customer);
                    }
                }
                tx.Commit();
            }
        }

        //      W A R N I N G
        //1-we could use only Object chaining OR Method Syntax , the query comprehension syntax OR Query syntax is not allowed
        //2- we can't mix  Linq style method chain syntax and Classic criteria syntax  
        //Classic criteria syntax => object (Restrictions) based API. 
        private static void SessionDemo19(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                {
                    var query = session.CreateCriteria<Customer>()
                        .Add(Restrictions.Like("FirstName", "J%"));

                    Console.WriteLine("Press 'D' to query for Doe");
                    var read = Console.ReadLine();
                    if (read == "D")
                    {
                        //Adding dynamically queries : based on I/O's (files, UI, ...)
                        query.Add(Restrictions.Eq("LastName", "Doe3"));
                    }

                    var customers = query.List<Customer>();
                    foreach (var customer in customers)
                    {
                        Console.WriteLine(customer);
                    }
                }
                tx.Commit();
            }
        }



        //HQL
        private static void SessionDemo18(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                {
                    var query = session.CreateQuery("select c from Customer c " +
                                                    "where c.FirstName like 'J%' and c.Orders.size >2 ");

                    //var query = session.CreateQuery("select c from Customer c " +
                    //                                "where c.FirstName like 'J%' and (size(c.Orders)>2 or c.Orders.size >2 ) ");
                    foreach (var customer in query.List<Customer>())
                    {
                        Console.WriteLine($"{customer.FirstName} {customer.LastName} ");
                    }
                }

                //same Query size differs
                {
                   var query = session.CreateQuery("select c from Customer c " +
                                                   " where c.FirstName like 'J%' and  c.Orders.size > 2  " +
                                                   " order by c.FirstName asc, c.LastName asc");
                    //HQL predates Generics we could use query.List() instead of query.List<Customer>() but then 
                    //we lose strong typing...
                    foreach (var customer in query.List<Customer>())
                    {
                        Console.WriteLine($"{customer.FirstName} {customer.LastName} ");
                    }
                }

                tx.Commit();
            }
        }
    
    private static void SessionDemo17(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                //W A R N I N G 
                //We do twice the projection Queries if we call it a 2nd time we need to iterate over customers = query.ToList() instead of
                //using the query which runs the same query again into the DB
                //Linq using query comprehension syntax OR Query syntax + PROJECTION AND ORDER BY 

                //W A R N I N G
                //adding 2nd linq query to combined to the 1st one and before sending the entire query to DB (query.ToList() same effect as query.AsEnumerable())
                //if we apply ToList() or AsEnumerable() (e.g.  var augmented = from c in query.ToList()), we will do the filtering in memory and not in the db 
                //AsQueryable() doesn't call the DB for query to be run.
                {
                    var query = from c in session.Query<Customer>()
                                where c.Orders.Count > 2
                                orderby c.FirstName, c.LastName
                                select new { c.FirstName, c.LastName, orderCount = c.Orders.Count };

                   
                    var augmented = from c in query
                                    where c.FirstName.StartsWith("J")
                                select c; 
                                var customers = augmented.ToList();
                    //foreach (var customerProjection in query.ToList()) <-- cause a run in DB each time
                    //doesn't cause a run in DB each time, only a run im memory
                    foreach (var customerProjection in customers)
                    {
                        Console.WriteLine($"{customerProjection.FirstName} {customerProjection.LastName} {customerProjection.orderCount}");
                    }

                    foreach (var customerProjection in customers)
                    {
                        Console.WriteLine($"{customerProjection.FirstName} {customerProjection.LastName} {customerProjection.orderCount}");
                    }
                }

                //'augmented' filter Applied In memory, .ToList() cause the run in DB
                {
                    var query = from c in session.Query<Customer>()
                        where c.Orders.Count > 2
                        orderby c.FirstName, c.LastName
                        select new { c.FirstName, c.LastName, orderCount = c.Orders.Count };

                    var customers = query.ToList();
                    var augmented = from c in customers
                                    where c.FirstName.StartsWith("J")
                        select c;
                    
                    //foreach (var customerProjection in query.ToList())
                    foreach (var customerProjection in augmented)
                    {
                        Console.WriteLine($"{customerProjection.FirstName} {customerProjection.LastName} {customerProjection.orderCount}");
                    }
                }

                // 'augmented' filter Applied In memory, .AsEnumerable() cause the run in DB
                {
                    var query = from c in session.Query<Customer>()
                        where c.Orders.Count > 2
                        orderby c.FirstName, c.LastName
                        select new { c.FirstName, c.LastName, orderCount = c.Orders.Count };

                    var customers = query.AsEnumerable();
                    var augmented = from c in customers
                        where c.FirstName.StartsWith("J")
                        select c;

                    //foreach (var customerProjection in query.ToList())
                    foreach (var customerProjection in augmented)
                    {
                        Console.WriteLine($"{customerProjection.FirstName} {customerProjection.LastName} {customerProjection.orderCount}");
                    }
                }

                //'augmented' filter Applied as comnination of query In DB , .AsQueryable() doesn't cause the run in DB
                {
                    var query = from c in session.Query<Customer>()
                        where c.Orders.Count > 2
                        orderby c.FirstName, c.LastName
                        select new { c.FirstName, c.LastName, orderCount = c.Orders.Count };

                    var customers = query.AsQueryable();
                    var augmented = from c in customers
                        where c.FirstName.StartsWith("J")
                        select c;

                    //foreach (var customerProjection in query.ToList())
                    foreach (var customerProjection in augmented)
                    {
                        Console.WriteLine($"{customerProjection.FirstName} {customerProjection.LastName} {customerProjection.orderCount}");
                    }
                }
            }
        }

        private static void SessionDemo16(ISessionFactory sessionFactory)
        {
            
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                //Object chaining OR Method Syntax in linq
                {
                    var query = session.Query<Customer>()
                        .Where(x => x.FirstName.StartsWith("J"));

                    foreach (var customer in query.ToList())
                    {
                        Console.WriteLine(customer.PrintShort());
                    }
                }


                //Linq using query comprehension syntax OR Query syntax
                {
                    var query = from customer in session.Query<Customer>()
                                where customer.FirstName.StartsWith("J")
                                select customer;

                    foreach (var customer in query.ToList())
                    {
                        Console.WriteLine(customer.PrintShort());
                    }
                }

                //Linq using query comprehension syntax OR Query syntax + COUNT
                {
                    var query = from customer in session.Query<Customer>()
                        where customer.Orders.Count > 2
                        select customer;

                    foreach (var customer in query.ToList())
                    {
                        Console.WriteLine(customer.PrintShort());
                    }
                }

                //W A R N I N G 
                //We do twice the projection Queries if we call it a 2nd time we need to iterate over customers = query.ToList() instead of
                //using the query which runs the same query again into the DB
                //Linq using query comprehension syntax OR Query syntax + PROJECTION AND ORDER BY 
                {
                    var query = from c in session.Query<Customer>()
                        where c.Orders.Count > 2
                        orderby c.FirstName, c.LastName
                      select new {c.FirstName, c.LastName, orderCount= c.Orders.Count};
                    var customers = query.ToList();
                    //foreach (var customerProjection in query.ToList())
                    foreach (var customerProjection in customers)
                    {
                        Console.WriteLine($"{customerProjection.FirstName} {customerProjection.LastName} {customerProjection.orderCount}");
                    }

                    foreach (var customerProjection in customers)
                    {
                        Console.WriteLine($"{customerProjection.FirstName} {customerProjection.LastName} {customerProjection.orderCount}");
                    }

                    foreach (var customerProjection in customers)
                    {
                        Console.WriteLine($"{customerProjection.FirstName} {customerProjection.LastName} {customerProjection.orderCount}");
                    }

                }

               //Linq using query comprehension syntax OR Query syntax + PROJECTION AND ORDER BY 
                //{
                //    var query = from c in session.Query<Customer>()
                //        where c.Orders.Count > 2
                //        orderby c.FirstName, c.LastName
                //        select new { c.FirstName, c.LastName, orderCount = c.Orders.Count };

                //    foreach (var customerProjection in query.ToList())
                //    {
                //        Console.WriteLine($"{customerProjection.FirstName} {customerProjection.LastName} {customerProjection.orderCount}");
                //    }
                //}

                /*
                 select customer0_.FirstName                         as col_0_0_,
                           customer0_.LastName                          as col_1_0_,
                           (select cast(count(*) as INT)
                            from   Orders orders1_
                            where  customer0_.Id = orders1_.CustomerId) as col_2_0_
                    from   Customers customer0_
                    where  (select cast(count(*) as INT)
                            from   Orders orders2_
                            where  customer0_.Id = orders2_.CustomerId) > 2 
                                   order by customer0_.FirstName asc,
                                   customer0_.LastName asc*/
                tx.Commit();
            }
        } 

        private static void SessionDemo15(ISessionFactory sessionFactory)
        {
            var goodId = Guid.Parse("65F76842-8A89-4590-9946-A9E501591AFB");
            var badId = Guid.Parse("E2D91B82-E832-4F27-99F9-A9E50159D213");

            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                //Saving an a child entity, with load we don't need to load all customer we get just the customerId + an insert VS get = select customer + Insert
                //var orderLoad = CreateOrder();
                //orderLoad.Customer = session.Load<Customer>(goodId);
                //session.Save(orderLoad);

                var orderGet = CreateOrder();
                orderGet.Customer = session.Get<Customer>(goodId);
                session.Save(orderGet);
                tx.Commit();
            }
        }

        private static void SessionDemo14(ISessionFactory sessionFactory)
        {
            var goodId = Guid.Parse("65F76842-8A89-4590-9946-A9E501591AFB");
            var badId = Guid.Parse("E2D91B82-E832-4F27-99F9-A9E50159D213");

            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {

                //We GET: NULL because customer been fetched right away
                var customerGET = session.Get<Customer>(goodId);
                Console.WriteLine(customerGET);
                try
                {
                    //Lazy loaded through proxies
                    var customerLoaded = session.Load<Customer>(badId);

                    //When we load we GET:
                    //NHibernate.ObjectNotFoundException: 'No row with the given identifier
                    //exists[NhibernateSample.Customer#e2d91b82-e832-4f27-99f9-a9e50159d213]'
                    Console.WriteLine(customerLoaded);
                }
                catch (NHibernate.ObjectNotFoundException e)
                {
                    Console.WriteLine(e);
                }
                tx.Commit();
            }
        }

        private static Order  CreateOrder()
        {
            return new Order()
            {
                OrderAt = DateTime.Now.AddDays(-2),
                ShipTo = CreateAddress(),
                ShippedAt = DateTime.UtcNow
            };
        }

        private static void SessionDemo13(ISessionFactory sessionFactory)
        {
            Guid id;
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var newCustomer = CreateCustomer();
                Console.WriteLine("New Customer");
                Console.WriteLine(newCustomer);
                session.Save(newCustomer);
                tx.Commit();
            }
        }



        private static void SessionDemo12(ISessionFactory sessionFactory)
        {

            //Using linq instead of load
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {

                var query = from customer in session.Query<Customer>()
                    select customer;
                Console.WriteLine("Linqed:");
                //fetch allows eager left outer join Orders otherwise N+1 trip PB.
                //var reloaded = query.Fetch(x => x.Orders).First();
                //N+1
                var leftOuterJoin = query.Fetch(x=>x.Orders).ToList(); //or var leftOuterJoin = query.Fetch(x => x.Orders);
                foreach (var c in leftOuterJoin) //because foreach forces the load into memory like ToList()
                {
                    Console.WriteLine(c);
                    foreach (var o in c.Orders)
                    {
                        Console.WriteLine(o);
                    }
                }
                tx.Commit();
            }
        }

        private static void SessionDemo11(ISessionFactory sessionFactory)
        {
           
            //Using linq instead of load
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
            
                var query = from customer in session.Query<Customer>()
                    select customer;
                Console.WriteLine("Linqed:");
                //fetch allows eager left outer join Orders otherwise N+1 trip PB.
                //var reloaded = query.Fetch(x => x.Orders).First();
                //N+1
                var NPlusOnePb = query.ToList();//all customer is eagerly loaded without orders
                foreach (var c in NPlusOnePb)
                {
                    Console.WriteLine(c);
                    foreach (var o in c.Orders)
                    {
                        Console.WriteLine(o);
                    }
                }
                tx.Commit();
            }
        }

        private static void SessionDemo10(ISessionFactory sessionFactory)
        {
            Guid id;
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var newCustomer = CreateCustomer();
                Console.WriteLine("New Customer");
                Console.WriteLine(newCustomer);
                session.Save(newCustomer);
                //<set name="Orders" table="Orders" cascade="all-delete-orphan"> handles the save/update/delete cascade
                //foreach (var order in newCustomer.Orders)
                //{
                //    session.Save(order);
                //}

                id = newCustomer.Id;
                tx.Commit();
            }

            //Using linq instead of load
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                //var reloaded = session.Load<Customer>(id);
                //Console.WriteLine("Reloaded:");
                //Console.WriteLine(reloaded);

                var query = from customer in session.Query<Customer>()
                             where customer.Id == id
                    select customer;
                Console.WriteLine("Linqed:");
                //fetch allows eager left outer join Orders otherwise N+1 trip PB.
                var reloaded = query.Fetch(x => x.Orders).First();
                Console.WriteLine(reloaded);

                // N+1 trip PB.
                var twoSeparateQuery = query.First();
                Console.WriteLine(twoSeparateQuery);

                tx.Commit();
            }
        }


        private static void SessionDemo9(ISessionFactory sessionFactory)
        {
            Guid id;
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var newCustomer = CreateCustomer();
                Console.WriteLine("New Customer");
                Console.WriteLine(newCustomer);
                session.Save(newCustomer);
                //<set name="Orders" table="Orders" cascade="all-delete-orphan"> handles the save/update/delete cascade
                //foreach (var order in newCustomer.Orders)
                //{
                //    session.Save(order);
                //}

                id = newCustomer.Id;
                tx.Commit();
            }

            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
               var reloaded = session.Load<Customer>(id);
                Console.WriteLine("Reloaded:");
                Console.WriteLine(reloaded);
                tx.Commit();
            }
        }

        private static void SessionDemo8(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var customers = session.QueryOver<Customer>().List();
                foreach (var customer in customers)
                {
                    Console.WriteLine(customer);
                }
                tx.Commit();

            }
        }

        //mapping datatype
        private static void SessionDemo7(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var customer = CreateCustomer();
                session.Save(customer);
                tx.Commit();
                Console.WriteLine(customer);

            }

        }

        private static Customer CreateCustomer()
        {
            var customer = 
             new Customer()
            {
                FirstName = "John3",
                LastName = "Doe3",
                //LastName = new string('D', 100), //give an exception 
                Points = 100,
                HasGoldStatus = true,
                //MemberSince = new DateTime(2016,1,1),
                CreditRating = CustomerCreditRating.Good,
                Address = CreateAddress(),
                //Street = "Freedom avenue , 3th ",
                //City = "Mons",
                //Province = "Wallonia",
                //Country = "Belgium"
            };

            customer.Orders = new HashSet<Order>()
            {
                new Order()
                {
                    OrderAt = DateTime.Now,
                    ShipTo = CreateAddress(),
                    ShippedAt = DateTime.UtcNow.AddDays(2),
                    Customer = customer
                },
                new Order()
                {
                    OrderAt = DateTime.Now.AddDays(-1),
                    ShipTo = CreateAddress(),
                    ShippedAt = DateTime.UtcNow.AddDays(1),
                    Customer = customer
                }
            };
            return customer;
        }

        private static Location CreateAddress()
        {
            return new Location()
            {
                Street = "Freedom avenue , 3th ",
                City = "Mons",
                Province = "Wallonia",
                Country = "Belgium"
            };
        }



        //DELETE
        private static void SessionDemo6(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var query = from c in session.Query<Customer>()
                            where c.FirstName == "Julien"
                            select c;

                var customer = query.FirstOrDefault();
                if (customer != null)
                {
                    session.Delete(customer);
                    tx.Commit();
                    Console.WriteLine($"Record deleted : {customer.FirstName} - {customer.LastName}");
                }
                else
                {
                    Console.WriteLine($"User not found !");
                }
            }

        }


        //Update
        //private static void SessionDemo5(ISessionFactory sessionFactory)
        //{
        //    using (var session = sessionFactory.OpenSession())
        //    using (var tx = session.BeginTransaction())
        //    {
        //        var query = from c in session.Query<Customer>()
        //                    where c.Id == 1
        //                    select c;

        //        var customer = query.FirstOrDefault();
        //        customer.FirstName = "Mohamed";
        //        session.Save(customer);
        //        tx.Commit();

        //        //extra trip to DB: just exploring what Nhibernate doing behind the scenes
        //        query = from c in session.Query<Customer>()
        //                where c.Id == 1
        //                select c;
        //        var customer2 = query.FirstOrDefault();
        //        Console.WriteLine($"{customer2.FirstName} - {customer2.LastName}");
        //    }

        //}


        //GET ID AFTER INSERT
        private static void SessionDemo4(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var customer = new Customer
                {
                    FirstName = "John",
                    LastName = "Doe"

                };
                Console.WriteLine($"{customer.FirstName} - {customer.LastName}");
                Console.WriteLine($"(Before save) Customer ID: {customer.Id} ");
                session.Save(customer);
                Console.WriteLine($"(Before commit) Customer ID: {customer.Id} ");
                tx.Commit();
                Console.WriteLine($"Customer ID: {customer.Id} ");
            }

        }
        //INSERT DATA
        private static void SessionDemo3(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var customer = new Customer
                {
                    FirstName = "Maximiliam",
                    LastName = "Cesar"
                };
                session.Save(customer);
                tx.Commit();
            }

            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var query = from customer in session.Query<Customer>()
                            where customer.FirstName == "Julien"
                            select customer;
                var retrieved = query.FirstOrDefault();
                Console.WriteLine($"{retrieved.FirstName}  {retrieved.LastName}");
                tx.Commit();
            }

        }

        //LINQ
        private static void SessionDemo2(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var customers = from customer in session.Query<Customer>()
                                where customer.LastName.Length > 3 && customer.LastName.StartsWith("k")
                                orderby customer.LastName
                                select customer;
                foreach (var customer in customers)
                {
                    Console.WriteLine($"{customer.FirstName}  {customer.LastName}");
                }
                tx.Commit();
            }
        }

        //CRITERIA
        private static void SessionDemo1(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var customers = session.CreateCriteria<Customer>()
                    .List<Customer>();
                foreach (var customer in customers)
                {
                    Console.WriteLine($"{customer.FirstName}  {customer.LastName}");
                }
                tx.Commit();

            }
        }
    }
    public class Customer
    {
        public Customer()
        {
            //an old issue with SQL server regarding min date started at year 1753 othewise Exception
            //MemberSince = new DateTime(2016, 1, 1);
            MemberSince = DateTime.UtcNow;
            AverageRating = 19.2623258;
        }

        public virtual Guid Id { get; set; }
        public virtual string FirstName { get; set; }
        public virtual string LastName { get; set; }
        public virtual double AverageRating { get; set; }
        public virtual int Points { get; set; }
        public virtual bool HasGoldStatus { get; set; }
        public virtual DateTime MemberSince { get; set; }
        public virtual CustomerCreditRating CreditRating { get; set; }
        //Location
        //public virtual string Street { get; set; }
        //public virtual string City { get; set; }
        //public virtual string Province { get; set; }
        //public virtual string Country { get; set; }
        public virtual Location Address { get; set; }
        public virtual ISet<Order> Orders { get; set; }

        public virtual void addOrder(Order order)
        {
            Orders.Add(order);
            order.Customer = this;
        }

        public virtual string PrintShort()
        {
            return $"\nid: {Id}, Firstname : {FirstName}, LastName: {LastName}, Points: {Points}, " +
                   $"HasGoldStatus:  {HasGoldStatus}, MemberSince: {MemberSince} ({MemberSince.Kind}), " +
                   $"CreditRating: {CreditRating}, AverageRating: {AverageRating}";
        }

        public override string ToString()
        {
            return $"\nid: {Id}, Firstname : {FirstName}, LastName: {LastName}, Points: {Points}, " +
                   $"HasGoldStatus:  {HasGoldStatus}, MemberSince: {MemberSince} ({MemberSince.Kind}), " +
                   $"CreditRating: {CreditRating}, AverageRating: {AverageRating} , \nOrders:  {Orders}";
        }
    }

    public class Location
    {
        public virtual string Street { get; set; }
        public virtual string City { get; set; }
        public virtual string Province { get; set; }
        public virtual string Country { get; set; }
        public override string ToString()
        {
            return $"\n\t\tStreet: {Street}, City : {City}, Province: {Province}, Country: {Country} " ;
        }


    }
    public enum CustomerCreditRating
    {
        //those are default values 
        //Adding VeryGood is confusing, it would be better to store as string, that where the EnumStringType should come to play 
        //Excellent = 0, VeryGood=6, Good=1, Neutral=2, Poor=3, Terrible=4, bad = 5
        Excellent, VeryVeryGood, VeryGood, Good, Neutral, Poor, Terrible, bad
    }

    public class Order
    {
        public virtual Guid Id { get; set; }
        public virtual DateTime OrderAt { get; set; }
        public virtual DateTime ShippedAt { get; set; }
        public virtual Location ShipTo { get; set; }
        public virtual Customer Customer { get; set; }

        public override string ToString()
        {
            return $"\n\tid: {Id}, OrderAt : {OrderAt}, ShippedAt: {ShippedAt}, \n\tShipTo: {ShipTo}";
        }
    }

    //allow us to store the CreditRating as string using component mapping
    public class CustomerCreditRatingType : EnumStringType<CustomerCreditRating> { }
}

