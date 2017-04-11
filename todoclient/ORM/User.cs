using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ORM
{
    public class User
    {
        public User()
        {
            UserTasks = new List<ToDoTask>();
        }
        public int Id { get; set; }

        public string Username { get; set; }

        public virtual ICollection<ToDoTask> UserTasks { get; set; }
    }
}