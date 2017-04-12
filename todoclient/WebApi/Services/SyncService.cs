using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using WebApi.Concrete;
using WebApi.DAL;
using WebApi.Helpers;
using WebApi.Models;

namespace WebApi.Services
{
    public class SyncService
    {
        private readonly ToDoService todoService;
        private readonly TempDBEntities dbEntities;

        private static bool isSync;

        public SyncService()
        {
            todoService = new ToDoService();
            dbEntities = new TempDBEntities();
        }

        public async Task<IList<ToDoItemModel>> GetToDoItemsAsync(int userId)
        {
            if (!isSync)
            {
                var response = await todoService.GetItemsAsync(userId);
               
                List<ToDoTask> userToDos = dbEntities.ToDoTask.Where(item => item.UserId == userId).ToList();

                int noGiudId;
                List<ToDoTask>  toDos = userToDos.Where(item => !item.Name.TryGetId(out noGiudId)).ToList();

                foreach (var item in response)
                {
                    if (toDos.Count != 0)
                    { 
                        var toDoItem =
                            toDos.FirstOrDefault(toDo =>toDo.Name.Trim()
                                                            .Equals(item.Name.Trim(), StringComparison.InvariantCultureIgnoreCase));

                        if (!ReferenceEquals(toDoItem, null))
                        {
                            toDoItem.Name = item.Name.GetName(true) + item.ToDoId;

                            dbEntities.Entry(toDoItem).State = EntityState.Modified;
                        }
                    }

                    if (!userToDos.Any(toDo => toDo.Name.GetName().Equals(item.Name.GetName())))
                    {
                        dbEntities.ToDoTask.Add(item.ToOrmEntity());
                    }

                }

                dbEntities.SaveChanges();

                isSync = true;
            }

            return dbEntities.ToDoTask.Where(item => item.UserId == userId).ToList().Select(item => item.ToUIEntity()).ToList();
        }

        public async Task<HttpResponseMessage> AddToDoItemAsync(ToDoItemModel toDoItem)
        {
            toDoItem.Name += "," + Guid.NewGuid();

            var response = await todoService.CreateItemAsync(toDoItem);

            if (response.IsSuccessStatusCode)
            {
                dbEntities.ToDoTask.Add(toDoItem.ToOrmEntity());
                dbEntities.SaveChanges();

                isSync = false;
            }

            return response;
        }

        public async Task<HttpResponseMessage> UpdateToDoItemAsync(ToDoItemModel toDoItem)
        {
            int cloudId;
            if (toDoItem.Name.TryGetId(out cloudId))
            {
                var copyToCloud = toDoItem.Clone();
                copyToCloud.ToDoId = cloudId;

                var response = await todoService.UpdateItemAsync(copyToCloud);

                if (response.IsSuccessStatusCode)
                {
                    var toDoTask = dbEntities.ToDoTask.Find(toDoItem.ToDoId);

                    toDoTask.IsCompleted = toDoItem.IsCompleted;

                    dbEntities.Entry(toDoTask).State = EntityState.Modified;
                    dbEntities.SaveChanges();

                    isSync = false;
                }

                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        public async Task<HttpResponseMessage> DeleteToDoItemAsync(int id)
        {

            var ormToDo = dbEntities.ToDoTask.Find(id);

            int cloudId;
            if (ormToDo.Name.TryGetId(out cloudId))
            {
                var response = await todoService.DeleteItemAsync(cloudId);

                if (response.IsSuccessStatusCode)
                {
                    dbEntities.ToDoTask.Remove(ormToDo);

                    dbEntities.SaveChanges();

                    isSync = false;
                }

                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }        
    }
}