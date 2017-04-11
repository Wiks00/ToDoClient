using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace ORM
{
    public class ToDoContext:DbContext
    {
        public ToDoContext():base("ToDoDB")
        {

        }

        public DbSet<ToDoTask> ToDoTasks { get; set; }

        public DbSet<User> Users { get; set; }

    }
}