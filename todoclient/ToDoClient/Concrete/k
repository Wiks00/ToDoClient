using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ToDoClient.Models;

namespace ToDoClient.Concrete
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

        public ToDoItemViewModel ToDoItem { get; set; }

    }
}