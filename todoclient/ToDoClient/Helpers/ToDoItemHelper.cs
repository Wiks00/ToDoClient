using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using todoclient.DAL;
using ToDoClient.Models;

namespace ToDoClient.Helpers
{
    public static class ToDoItemHelper
    {
        public static int GetId(this ToDoItemViewModel toDoItem)
            => int.Parse(
                    toDoItem.Name.Substring(
                        toDoItem.Name.LastIndexOf(",", StringComparison.InvariantCultureIgnoreCase) + 1));

        public static ToDoItemViewModel ToUIEntity(this ToDoTask toDoTask)
        {
            if (ReferenceEquals(toDoTask, null))
                return null;

            return new ToDoItemViewModel
            {
                ToDoId = toDoTask.Id,
                UserId = toDoTask.UserId,
                IsCompleted = toDoTask.IsCompleted,
                Name = toDoTask.Name             
            };
        }

   
        public static ToDoTask ToOrmEntity(this ToDoItemViewModel toDoItem)
        {
            if (ReferenceEquals(toDoItem, null))
                return null;

            return new ToDoTask
            {
                UserId = toDoItem.UserId,
                IsCompleted = toDoItem.IsCompleted,
                Name = toDoItem.ToDoId != 0 ? toDoItem.Name.Trim()
                    .Substring(0, toDoItem.Name.Trim().LastIndexOf(",", StringComparison.InvariantCultureIgnoreCase) + 1)  +
                toDoItem.ToDoId : toDoItem.Name
            };
        }
    }
}