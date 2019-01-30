using System;
using System.Linq;
using System.Reflection;
using HibernatingRhinos.Profiler.Appender.NHibernate;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Type;

namespace NhibernateSample
{
    class Program
    {
        static void Main(string[] args)
        {
            //App_Start.NHibernateProfilerBootstrapper.PreStart();
           // NHibernateProfiler.Initialize();
            
            var cfg = new Configuration();
            /*
             //loquacious (talkative) config introduced in 3.0
            cfg.DataBaseIntegration(x =>
            {
                x.ConnectionStringName = "default";
                x.Driver<SqlClientDriver>();
                x.Dialect<MsSql2012Dialect>(); 
                x.LogSqlInConsole = false;
            });

            cfg.AddAssembly(Assembly.GetExecutingAssembly());
            //End loquacious (talkative) config introduced in 3.0 */

            //hibernate.cfg.xml is a default name!!!
            cfg.Configure("hibernate.cfg.xml");
            //cfg.Configure();
           
            var sessionFactory = cfg.BuildSessionFactory();
            //Call demo methods here
            SessionDemo7 (sessionFactory);
            SessionDemo8(sessionFactory);
            cfg.DataBaseIntegration(x =>
            {
                x.ConnectionStringName = "default";
                x.Driver<SqlClientDriver>();
                x.Dialect<MsSql2012Dialect>();
                x.LogSqlInConsole = false;
            });
            Console.WriteLine($"Press any key to continue...");
            Console.ReadKey();
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
           return  new Customer()
           {
               FirstName = "John3",
               LastName = "Doe3",
               //LastName = new string('D', 100), //give an exception 
               Points = 100,
               HasGoldStatus = true,
               //MemberSince = new DateTime(2016,1,1),
               CreditRating = CustomerCreditRating.Good,
               Address = CreateAddress()
               //Street = "Freedom avenue , 3th ",
               //City = "Mons",
               //Province = "Wallonia",
               //Country = "Belgium"
           };
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
        private static void SessionDemo5(ISessionFactory sessionFactory)
        {
            using (var session = sessionFactory.OpenSession())
            using (var tx = session.BeginTransaction())
            {
                var query = from c in session.Query<Customer>()
                    where c.Id == 1
                    select c;

                var customer = query.FirstOrDefault();
                customer.FirstName = "Mohamed";
                session.Save(customer);
                tx.Commit();

                //extra trip to DB: just exploring what Nhibernate doing behind the scenes
                query = from c in session.Query<Customer>()
                    where c.Id == 1
                    select c;
                var customer2 = query.FirstOrDefault();
                Console.WriteLine($"{customer2.FirstName} - {customer2.LastName}");
            }

        }


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
                    FirstName = "Julien",
                    LastName = "Dray"
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

        public virtual int Id { get; set; }
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


        public override string ToString()
        {
            return $"id: {Id}, Firstname : {FirstName}, LastName: {LastName}, " +
                   $"Points: {Points}, HasGoldStatus:  {HasGoldStatus}, " +
                   $"MemberSince: {MemberSince} ({MemberSince.Kind}), CreditRating: {CreditRating}, AverageRating: {AverageRating} ";
        }
    }

    public class Location
    {
        public virtual string Street { get; set; }
        public virtual string City { get; set; }
        public virtual string Province { get; set; }
        public virtual string Country { get; set; }

    }
    public enum CustomerCreditRating
    {
        //those are default values 
        //Adding VeryGood is confusing, it would be better to store as string, that where the EnumStringType should come to play 
        //Excellent = 0, VeryGood=6, Good=1, Neutral=2, Poor=3, Terrible=4, bad = 5
        Excellent, VeryVeryGood, VeryGood, Good , Neutral , Poor, Terrible, bad  
    }

    //allow us to store the CreditRating as string using component mapping
    public class CustomerCreditRatingType : EnumStringType<CustomerCreditRating>{}
}

