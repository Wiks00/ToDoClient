using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebApi.DAL;
using WebApi.Models;

namespace WebApi.Helpers
{
    public static class ToDoItemHelper
    {
        public static int GetId(this ToDoItemModel toDoItem)
            => int.Parse(
                toDoItem.Name.Substring(
                    toDoItem.Name.LastIndexOf(",", StringComparison.InvariantCultureIgnoreCase) + 1));

        public static string GetName(this string title, bool withComma = false)
        {
            title = title.Trim();

            return title.Substring(0, title.LastIndexOf(",", StringComparison.InvariantCultureIgnoreCase) + Convert.ToInt32(withComma));
        }

        public static ToDoItemModel ToUIEntity(this ToDoTask toDoTask)
        {
            if (ReferenceEquals(toDoTask, null))
                return null;

            return new ToDoItemModel
            {
                ToDoId = toDoTask.Id,
                UserId = toDoTask.UserId,
                IsCompleted = toDoTask.IsCompleted,
                Name = toDoTask.Name
            };
        }


        public static ToDoTask ToOrmEntity(this ToDoItemModel toDoItem)
        {
            if (ReferenceEquals(toDoItem, null))
                return null;

            return new ToDoTask
            {
                UserId = toDoItem.UserId,
                IsCompleted = toDoItem.IsCompleted,
                Name = toDoItem.ToDoId != 0
                    ? toDoItem.Name.GetName(true) + toDoItem.ToDoId : toDoItem.Name
            };
        }
    }
}