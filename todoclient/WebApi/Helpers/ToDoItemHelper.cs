using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApi.Models;

namespace WebApi.Helpers
{
    public static class ToDoItemHelper
    {
        public static int GetId(this ToDoItemModel toDoItem)
            => int.Parse(
                    toDoItem.Name.Substring(
                        toDoItem.Name.LastIndexOf(",", StringComparison.InvariantCultureIgnoreCase) + 1));
    }
}