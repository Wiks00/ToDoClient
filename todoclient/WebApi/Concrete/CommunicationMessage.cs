using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApi.Models;

namespace WebApi.Concrete
{
    public enum Operation
    {
        Add = 0,
        Delete = 1,
        Update = 2
    }

    public class CommunicationMessage
    {
        public Operation Operation { get; set; }

        public ToDoItemModel ToDoItem { get; set; }

    }
}